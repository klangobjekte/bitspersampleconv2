﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WWLanBenchmark {
    class ServerController {
        private const int HASH_BYTES = 32;
        private const long ONE_MEGA = 1000 * 1000;
        private const long ONE_GIGA = 1000 * 1000 * 1000;

        private ServerReceiver mServerReceiver = new ServerReceiver();

        TcpListener mListener = null;
        private BackgroundWorker mBackgroundWorker;

        private static int ReadInt32(NetworkStream stream) {
            var data = new byte[4];
            stream.Read(data, 0, data.Count());
            using (var ms = new MemoryStream(data)) {
                using (var br = new BinaryReader(ms)) {
                    return br.ReadInt32();
                }
            }
        }

        private static void WriteInt64(NetworkStream stream, long v) {
            var data = new byte[8];
            using (var ms = new MemoryStream(data)) {
                using (var bw = new BinaryWriter(ms)) {
                    bw.Write(v);
                    bw.Flush();
                }
            }
            stream.Write(data, 0, 8);
        }

        private static void TouchMemory(Byte[] buff) {
            for (int i = 0; i < buff.Length; ++i) {
                buff[i] = 0;
            }
        }

        struct Settings {
            public long xmitFragmentBytes;
            public long totalBytes;
        };

        private static Settings RecvSettings(NetworkStream stream) {
            var settings = new Settings();
            settings.xmitFragmentBytes = Utility.StreamReadInt64(stream);
            settings.totalBytes = Utility.StreamReadInt64(stream);
            return settings;
        }

        public void Abort() {
            if (mListener != null) {
                mListener.Server.Close();
            }
        }

        public void Run(BackgroundWorker backgroundWorker, int controlPort, int dataPort, int recvTimeoutMillisec) {
            mBackgroundWorker = backgroundWorker;
            try {
                // コントロールポートの待受を開始する。
                IPAddress addr = IPAddress.Any;
                mListener = new TcpListener(addr, controlPort);
                mListener.Start();

                while (true) {
                    mBackgroundWorker.ReportProgress(1, "Waiting for a connection...\n");

                    using (var client = mListener.AcceptTcpClient()) {
                        using (var stream = client.GetStream()) {
                            mBackgroundWorker.ReportProgress(1, string.Format("Connected from {0}\n", client.Client.RemoteEndPoint));
                            // 接続してきた。設定情報を受信する。
                            Settings settings = RecvSettings(stream);

                            mBackgroundWorker.ReportProgress(1, string.Format("Settings: Recv {0}GB of data. data fragment size={1}Mbytes\n",
                                settings.totalBytes / ONE_GIGA, settings.xmitFragmentBytes / ONE_MEGA));

                            // データポートの待受を開始する。
                            if (!mServerReceiver.Initialize(dataPort, settings.totalBytes)) {
                                mBackgroundWorker.ReportProgress(1, "Error: failed to listen data port!\n");
                                // 失敗したので終了する。
                                mListener.Stop();
                                return;
                            }

                            // 準備OKを戻す。
                            stream.WriteByte(0);

                            byte[] recvHash = Utility.StreamReadBytes(stream, HASH_BYTES);

                            var sw = new Stopwatch();
                            sw.Start();

                            mServerReceiver.Wait(recvTimeoutMillisec);
                            sw.Stop();

                            mBackgroundWorker.ReportProgress(1, string.Format("Received {0}GB in {1} seconds. {2:0.###}Gbps\n",
                                settings.totalBytes / ONE_GIGA, sw.ElapsedMilliseconds / 1000.0,
                                (double)settings.totalBytes * 8 / ONE_GIGA / (sw.ElapsedMilliseconds / 1000.0)));

                            mBackgroundWorker.ReportProgress(1, string.Format("Checking consistency of received data...\n"));
                            var calcHash = mServerReceiver.CalcHash();
                            if (calcHash.SequenceEqual(recvHash)) {
                                mBackgroundWorker.ReportProgress(1, string.Format("SHA256 hash consistency check succeeded.\n"));
                            } else {
                                mBackgroundWorker.ReportProgress(1, string.Format("SHA256 hash consistency check FAILED !!\n"));
                            }

                            mServerReceiver.Terminate();

                            WriteInt64(stream, sw.ElapsedMilliseconds);
                        }
                    }
                }
            } catch (SocketException e) {
                Console.WriteLine("SocketException: {0}\n", e);
            } catch (IOException e) {
                Console.WriteLine("IOException: {0}\n", e);
            } finally {
                mListener.Stop();
            }
            mListener = null;
        }
    }
}