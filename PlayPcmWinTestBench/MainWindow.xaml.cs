﻿// 日本語UTF-8

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Controls;
using PcmDataLib;
using Wasapi;
using WasapiPcmUtil;
using WavRWLib2;
using System.Threading.Tasks;
using WWDirectComputeCS;
using System.Runtime.InteropServices;

namespace PlayPcmWinTestBench {
    public partial class MainWindow : Window {
        Wasapi.WasapiCS wasapi;
        RNGCryptoServiceProvider gen = new RNGCryptoServiceProvider();
        private BackgroundWorker m_playWorker;
        private BackgroundWorker m_USAQworker;
        private BackgroundWorker m_FirWorker;

        enum AB {
            Unknown = -1,
            A,
            B
        };

        class TestInfo {
            public AB x;
            public AB answer;

            public TestInfo(AB x) {
                this.x      = x;
                answer = AB.Unknown;
            }

            public void SetAnswer(AB answer) {
                this.answer = answer;
            }
        }

        List<TestInfo> m_testInfoList = new List<TestInfo>();

        private AB GetX() {
            return m_testInfoList[m_testInfoList.Count - 1].x;
        }

        /// <summary>
        /// 回答する。
        /// </summary>
        /// <param name="answer"></param>
        /// <returns>true:まだ続きがある。false:終わり</returns>
        private bool Answer(AB answer) {
            m_testInfoList[m_testInfoList.Count - 1].answer = answer;
            if (10 == m_testInfoList.Count) {
                return false;
            }
            return true;
        }

        private void PrepareNextTest() {
            byte[] r = new byte[1];
            gen.GetBytes(r);

            AB x = ((r[0] & 1) == 0) ? AB.A : AB.B;
            TestInfo ti = new TestInfo(x);
            m_testInfoList.Add(ti);
        }


        public MainWindow() {
            InitializeComponent();

            wasapi = new WasapiCS();
            wasapi.Init();

            Prepare();
        }

        private void Prepare() {
            ListDevices();

            m_testInfoList.Clear();
            PrepareNextTest();

            UpdateUIStatus();

            m_playWorker = new BackgroundWorker();
            m_playWorker.WorkerReportsProgress = true;
            m_playWorker.DoWork += new DoWorkEventHandler(PlayDoWork);
            m_playWorker.ProgressChanged += new ProgressChangedEventHandler(PlayProgressChanged);
            m_playWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(PlayRunWorkerCompleted);
            m_playWorker.WorkerSupportsCancellation = true;

            m_USAQworker = new BackgroundWorker();
            m_USAQworker.WorkerReportsProgress = true;
            m_USAQworker.DoWork += new DoWorkEventHandler(m_USAQworker_DoWork);
            m_USAQworker.ProgressChanged += new ProgressChangedEventHandler(m_USAQworker_ProgressChanged);
            m_USAQworker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(m_USAQworker_RunWorkerCompleted);
            m_USAQworker.WorkerSupportsCancellation = true;

            m_FirWorker = new BackgroundWorker();
            m_FirWorker.WorkerReportsProgress = true;
            m_FirWorker.DoWork += new DoWorkEventHandler(m_FirWorker_DoWork);
            m_FirWorker.ProgressChanged += new ProgressChangedEventHandler(m_FirWorker_ProgressChanged);
            m_FirWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(m_FirWorker_RunWorkerCompleted);
            m_FirWorker.WorkerSupportsCancellation = true;

            string s = "※GPU計算機能を使用するためには以下の3つの準備が要ります:\r\n"
                + "・GPUはGeForce GTX 570以上を用意して下さい。\r\n"
                + "・最新のNVIDIAディスプレイドライバ をインストールして下さい(バージョン260以降が必要)。\r\n"
                + "・最新のDirectXエンドユーザーランタイムをインストールする必要があります(August 2009以降が必要)。"
                + " http://www.microsoft.com/downloads/details.aspx?FamilyID=2da43d38-db71-4c1b-bc6a-9b6652cd92a3&displayLang=ja\r\n";

            textBoxUSResult.Text = s;
            textBoxAQResult.Text = s;

            InitializeFir();
            UpdateFreqLine();
        }

        private void Window_Closed(object sender, EventArgs e) {
            if (wasapi != null) {
                Stop();

                // バックグラウンドスレッドにjoinして、完全に止まるまで待ち合わせする。
                // そうしないと、バックグラウンドスレッドによって使用中のオブジェクトが
                // この後のTermの呼出によって開放されてしまい問題が起きる。

                while (m_playWorker.IsBusy) {
                    System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(
                        System.Windows.Threading.DispatcherPriority.Background,
                        new System.Threading.ThreadStart(delegate { }));

                    System.Threading.Thread.Sleep(100);
                }

                wasapi.UnchooseDevice();
                wasapi.Term();
                wasapi = null;
            }

        }

        private void ComboBoxDeviceInit(ComboBox comboBox) {
            int selectedIdx = comboBox.SelectedIndex;

            comboBox.Items.Clear();

            int nDevices = wasapi.GetDeviceCount();
            for (int i = 0; i < nDevices; ++i) {
                string deviceName = wasapi.GetDeviceName(i);
                comboBox.Items.Add(deviceName);
            }

            if (0 <= selectedIdx && selectedIdx < nDevices) {
                comboBox.SelectedIndex = selectedIdx;
            } else if (0 < nDevices) {
                comboBox.SelectedIndex = 0;
            }
        }

        private void ListDevices() {
            int hr = wasapi.DoDeviceEnumeration(WasapiCS.DeviceType.Play);
            
            ComboBoxDeviceInit(comboBoxDeviceA);
            ComboBoxDeviceInit(comboBoxDeviceB);
        }

        /// <summary>
        /// 再生中。バックグラウンドスレッド。
        /// </summary>
        private void PlayDoWork(object o, DoWorkEventArgs args) {
            //Console.WriteLine("PlayDoWork started");
            BackgroundWorker bw = (BackgroundWorker)o;

            while (!wasapi.Run(100)) {
                m_playWorker.ReportProgress(0);
                System.Threading.Thread.Sleep(1);
                if (bw.CancellationPending) {
                    Console.WriteLine("PlayDoWork() CANCELED");
                    wasapi.Stop();
                    args.Cancel = true;
                }
            }

            // 正常に最後まで再生が終わった場合、ここでStopを呼んで、後始末する。
            // キャンセルの場合は、2回Stopが呼ばれることになるが、問題ない!!!
            wasapi.Stop();

            // 停止完了後タスクの処理は、ここではなく、PlayRunWorkerCompletedで行う。

            //Console.WriteLine("PlayDoWork end");
        }

        /// <summary>
        /// 再生の進行状況をUIに反映する。
        /// </summary>
        private void PlayProgressChanged(object o, ProgressChangedEventArgs args) {
            BackgroundWorker bw = (BackgroundWorker)o;

            if (null == wasapi) {
                return;
            }

            if (bw.CancellationPending) {
                // ワーカースレッドがキャンセルされているので、何もしない。
                return;
            }
        }

        /// <summary>
        /// 再生終了。
        /// </summary>
        private void PlayRunWorkerCompleted(object o, RunWorkerCompletedEventArgs args) {
            m_status = Status.Stop;

            wasapi.Unsetup();
            UnchooseRecreateDeviceList();
        }

        enum Status {
            Stop,
            Play,
        }

        private Status m_status = Status.Stop;

        public void UpdateUIStatus() {
            labelGuide.Content = string.Format("テスト{0}回目", m_testInfoList.Count);

            if (m_status != Status.Play) {
                // Stop
                buttonPlayA.IsEnabled = false;
                buttonPlayB.IsEnabled = false;
                buttonPlayX.IsEnabled = false;
                buttonPlayY.IsEnabled = false;
                buttonConfirm.IsEnabled = false;
                buttonStopA.IsEnabled = false;
                buttonStopB.IsEnabled = false;
                buttonStopX.IsEnabled = false;
                buttonStopY.IsEnabled = false;

                if (0 < textBoxPathA.Text.Length && System.IO.File.Exists(textBoxPathA.Text)) {
                    buttonPlayA.IsEnabled = true;
                }
                if (0 < textBoxPathA.Text.Length && System.IO.File.Exists(textBoxPathB.Text)) {
                    buttonPlayB.IsEnabled = true;
                }
                if (buttonPlayA.IsEnabled && buttonPlayB.IsEnabled) {
                    buttonPlayX.IsEnabled = true;
                    buttonPlayY.IsEnabled = true;
                    buttonConfirm.IsEnabled = true;
                }
            } else {
                // Play
                buttonPlayA.IsEnabled = false;
                buttonPlayB.IsEnabled = false;
                buttonPlayX.IsEnabled = false;
                buttonPlayY.IsEnabled = false;
                buttonStopA.IsEnabled = true;
                buttonStopB.IsEnabled = true;
                buttonStopX.IsEnabled = true;
                buttonStopY.IsEnabled = true;
            }
        }

        private void buttonFinish_Click(object sender, RoutedEventArgs e) {
            DispResult();
            DialogResult = true;
            Close();
        }

        private string BrowseOpenFile() {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.Filter =
                "WAVEファイル|*.wav;*.wave";
            dlg.Multiselect = false;

            Nullable<bool> result = dlg.ShowDialog();
            if (result != true) {
                return "";
            }
            return dlg.FileName;
        }

        private string BrowseSaveFile() {
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.Filter =
                "WAVEファイル|*.wav";

            Nullable<bool> result = dlg.ShowDialog();
            if (result != true) {
                return "";
            }
            return dlg.FileName;
        }

        private void buttonBrowseA_Click(object sender, RoutedEventArgs e) {
            string fileName = BrowseOpenFile();
            if (0 < fileName.Length) {
                textBoxPathA.Text = fileName;
                UpdateUIStatus();
            }
        }

        private void buttonBrowseB_Click(object sender, RoutedEventArgs e) {
            string fileName = BrowseOpenFile();
            if (0 < fileName.Length) {
                textBoxPathB.Text = fileName;
                UpdateUIStatus();
            }
        }

        private void Stop() {
            if (m_status == Status.Play) {
                buttonStopA.IsEnabled = false;
                buttonStopB.IsEnabled = false;
                buttonStopX.IsEnabled = false;
                buttonStopY.IsEnabled = false;
                wasapi.Stop();
            }
        }

        private void buttonStop_Click(object sender, RoutedEventArgs e) {
            Stop();
        }
        
        private PcmData ReadWavFile(string path) {
            PcmData pcmData = new PcmData();

            using (BinaryReader br = new BinaryReader(
                    File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read))) {
                WavData wavData = new WavData();
                bool readSuccess = wavData.ReadHeaderAndSamples(br, 0, -1);
                if (!readSuccess) {
                    return null;
                }
                pcmData.SetFormat(wavData.NumChannels, wavData.BitsPerFrame, wavData.BitsPerFrame,
                    wavData.SampleRate, wavData.SampleValueRepresentationType, wavData.NumFrames);
                pcmData.SetSampleArray(wavData.NumFrames, wavData.GetSampleArray());
            }

            return pcmData;
        }

        private bool WriteWavFile(PcmData pcmData, string path) {
            using (BinaryWriter bw = new BinaryWriter(
                    File.Open(path, FileMode.Create, FileAccess.Write, FileShare.Write))) {
                WavData wavData = new WavData();
                wavData.Set(pcmData.NumChannels, pcmData.BitsPerSample, pcmData.ValidBitsPerSample, pcmData.SampleRate,
                    pcmData.SampleValueRepresentationType, pcmData.NumFrames, pcmData.GetSampleArray());
                wavData.Write(bw);
            }

            return true;
        }

        private SampleFormatInfo m_sampleFormat;

        private int WasapiSetup(
                bool isExclusive,
                bool isEventDriven,
                int sampleRate,
                int pcmDataValidBitsPerSample,
                PcmDataLib.PcmData.ValueRepresentationType vrt,
                int latencyMillisec) {
            int num = SampleFormatInfo.GetDeviceSampleFormatCandidateNum(
                isExclusive ? WasapiSharedOrExclusive.Exclusive : WasapiSharedOrExclusive.Shared,
                BitsPerSampleFixType.AutoSelect,
                pcmDataValidBitsPerSample, vrt);

            int hr = -1;
            for (int i = 0; i < num; ++i) {
                SampleFormatInfo sf = SampleFormatInfo.GetDeviceSampleFormat(
                    isExclusive ? WasapiSharedOrExclusive.Exclusive : WasapiSharedOrExclusive.Shared,
                    BitsPerSampleFixType.AutoSelect,
                    pcmDataValidBitsPerSample, vrt, i);

                wasapi.SetDataFeedMode(
                    isEventDriven ?
                        WasapiCS.DataFeedMode.EventDriven :
                        WasapiCS.DataFeedMode.TimerDriven);

                wasapi.SetLatencyMillisec(latencyMillisec);

                hr = wasapi.Setup(
                    sampleRate, sf.GetSampleFormatType(), 2);
                if (0 <= hr) {
                    m_sampleFormat = sf;
                    return hr;
                }
            }
            wasapi.Unsetup();
            return hr;
        }


        private void UnchooseRecreateDeviceList() {
            wasapi.UnchooseDevice();
            ListDevices();
            UpdateUIStatus();
        }

        private void buttonPlayA_Click(object sender, RoutedEventArgs e) {
            PcmData pcmData = ReadWavFile(textBoxPathA.Text);
            if (null == pcmData) {
                MessageBox.Show(
                    string.Format("WAVファイル A 読み込み失敗: {0}", textBoxPathA.Text));
                return;
            }

            int hr = wasapi.ChooseDevice(comboBoxDeviceA.SelectedIndex);
            if (hr < 0) {
                MessageBox.Show(string.Format("Wasapi.ChooseDevice()失敗 {0:X8}", hr));
                UnchooseRecreateDeviceList();
                return;
            }

            hr = WasapiSetup(
                radioButtonExclusiveA.IsChecked == true,
                radioButtonEventDrivenA.IsChecked == true,
                pcmData.SampleRate,
                pcmData.ValidBitsPerSample,
                pcmData.SampleValueRepresentationType,
                Int32.Parse(textBoxLatencyA.Text));
            if (hr < 0) {
                MessageBox.Show(string.Format("Wasapi.Setup失敗 {0:X8}", hr));
                UnchooseRecreateDeviceList();
                return;
            }

            pcmData = PcmUtil.BitsPerSampleConvAsNeeded(pcmData, m_sampleFormat.GetSampleFormatType());

            wasapi.ClearPlayList();
            wasapi.AddPlayPcmDataStart();
            wasapi.AddPlayPcmData(0, pcmData.GetSampleArray());
            wasapi.AddPlayPcmDataEnd();

            hr = wasapi.Start(0);
            m_playWorker.RunWorkerAsync();
            m_status = Status.Play;
            UpdateUIStatus();
        }

        private void buttonPlayB_Click(object sender, RoutedEventArgs e) {
            PcmData pcmData = ReadWavFile(textBoxPathB.Text);
            if (null == pcmData) {
                MessageBox.Show(
                    string.Format("WAVファイル B 読み込み失敗: {0}", textBoxPathB.Text));
                return;
            }

            int hr = wasapi.ChooseDevice(comboBoxDeviceB.SelectedIndex);
            if (hr < 0) {
                MessageBox.Show(string.Format("Wasapi.ChooseDevice()失敗 {0:X8}", hr));
                UnchooseRecreateDeviceList();
                return;
            }

            hr = WasapiSetup(
                radioButtonExclusiveB.IsChecked == true,
                radioButtonEventDrivenB.IsChecked == true,
                pcmData.SampleRate,
                pcmData.ValidBitsPerSample,
                pcmData.SampleValueRepresentationType,
                Int32.Parse(textBoxLatencyB.Text));
            if (hr < 0) {
                MessageBox.Show(string.Format("Wasapi.Setup失敗 {0:X8}", hr));
                UnchooseRecreateDeviceList();
                return;
            }

            pcmData = PcmUtil.BitsPerSampleConvAsNeeded(pcmData, m_sampleFormat.GetSampleFormatType());

            wasapi.ClearPlayList();
            wasapi.AddPlayPcmDataStart();
            wasapi.AddPlayPcmData(0, pcmData.GetSampleArray());
            wasapi.AddPlayPcmDataEnd();

            hr = wasapi.Start(0);
            m_playWorker.RunWorkerAsync();
            m_status = Status.Play;
            UpdateUIStatus();
        }

        private void buttonPlayX_Click(object sender, RoutedEventArgs e) {
            AB x = GetX();
            if (x == AB.A) {
                buttonPlayA_Click(sender, e);
            } else {
                buttonPlayB_Click(sender, e);
            }
        }

        private void buttonPlayY_Click(object sender, RoutedEventArgs e) {
            AB x = GetX();
            if (x == AB.A) {
                buttonPlayB_Click(sender, e);
            } else {
                buttonPlayA_Click(sender, e);
            }
        }

        private void buttonConfirm_Click(object sender, RoutedEventArgs e) {
            if (!Answer(radioButtonAXBY.IsChecked == true ? AB.A : AB.B)) {
                // 終わり
                DispResult();
                DialogResult = true;
                Close();
                return;
            }
            PrepareNextTest();
            UpdateUIStatus();
        }

        private void DispResult() {
            if (m_testInfoList.Count == 1 && m_testInfoList[0].answer == AB.Unknown) {
                return;
            }
            string s = "結果発表\r\n\r\n";

            int answered = 0;
            int correct = 0;
            for (int i=0; i<m_testInfoList.Count; ++i) {
                TestInfo ti = m_testInfoList[i];
                if (ti.answer == AB.Unknown) {
                    break;
                }

                ++answered;
                s += string.Format("テスト{0}回目 {1} 正解は X={2},Y={3}\r\n",
                    i, ti.x == ti.answer ? "○" : "×",
                    ti.x == AB.A ? "A" : "B",
                    ti.x == AB.A ? "B" : "A");
                if (ti.x == ti.answer) {
                    ++correct;
                }
            }
            s += string.Format("\r\n{0}回中{1}回正解", answered, correct);
            MessageBox.Show(s);
        }

        /// /////////////////////////////////////////////////////////////////
        /// アップサンプル

        enum ProcessDevice {
            Cpu,
            Gpu
        };

        struct USWorkerArgs {
            public string inputPath;
            public string outputPath;
            public int resampleFrequency;
            public int convolutionN;
            public ProcessDevice device;
            public int sampleSlice;

            public bool addJitter;
            public double sequentialJitterFrequency;
            public double sequentialJitterPicoseconds;
            public double tpdfJitterPicoseconds;
            public double rpdfJitterPicoseconds;

            public int outputBitsPerSample;
            public PcmDataLib.PcmData.ValueRepresentationType outputVRT;

            // --------------------------------------------------------
            // 以降、物置(DoWork()の中で使用する)
            public double thetaCoefficientSeqJitter;
            public double ampSeqJitter;
            public double ampTpdfJitter;
            public double ampRpdfJitter;

            public int[] resamplePosArray;
            public double[] fractionArray;
        };

        private void buttonUSBrowseOpen_Click(object sender, RoutedEventArgs e) {
            string fileName = BrowseOpenFile();
            if (0 < fileName.Length) {
                textBoxUSInputFilePath.Text = fileName;
            }
        }

        private void buttonUSBrowseSaveAs_Click(object sender, RoutedEventArgs e) {
            string fileName = BrowseSaveFile();
            if (0 < fileName.Length) {
                textBoxUSOutputFilePath.Text = fileName;
            }
        }

        private void buttonUSOutputStart_Click(object sender, RoutedEventArgs e) {
            USWorkerArgs args = new USWorkerArgs();
            args.inputPath = textBoxUSInputFilePath.Text;
            args.outputPath = textBoxUSOutputFilePath.Text;
            if (!System.IO.File.Exists(args.inputPath)) {
                MessageBox.Show("エラー。入力ファイルが存在しません");
                return;
            }
            if (!Int32.TryParse(textBoxUSFrequency.Text, out args.resampleFrequency) ||
                    args.resampleFrequency < 0.0) {
                MessageBox.Show("エラー。リサンプル周波数に0以上の数値を入力してください");
                return;
            }

            args.convolutionN = 256;
            args.device = ProcessDevice.Cpu;
            args.sampleSlice = 256;
            if (radioButtonUSCpu16.IsChecked == true) {
                args.convolutionN = 65536;
            }
            if (radioButtonUSGpu16.IsChecked == true) {
                args.convolutionN = 65536;
                args.device = ProcessDevice.Gpu;
                args.sampleSlice = 32768;
            }
            if (radioButtonUSGpu20.IsChecked == true) {
                args.convolutionN = 1048576;
                args.device = ProcessDevice.Gpu;

                // 重いので減らす。
                args.sampleSlice = 256;
            }
            if (radioButtonUSGpu24.IsChecked == true) {
                args.convolutionN = 16777216;
                args.device = ProcessDevice.Gpu;

                // この条件では一度に32768個処理できない。
                // 16384以下の値をセットする。
                args.sampleSlice = 16;
            }

            args.addJitter = false;

            // 出力フォーマット
            args.outputBitsPerSample = 16;
            args.outputVRT = PcmData.ValueRepresentationType.SInt;
            if (radioButtonOutputSint24.IsChecked == true) {
                args.outputBitsPerSample = 24;
            }
            if (radioButtonOutputFloat32.IsChecked == true) {
                args.outputBitsPerSample = 32;
                args.outputVRT = PcmData.ValueRepresentationType.SFloat;
            }

            buttonUSOutputStart.IsEnabled = false;
            buttonUSBrowseOpen.IsEnabled   = false;
            buttonUSBrowseSaveAs.IsEnabled = false;
            buttonUSAbort.IsEnabled        = true;
            progressBarUS.Value = 0;

            textBoxUSResult.Text += string.Format("処理中 {0}⇒{1}……処理中はPCの動作が重くなります!\r\n",
                args.inputPath, args.outputPath);
            textBoxUSResult.ScrollToEnd();

            m_USAQworker.RunWorkerAsync(args);
        }

        ////////////////////////////////////////////////////////////////////////////////////////
        /// 音質劣化

        private void buttonAQBrowseOpen_Click(object sender, RoutedEventArgs e) {
            string fileName = BrowseOpenFile();
            if (0 < fileName.Length) {
                textBoxAQInputFilePath.Text = fileName;
            }
        }

        private void buttonAQBrowseSaveAs_Click(object sender, RoutedEventArgs e) {
            string fileName = BrowseSaveFile();
            if (0 < fileName.Length) {
                textBoxAQOutputFilePath.Text = fileName;
            }
        }

        private void buttonAQOutputStart_Click(object sender, RoutedEventArgs e) {
            USWorkerArgs args = new USWorkerArgs();
            args.inputPath = textBoxAQInputFilePath.Text;
            args.outputPath = textBoxAQOutputFilePath.Text;
            if (!System.IO.File.Exists(args.inputPath)) {
                MessageBox.Show("エラー。入力ファイルが存在しません");
                return;
            }
            if (!Double.TryParse(textBoxSequentialJitterFrequency.Text, out args.sequentialJitterFrequency) ||
                    args.sequentialJitterFrequency < 0.0) {
                MessageBox.Show("エラー。周期ジッター周波数に0以上の数値を入力してください");
                return;
            }
            if (!Double.TryParse(textBoxSequentialJitterPicoseconds.Text, out args.sequentialJitterPicoseconds) ||
                    args.sequentialJitterPicoseconds < 0.0) {
                MessageBox.Show("エラー。周期ジッター最大ずれ量に0以上の数値を入力してください");
                return;
            }
            // sequential jitter RMS⇒peak 正弦波なので√2倍する
            args.sequentialJitterPicoseconds *= Math.Sqrt(2.0);

            if (!Double.TryParse(textBoxTpdfJitterPicoseconds.Text, out args.tpdfJitterPicoseconds) ||
                    args.tpdfJitterPicoseconds < 0.0) {
                MessageBox.Show("エラー。三角分布ジッター最大ずれ量に0以上の数値を入力してください");
                return;
            }
            if (!Double.TryParse(textBoxRpdfJitterPicoseconds.Text, out args.rpdfJitterPicoseconds) ||
                    args.rpdfJitterPicoseconds < 0.0) {
                MessageBox.Show("エラー。一様分布ジッター最大ずれ量に0以上の数値を入力してください");
                return;
            }

            args.convolutionN = 256;
            args.device = ProcessDevice.Cpu;
            args.sampleSlice = 32768;
            if (radioButtonAQCpu16.IsChecked == true) {
                args.convolutionN = 65536;
            }
            if (radioButtonAQGpu16.IsChecked == true) {
                args.convolutionN = 65536;
                args.device = ProcessDevice.Gpu;
            }
            if (radioButtonAQGpu20.IsChecked == true) {
                args.convolutionN = 1048576;
                args.device = ProcessDevice.Gpu;

                // 重いので減らす。
                args.sampleSlice = 256;
            }
            if (radioButtonAQGpu24.IsChecked == true) {
                args.convolutionN = 16777216;
                args.device = ProcessDevice.Gpu;

                // この条件では一度に32768個処理できない。
                // 16384以下の値をセットする。
                args.sampleSlice = 16;
            }

            args.addJitter = true;

            // 出力フォーマット
            args.outputBitsPerSample = 32;
            args.outputVRT = PcmData.ValueRepresentationType.SFloat;

            buttonAQOutputStart.IsEnabled  = false;
            buttonAQBrowseOpen.IsEnabled   = false;
            buttonAQBrowseSaveAs.IsEnabled = false;
            buttonAQAbort.IsEnabled        = true;
            progressBarAQ.Value = 0;

            textBoxAQResult.Text += string.Format("処理中 {0}⇒{1}……処理中はPCの動作が重くなります!\r\n",
                args.inputPath, args.outputPath);
            textBoxAQResult.ScrollToEnd();

            m_USAQworker.RunWorkerAsync(args);
        }

        private void buttonAQAbort_Click(object sender, RoutedEventArgs e) {
            m_USAQworker.CancelAsync();
            buttonAQAbort.IsEnabled = false;
        }

        ////////////////////////////////////////////////////////////////////
        // US AQ共用ワーカースレッド

        private int CpuUpsample(USWorkerArgs args, PcmData pcmDataIn, PcmData pcmDataOut) {
            int hr = 0;

            int sampleTotalTo = (int)pcmDataOut.NumFrames;

            float[] sampleData = new float[pcmDataIn.NumFrames];
            for (int ch = 0; ch < pcmDataIn.NumChannels; ++ch) {
                for (int i = 0; i < pcmDataIn.NumFrames; ++i) {
                    sampleData[i] = pcmDataIn.GetSampleValueInFloat(ch, i);
                }

                WWUpsampleCpu us = new WWUpsampleCpu();
                if (args.addJitter) {
                    hr = us.Setup(args.convolutionN, sampleData, sampleData.Length,
                        pcmDataIn.SampleRate, pcmDataOut.SampleRate, sampleTotalTo,
                        args.resamplePosArray, args.fractionArray);
                } else {
                    hr = us.Setup(args.convolutionN, sampleData, sampleData.Length,
                        pcmDataIn.SampleRate, pcmDataOut.SampleRate, sampleTotalTo);
                }
                if (hr < 0) {
                    break;
                }

                for (int offs = 0; offs < sampleTotalTo; offs += args.sampleSlice) {
                    int sample1 = args.sampleSlice;
                    if (sampleTotalTo - offs < sample1) {
                        sample1 = sampleTotalTo - offs;
                    }
                    if (sample1 < 1) {
                        break;
                    }

                    float[] outFragment = new float[sample1];
                    hr = us.Do(offs, sample1, outFragment);
                    if (hr < 0) {
                        // ここからbreakしても外のfor文までしか行かない
                        us.Unsetup();
                        sampleData = null;
                        break;
                    }
                    if (m_USAQworker.CancellationPending) {
                        // ここからbreakしても外のfor文までしか行かない
                        us.Unsetup();
                        sampleData = null;
                        hr = -1;
                        break;
                    }
                    if (0 <= hr) {
                        // 成功。出てきたデータをpcmDataOutに詰める。
                        for (int j = 0; j < sample1; ++j) {
                            pcmDataOut.SetSampleValueInFloat(ch, offs + j, outFragment[j]);
                        }
                    }
                    outFragment = null;

                    // 10%～99%
                    m_USAQworker.ReportProgress(
                        10 + (int)(89L * offs / sampleTotalTo + 89L * ch) / pcmDataIn.NumChannels);
                }

                if (m_USAQworker.CancellationPending) {
                    break;
                }
                if (hr < 0) {
                    break;
                }

                us.Unsetup();
            }

            sampleData = null;

            return hr;
        }

        private int GpuUpsample(USWorkerArgs args, PcmData pcmDataIn, PcmData pcmDataOut) {
            int hr = 0;

            int sampleTotalTo = (int)pcmDataOut.NumFrames;

            float[] sampleData = new float[pcmDataIn.NumFrames];
            for (int ch = 0; ch < pcmDataIn.NumChannels; ++ch) {
                for (int i = 0; i < pcmDataIn.NumFrames; ++i) {
                    sampleData[i] = pcmDataIn.GetSampleValueInFloat(ch, i);
                }

                WWUpsampleGpu us = new WWUpsampleGpu();
                if (args.addJitter) {
                    hr = us.Init(args.convolutionN, sampleData, (int)pcmDataIn.NumFrames, pcmDataIn.SampleRate,
                        args.resampleFrequency, sampleTotalTo, args.resamplePosArray, args.fractionArray);
                } else {
                    hr = us.Init(args.convolutionN, sampleData, (int)pcmDataIn.NumFrames, pcmDataIn.SampleRate,
                        args.resampleFrequency, sampleTotalTo);
                }
                if (hr < 0) {
                    us.Term();
                    return hr;
                }

                int sampleRemain = sampleTotalTo;
                int offs = 0;
                while (0 < sampleRemain) {
                    int sample1 = args.sampleSlice;
                    if (sampleRemain < sample1) {
                        sample1 = sampleRemain;
                    }
                    hr = us.ProcessPortion(offs, sample1);
                    if (hr < 0) {
                        break;
                    }
                    if (m_USAQworker.CancellationPending) {
                        us.Term();
                        return -1;
                    }

                    sampleRemain -= sample1;
                    offs += sample1;

                    // 10%～99%
                    m_USAQworker.ReportProgress(
                        10 + (int)(89L * offs / sampleTotalTo + 89L * ch) / pcmDataIn.NumChannels);
                }

                if (0 <= hr) {
                    float[] output = new float[sampleTotalTo];
                    hr = us.GetResultFromGpuMemory(ref output, sampleTotalTo);
                    if (0 <= hr) {
                        // すべて成功。
                        for (int i = 0; i < pcmDataOut.NumFrames; ++i) {
                            pcmDataOut.SetSampleValueInFloat(ch, i, output[i]);
                        }
                    }
                    output = null;
                }
                us.Term();

                if (hr < 0) {
                    break;
                }
            }
            sampleData = null;

            return hr;
        }

        private void m_USAQworker_ProgressChanged(object sender, ProgressChangedEventArgs e) {
            progressBarUS.Value = e.ProgressPercentage;
            progressBarAQ.Value = e.ProgressPercentage;
        }

        /// <summary>
        ///  仮数部が32bitぐらいまで値が埋まっているランダムの0～1
        /// </summary>
        /// <returns></returns>
        private static double GenRandom0to1(RNGCryptoServiceProvider gen) {
            byte[] bytes = new byte[4];
            gen.GetBytes(bytes);
            uint u = BitConverter.ToUInt32(bytes, 0);
            double d = (double)u / uint.MaxValue;
            return d;
        }

        /// <summary>
        /// ジッター発生。
        /// </summary>
        private double GenerateJitter(USWorkerArgs args, int offs) {
            double seqJitter = args.ampSeqJitter
                * Math.Sin((args.thetaCoefficientSeqJitter * offs) % (2.0 * Math.PI));
            double tpdfJitter = 0.0;
            double rpdfJitter = 0.0;
            if (0.0 < args.tpdfJitterPicoseconds) {
                double r = GenRandom0to1(gen) + GenRandom0to1(gen) - 1.0;
                tpdfJitter = args.ampTpdfJitter * r;
            }
            if (0.0 < args.rpdfJitterPicoseconds) {
                rpdfJitter = args.ampRpdfJitter * (GenRandom0to1(gen) * 2.0 - 1.0);
            }
            double jitter = seqJitter + tpdfJitter + rpdfJitter;
            return jitter;
        }

        private void PrepareResamplePosArray(
                USWorkerArgs args,
                int sampleRateFrom,
                int sampleRateTo,
                int sampleTotalFrom,
                int sampleTotalTo,
                int[] resamplePosArray,
                double[] fractionArray) {

            // resamplePosArrayとfractionArrayにジッターを付加する

            for (int i = 0; i < sampleTotalTo; ++i) {
                double resamplePos = (double)i * sampleRateFrom / sampleRateTo +
                    GenerateJitter(args, i);

                /* -0.5 <= fraction<+0.5になるようにresamplePosを選ぶ。
                 * 最後のほうで範囲外を指さないようにする。
                 */
                int resamplePosI = (int)(resamplePos + 0.5);

                if (resamplePosI < 0) {
                    resamplePosI = 0;
                }
                
                if (sampleTotalFrom <= resamplePosI) {
                    resamplePosI = sampleTotalFrom - 1;
                }
                double fraction = resamplePos - resamplePosI;

                resamplePosArray[i] = resamplePosI;
                fractionArray[i]    = fraction;
            }
        }

        private void m_USAQworker_DoWork(object sender, DoWorkEventArgs e) {
            // System.Threading.Thread.CurrentThread.Priority = System.Threading.ThreadPriority.Lowest;

            USWorkerArgs args = (USWorkerArgs)e.Argument;

            PcmData pcmDataIn = ReadWavFile(args.inputPath);
            if (null == pcmDataIn) {
                e.Result = string.Format("WAVファイル 読み込み失敗: {0}", args.inputPath);
                return;
            }

            // ファイル読み込み完了。
            if (args.addJitter) {
                // ジッター負荷の場合、サンプリング周波数は変更しない。
                args.resampleFrequency = pcmDataIn.SampleRate;
            }

            if (args.resampleFrequency < pcmDataIn.SampleRate) {
                e.Result = string.Format("エラー: ダウンサンプルは対応していません {0} from={1} to={2}",
                    args.inputPath, pcmDataIn.SampleRate, args.resampleFrequency);
                return;
            }
            if (0x7fff0000L < pcmDataIn.NumFrames * 4 * pcmDataIn.NumChannels * args.resampleFrequency / pcmDataIn.SampleRate) {
                e.Result = string.Format("エラー: リサンプル後のファイルサイズが2GBを超えそうなので中断しました {0}",
                    args.inputPath);
                return;
            }

            m_USAQworker.ReportProgress(1);

            pcmDataIn = pcmDataIn.BitsPerSampleConvertTo(32, PcmData.ValueRepresentationType.SFloat);
            PcmData pcmDataOut = new PcmData();
            pcmDataOut.CopyFrom(pcmDataIn);
            int sampleTotalTo = (int)(args.resampleFrequency * pcmDataIn.NumFrames / pcmDataIn.SampleRate);
            {   // PcmDataOutのサンプルレートとサンプル数を更新する。
                byte[] outSampleArray = new byte[(long)sampleTotalTo * pcmDataOut.NumChannels * 4];
                pcmDataOut.SetSampleArray(sampleTotalTo, outSampleArray);
                pcmDataOut.SampleRate = args.resampleFrequency;
                outSampleArray = null;
            }

            // 再サンプルテーブル作成
            args.resamplePosArray = null;
            args.fractionArray = null;
            if (args.addJitter) {
                // ジッター付加の場合、サンプルレートは変更しない。
                args.resamplePosArray = new int[pcmDataIn.NumFrames];
                args.fractionArray    = new double[pcmDataIn.NumFrames];
                /*
                 sampleRate        == 96000 Hz
                 jitterFrequency   == 50 Hz
                 jitterPicoseconds == 1 ps の場合

                 サンプル位置posのθ= 2 * PI * pos * 50 / 96000 (ラジアン)

                 サンプル間隔= 1/96000秒 = 10.4 μs
             
                 1ms = 10^-3秒
                 1μs= 10^-6秒
                 1ns = 10^-9秒
                 1ps = 10^-12秒

                  1psのずれ                     x サンプルのずれ
                 ───────────── ＝ ─────────
                  10.4 μs(1/96000)sのずれ      1 サンプルのずれ

                 1psのサンプルずれA ＝ 10^-12 ÷ (1/96000) (サンプルのずれ)
             
                 サンプルを採取する位置= pos + Asin(θ)
             
                 */

                args.thetaCoefficientSeqJitter = 2.0 * Math.PI * args.sequentialJitterFrequency / pcmDataIn.SampleRate;
                args.ampSeqJitter = 1.0e-12 * pcmDataIn.SampleRate * args.sequentialJitterPicoseconds;
                args.ampTpdfJitter = 1.0e-12 * pcmDataIn.SampleRate * args.tpdfJitterPicoseconds;
                args.ampRpdfJitter = 1.0e-12 * pcmDataIn.SampleRate * args.rpdfJitterPicoseconds;

                PrepareResamplePosArray(
                    args, pcmDataIn.SampleRate, pcmDataOut.SampleRate,
                    (int)pcmDataIn.NumFrames, sampleTotalTo,
                    args.resamplePosArray, args.fractionArray);
            }

            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();

            int hr = 0;
            if (args.device == ProcessDevice.Gpu) {
                hr = GpuUpsample(args, pcmDataIn, pcmDataOut);
            } else {
                hr = CpuUpsample(args, pcmDataIn, pcmDataOut);
            }

            // args.resamplePosArrayは中でコピーされるのでここで不要になる。
            args.resamplePosArray = null;
            args.fractionArray = null;

            if (m_USAQworker.CancellationPending) {
                e.Result = string.Format("キャンセル完了。");
                e.Cancel = true;
                return;
            }
            if (hr < 0) {
                e.Result = string.Format("Upsample エラー 0x{0:X8}", hr);
                return;
            }
            sw.Stop();

            // 成功した。レベル制限する。
            float scale = pcmDataOut.LimitLevelOnFloatRange();

            if (args.outputVRT != PcmData.ValueRepresentationType.SFloat) {
                // ビットフォーマット変更。
                pcmDataOut = pcmDataOut.BitsPerSampleConvertTo(args.outputBitsPerSample, args.outputVRT);
            }

            try {
                WriteWavFile(pcmDataOut, args.outputPath);
            } catch (IOException ex) {
                // 書き込みエラー。
                e.Result = ex.ToString();
                return;
            }

            e.Result = string.Format("書き込み成功。処理時間 {0}秒\r\n",
                sw.ElapsedMilliseconds * 0.001);
            if (scale < 1.0f) {
                e.Result = string.Format("書き込み成功。処理時間 {0}秒。" +
                    "レベルオーバーのため音量調整{1}dB({2}倍)しました。\r\n",
                    sw.ElapsedMilliseconds * 0.001,
                    20.0 * Math.Log10(scale), scale);
            }
            m_USAQworker.ReportProgress(100);
        }

        private void buttonUSAbort_Click(object sender, RoutedEventArgs e) {
            m_USAQworker.CancelAsync();
            buttonUSAbort.IsEnabled = false;
        }

        private void m_USAQworker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
            progressBarUS.Value = 0;
            buttonUSAbort.IsEnabled = false;
            buttonUSOutputStart.IsEnabled = true;
            buttonUSBrowseOpen.IsEnabled = true;
            buttonUSBrowseSaveAs.IsEnabled = true;

            progressBarAQ.Value = 0;
            buttonAQOutputStart.IsEnabled = true;
            buttonAQBrowseOpen.IsEnabled = true;
            buttonAQBrowseSaveAs.IsEnabled = true;
            buttonAQAbort.IsEnabled = false;

            if (e.Cancelled) {
                textBoxUSResult.Text += string.Format("処理中断。\r\n");
                textBoxAQResult.Text += string.Format("処理中断。\r\n");
            } else {
                string result = (string)e.Result;
                textBoxUSResult.Text += string.Format("結果: {0}\r\n", result);
                textBoxAQResult.Text += string.Format("結果: {0}\r\n", result);
            }
            textBoxUSResult.ScrollToEnd();
            textBoxAQResult.ScrollToEnd();
        }

        //////////////////////////////////////////////////////////////////////////////////////////
        // 互換性実験

        [DllImport("WWDirectDrawTest.dll")]
        private extern static int
        WWDirectDrawTest_Test();

        private void buttonCompatibility_Click(object sender, RoutedEventArgs e) {
            int hr = WWDirectDrawTest_Test();
            textBoxCompatibility.Text += string.Format("DirectDraw PrimarySurface Lock {0:X8} {1}\r\n",
                hr, hr==0 ? "成功" : "失敗");
        }

        //////////////////////////////////////////////////////////////////////////////////////////
        // FIR

        private const int m_firTapN = 4096;
        private double [] m_freqResponse20to20kLog;

        private void InitializeFir() {
            m_freqResponse20to20kLog = new double[512];
            for (int i=0; i < m_freqResponse20to20kLog.Length; ++i) {
                m_freqResponse20to20kLog[i] = 1;
            }
        }

        /// <summary>
        /// m_freqResponse20to20kLogの最新状態を画面表示する。
        /// </summary>
        private void UpdateFreqLine() {
            double width = rectangleFreq.Width;
            double height = rectangleFreq.Height;

            freqLine.Points.Clear();
            for (int i=0; i < m_freqResponse20to20kLog.Length; ++i) {
                // 周波数軸は対数なので、そのまま等間隔にプロットする

                double x = width * i / m_freqResponse20to20kLog.Length;

                double v = m_freqResponse20to20kLog[i];
                if (v < 0.25) {
                    v = 0.25;
                }
                if (v > 4) {
                    v = 4;
                }
                double y = height / 2 - height / 4 * Math.Log(v, 2);

                if (x < width) {
                    var point = new Point(x, y);
                    freqLine.Points.Add(point);
                }
            }
        }

        /// <summary>
        /// 周波数グラフからIDFT入力パラメータ配列を作成。
        /// </summary>
        /// <returns></returns>
        private double[] FreqGraphToIdftInput(int sampleRate) {
            System.Diagnostics.Debug.Assert((m_firTapN&1) == 0);

            var result = new double[m_firTapN];
            result[0] = GetAmplitudeOnFreq(0);
            for (int i=1; i <= result.Length / 2; ++i) {
                // 周波数は、リニアスケール
                // i==result.Length/2のとき freq = sampleRate/2 これが最大周波数。
                // 左右対称な感じで折り返す。
                var freq = (double)i * sampleRate/2 / (result.Length/2);

                var amp = GetAmplitudeOnFreq(freq);
                result[i] = amp;
                result[result.Length-i] = amp;
            }
            return result;
        }

        /// <summary>
        /// グラフから読み取る。周波数→振幅
        /// </summary>
        private double GetAmplitudeOnFreq(double freq) {
            int width = m_freqResponse20to20kLog.Length;
            double lowestFreq = 20;
            double highestFreq = 20000;
            double octave = 3;
            if (freq < lowestFreq) {
                return m_freqResponse20to20kLog[0];
            }
            if (highestFreq <= freq) {
                return m_freqResponse20to20kLog[m_freqResponse20to20kLog.Length - 1];
            }

            int x = (int)((Math.Log10(freq) - Math.Log10(lowestFreq)) * width / octave);
            if (x < 0) {
                System.Diagnostics.Debug.Assert(0 <= x);
                return m_freqResponse20to20kLog[0];
            }
            if (width <= x) {
                // 起こらないのではないだろうか？
                return m_freqResponse20to20kLog[m_freqResponse20to20kLog.Length - 1];
            }

            return m_freqResponse20to20kLog[x];
        }

        Point m_prevFirPress;

        private double PressYToMag(double y) {
            // yが最大 = 12dB  = 4x    = 2^2
            // yが中央 = -12dB = 1x    = 2^(0)
            // yが最小 = -12dB = 0.25x = 2^(-2)
            double vPowMag = 2;

            double yReg = (rectangleFreq.Height * 0.5 - y) / (rectangleFreq.Height * 0.5);
            double v = Math.Pow(2.0, vPowMag * yReg);
            if (v < Math.Pow(2.0, -vPowMag)) {
                v = Math.Pow(2.0, -vPowMag);
            }
            if (Math.Pow(2.0, vPowMag) < v) {
                v = Math.Pow(2.0, vPowMag);
            }

            return v;
        }

        /// <summary>
        /// 周波数-振幅グラフm_freqResponseに(px0, y0)-(px1, y1)の線を引く。
        /// </summary>
        private void UpdateFreqResponse(double px0, double y0, double px1, double y1) {
            int x0 = (int)(px0 * m_freqResponse20to20kLog.Length / rectangleFreq.Width);
            int x1 = (int)(px1 * m_freqResponse20to20kLog.Length / rectangleFreq.Width);
            if (x0 != x1) {
                double dy = (y1 - y0) / ((double)x1 - x0);
                if (x0 < x1) {
                    for (int x=x0; x < x1; ++x) {
                        m_freqResponse20to20kLog[x] = y0 + dy * (x-x0);
                    }
                }
                if (x1 < x0) {
                    for (int x=x0; x > x1; --x) {
                        m_freqResponse20to20kLog[x] = y0 + dy * (x-x0);
                    }
                }
            }
            m_freqResponse20to20kLog[x1] = y1;
        }

        private void firCanvasMouseUpdate(int mx, int my) {
            System.Diagnostics.Debug.Assert(rectangleFreq.Width == m_freqResponse20to20kLog.Length);

            var pos = new Point(mx - Canvas.GetLeft(rectangleFreq), my - Canvas.GetTop(rectangleFreq));

            if (pos.X < 0 || rectangleFreq.Width <= pos.X ||
                m_prevFirPress.X < 0 || rectangleFreq.Width <= m_prevFirPress.X) {
                m_prevFirPress = pos;
                return;
            }

            double prevY = PressYToMag(m_prevFirPress.Y);
            double nowY  = PressYToMag(pos.Y);

            UpdateFreqResponse(m_prevFirPress.X, prevY, pos.X, nowY);

            m_prevFirPress = pos;
            UpdateFreqLine();
        }

        private void buttonFirFlat_Click(object sender, RoutedEventArgs e) {
            for (int i=0; i < m_freqResponse20to20kLog.Length; ++i) {
                m_freqResponse20to20kLog[i] = 1.0;
            }
            UpdateFreqLine();
        }


        private void canvas1_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e) {
            System.Diagnostics.Debug.Assert(rectangleFreq.Width == m_freqResponse20to20kLog.Length);

            if (e.LeftButton != System.Windows.Input.MouseButtonState.Pressed) {
                return;
            }

            var pos = e.GetPosition(canvas1);
            m_prevFirPress = new Point(pos.X - Canvas.GetLeft(rectangleFreq), pos.Y - Canvas.GetTop(rectangleFreq));
            firCanvasMouseUpdate((int)pos.X, (int)pos.Y);
        }

        private void canvas1_MouseMove(object sender, System.Windows.Input.MouseEventArgs e) {
            if (e.LeftButton != System.Windows.Input.MouseButtonState.Pressed) {
                return;
            }

            var pos = e.GetPosition(canvas1);
            firCanvasMouseUpdate((int)pos.X, (int)pos.Y);
        }

        private void buttonFirDo_Click(object sender, RoutedEventArgs e) {
            var args = new FirWorkerArgs();
            args.inputPath = textBoxFirInputPath.Text;
            args.outputPath = textBoxFirOutputPath.Text;

            textBoxFirLog.Text += string.Format("開始。{0} → {1}\r\n", args.inputPath, args.outputPath);
            buttonFirDo.IsEnabled = false;
            m_FirWorker.RunWorkerAsync(args);
        }

        void m_FirWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
            string s = (string)e.Result;
            textBoxFirLog.Text += s + "\r\n";
            progressBarFir.Value = 0;
            buttonFirDo.IsEnabled = true;
        }

        void m_FirWorker_ProgressChanged(object sender, ProgressChangedEventArgs e) {
            progressBarFir.Value = e.ProgressPercentage;
        }

        struct FirWorkerArgs {
            public string inputPath;
            public string outputPath;
        }

        void m_FirWorker_DoWork(object sender, DoWorkEventArgs e) {
            FirWorkerArgs args = (FirWorkerArgs)e.Argument;

            var dft = new WWDirectComputeCS.WWDftCpu();

            // pcmファイルを読み込んでサンプル配列pcm1chを作成。
            PcmData pcmDataIn = null;
            try {
                pcmDataIn = ReadWavFile(args.inputPath);
            } catch (IOException ex) {
                e.Result = string.Format("WAVファイル {0} 読み込み失敗\r\n{1}", args.inputPath, ex);
                return;
            }
            if (null == pcmDataIn) {
                e.Result = string.Format("WAVファイル {0} 読み込み失敗", args.inputPath);
                return;
            }
            pcmDataIn = pcmDataIn.BitsPerSampleConvertTo(32, PcmData.ValueRepresentationType.SFloat);

            var from = FreqGraphToIdftInput(pcmDataIn.SampleRate);

            /*
            for (int i=0; i < from.Length / 2; ++i) {
                System.Console.WriteLine("G{0}=({1} {2})", i, from[i * 2], from[i * 2 + 1]);
            }
            System.Console.WriteLine("");
            */

            double [] idftResult;
            dft.Idft1d(from, out idftResult);

            /*
            for (int i=0; i < from.Length / 2; ++i) {
                System.Console.WriteLine("S{0}=({1} {2})", i, idftResult[i * 2], idftResult[i * 2 + 1]);
            }
            System.Console.WriteLine("");
            */

            /*
            double [] dftResult;
            dft.Dft1d(from, out dftResult);

            for (int i=0; i < from.Length / 2; ++i) {
                System.Console.WriteLine("G{0}=({1} {2})", i, dftResult[i * 2], dftResult[i * 2 + 1]);
            }
            System.Console.WriteLine("");
            */

            double [] window;
            WWWindowFunc.BlackmanWindow(idftResult.Length / 2 - 1, out window);
            /*
            for (int i=0; i < window.Length; ++i) {
                System.Console.WriteLine("Window {0:D2} {1}", i, window[i]);
            }
            System.Console.WriteLine("");
            */

            double [] coeff;
            dft.IdftToFirCoeff(idftResult, window, out coeff);
            /*
            for (int i=0; i < coeff.Length; ++i) {
                System.Console.WriteLine("coeff {0:D2} {1}", i, coeff[i]);
            }
            System.Console.WriteLine("");
            */

            PcmData pcmDataOut = new PcmData();
            pcmDataOut.CopyFrom(pcmDataIn);

            for (int ch=0; ch < pcmDataOut.NumChannels; ++ch) {
                // 全てのチャンネルでループ。

                var pcm1ch = new double[pcmDataOut.NumFrames];
                for (long i=0; i < pcm1ch.Length; ++i) {
                    pcm1ch[i] = pcmDataOut.GetSampleValueInFloat(ch, i);
                }

                // 少しずつFIRする。
                var fir = new WWFirCpu();
                fir.Setup(coeff, pcm1ch);

                const int FIR_SAMPLE = 65536;
                for (int offs=0; offs < pcm1ch.Length; offs += FIR_SAMPLE) {
                    int nSample = FIR_SAMPLE;
                    if (pcm1ch.Length < offs + nSample) {
                        nSample = pcm1ch.Length - offs;
                    }
                    var pcmFir = new double[nSample];
                    fir.Do(offs - window.Length / 2, pcmFir.Length, pcmFir);

                    // 結果を出力に書き込む。
                    for (long i=0; i < pcmFir.Length; ++i) {
                        pcmDataOut.SetSampleValueInFloat(ch, i + offs, (float)pcmFir[i]);
                    }

                    // 進捗Update。
                    int percentage = (int)(
                        (100L * ch  / pcmDataOut.NumChannels) +
                        (100L * (offs+1) / pcm1ch.Length / pcmDataOut.NumChannels));
                    m_FirWorker.ReportProgress(percentage);
                }
                fir.Unsetup();
            }

            // 音量制限処理。
            pcmDataOut.LimitLevelOnFloatRange();

            bool writeResult = false;
            try {
                writeResult = WriteWavFile(pcmDataOut, args.outputPath);
            } catch (IOException ex) {
                e.Result = string.Format("WAVファイル書き込み失敗: {0}\r\n{1}", args.outputPath, ex);
                return;
            }
            if (!writeResult) {
                e.Result = string.Format("WAVファイル書き込み失敗: {0}", args.outputPath);
                return;
            }

            e.Result = string.Format("WAVファイル書き込み成功: {0}", args.outputPath);
            return;
        }

        private void buttonFirInputBrowse_Click(object sender, RoutedEventArgs e) {
            string fileName = BrowseOpenFile();
            if (0 < fileName.Length) {
                textBoxFirInputPath.Text = fileName;
            }
        }

        private void buttonFirOutputBrowse_Click(object sender, RoutedEventArgs e) {
            string fileName = BrowseSaveFile();
            if (0 < fileName.Length) {
                textBoxFirOutputPath.Text = fileName;
            }
        }

        private void buttonFirSmooth_Click(object sender, RoutedEventArgs e) {

            double prev = m_freqResponse20to20kLog[0];
            for (int j=0; j < 10; ++j) {
                for (int i=1; i < m_freqResponse20to20kLog.Length - 1; ++i) {
                    double t = m_freqResponse20to20kLog[i];
                    m_freqResponse20to20kLog[i] =
                        (prev +
                         m_freqResponse20to20kLog[i] +
                         m_freqResponse20to20kLog[i + 1]) / 3.0;
                    prev = t;
                }
            }
            UpdateFreqLine();
        }


    }
}
