﻿using System;

namespace WWAudioFilter {
    class LowpassFilter : FilterBase {
        // フィルターの長さ-1。2のべき乗の値である必要がある
        private const int FILTER_LENP1 = 65536;
        private const int FILTER_DELAY = FILTER_LENP1/2;

        // 2のべき乗の値である必要がある
        private const int FFT_LEN    = FILTER_LENP1*4;

        public int SampleRate { get; set; }
        public double CutoffFrequency { get; set; }

        private WWComplex [] mFilterFreq;
        private double [] mIfftAddBuffer;
        private bool mFirstFilterDo;

        private static bool IsPowerOfTwo(int x) {
            return (x != 0) && ((x & (x - 1)) == 0);
        }

        public LowpassFilter(double cutoffFrequency)
                : base(FilterType.LPF) {
            if (cutoffFrequency < 0.0) {
                throw new ArgumentOutOfRangeException();
            }
            CutoffFrequency = cutoffFrequency;

            System.Diagnostics.Debug.Assert(IsPowerOfTwo(FILTER_LENP1) && IsPowerOfTwo(FFT_LEN) && FILTER_LENP1 < FFT_LEN);
        }

        public override PcmFormat Setup(PcmFormat inputFormat) {
            SampleRate = inputFormat.SampleRate;
            DesignCutoffFilter();
            mIfftAddBuffer = null;
            mFirstFilterDo = true;

            return new PcmFormat(inputFormat);
        }

        public override void FilterStart() {
            base.FilterStart();

            mIfftAddBuffer = null;
            mFirstFilterDo = true;
        }

        public override void FilterEnd() {
            base.FilterEnd();

            mIfftAddBuffer = null;
            mFirstFilterDo = true;
        }

        public override string ToDescriptionText() {
            return string.Format("LPF : Cutoff={0}Hz", CutoffFrequency);
        }

        public override string ToSaveText() {
            return string.Format("{0}", CutoffFrequency);
        }

        public static FilterBase Restore(string[] tokens) {
            if (tokens.Length != 2) {
                return null;
            }

            double cutoffFrequency;
            if (!Double.TryParse(tokens[1], out cutoffFrequency) || cutoffFrequency <= 0) {
                return null;
            }

            return new LowpassFilter(cutoffFrequency);
        }

        public override long NumOfSamplesNeeded() {
            return FFT_LEN - FILTER_LENP1;
        }

        private void DesignCutoffFilter() {
            var fromF = new WWComplex[FILTER_LENP1];

            // 50次のバターワースフィルター
            double orderX2 = 2.0 * 50;

            double cutoffRatio = CutoffFrequency / (SampleRate/2);

            // フィルタのF特
            fromF[0].real = 1.0f;
            for (int i=1; i <= FILTER_LENP1 / 2; ++i) {
                double omegaRatio = i * (1.0 / (FILTER_LENP1 / 2));
                fromF[i].real = Math.Sqrt(1.0 / (1.0 + Math.Pow(omegaRatio / cutoffRatio, orderX2)));
            }
            for (int i=1; i < FILTER_LENP1 / 2; ++i) {
                fromF[FILTER_LENP1 - i].real = fromF[i].real;
            }

            // IFFTでfromFをfromTに変換
            var fromT   = new WWComplex[FILTER_LENP1];
            {
                var fft = new WWRadix2Fft(FILTER_LENP1);
                fft.Fft(fromF, fromT);

                double compensation = 1.0 / (FILTER_LENP1 * cutoffRatio);
                for (int i=0; i < FILTER_LENP1; ++i) {
                    fromT[i].Set(
                            fromT[i].real      * compensation,
                            fromT[i].imaginary * compensation);
                }
            }
            fromF = null;

            // fromTの中心がFILTER_LENGTH/2番に来るようにする。
            // delayT[0]のデータはfromF[FILTER_LENGTH/2]だが、非対称になるので入れない
            // このフィルタの遅延はFILTER_LENGTH/2サンプルある

            var delayT = new WWComplex[FILTER_LENP1];
            for (int i=1; i < FILTER_LENP1 / 2; ++i) {
                delayT[i] = fromT[i + FILTER_LENP1 / 2];
            }
            for (int i=0; i < FILTER_LENP1 / 2; ++i) {
                delayT[i + FILTER_LENP1 / 2] = fromT[i];
            }
            fromT = null;

            // Kaiser窓をかける α=6.0
            double [] w;
            WWWindowFunc.KaiserWindow(FILTER_LENP1 + 1, 6.0, out w);
            for (int i=0; i < FILTER_LENP1; ++i) {
                delayT[i].Mul(w[i]);
            }

            var delayTL = new WWComplex[FFT_LEN];
            for (int i=0; i < delayT.Length; ++i) {
                delayTL[i] = delayT[i];
            }
            delayT = null;

            // できたフィルタをFFTする
            var delayF = new WWComplex[FFT_LEN];
            {
                var fft = new WWRadix2Fft(FFT_LEN);
                fft.Fft(delayTL, delayF);

                for (int i=0; i < FFT_LEN; ++i) {
                    delayF[i].Mul(cutoffRatio);
                }
            }
            delayTL = null;

            mFilterFreq = delayF;
        }

        public override double[] FilterDo(double[] inPcm) {
            System.Diagnostics.Debug.Assert(inPcm.LongLength <= NumOfSamplesNeeded());

            // Overlap and add continuous FFT

            var inTime = new WWComplex[FFT_LEN];
            for (int i=0; i < inPcm.LongLength; ++i) {
                inTime[i] = new WWComplex(inPcm[i], 0.0);
            }

            // FFTでinTimeをinFreqに変換
            var inFreq = new WWComplex[FFT_LEN];
            {
                var fft = new WWRadix2Fft(FFT_LEN);
                fft.Fft(inTime, inFreq);
            }
            inTime = null;

            // FFT後、フィルターHの周波数ドメインデータを掛ける
            for (int i=0; i < FFT_LEN; ++i) {
                inFreq[i].Mul(mFilterFreq[i]);
            }

            // inFreqをoutTimeに変換
            var outTime = new WWComplex[FFT_LEN];
            {
                var outTimeS = new WWComplex[FFT_LEN];
                var fft = new WWRadix2Fft(FFT_LEN);
                fft.Fft(inFreq, outTimeS);

                double compensate = 1.0 / FFT_LEN;
                for (int i=0; i < outTimeS.Length; ++i) {
                    outTimeS[i].Set(
                            outTimeS[i].real * compensate,
                            outTimeS[i].imaginary * compensate);
                }

                for (int i=0; i < outTime.Length; ++i) {
                    int pos = (outTime.Length - i) % outTime.Length;
                    outTime[i] = outTimeS[pos];
                }
            }
            inFreq = null;

            double [] outReal;
            if (mFirstFilterDo) {
                // 最初のFilterDo()のとき、フィルタの遅延サンプル数だけ先頭サンプルを削除する。
                outReal = new double[NumOfSamplesNeeded() - FILTER_DELAY];
                for (int i=0; i < outReal.Length; ++i) {
                    outReal[i] = outTime[i+FILTER_DELAY].real;
                }
                mFirstFilterDo = false;
            } else {
                outReal = new double[NumOfSamplesNeeded()];
                for (int i=0; i < outReal.Length; ++i) {
                    outReal[i] = outTime[i].real;
                }
            }

            // 前回のIFFT結果の最後のFILTER_LENGTH-1サンプルを先頭に加算する
            if (null != mIfftAddBuffer) {
                for (int i=0; i < mIfftAddBuffer.Length; ++i) {
                    outReal[i] += mIfftAddBuffer[i];
                }
            }

            // 今回のIFFT結果の最後のFILTER_LENGTH-1サンプルをmIfftAddBufferとして保存する
            mIfftAddBuffer = new double[FILTER_LENP1];
            for (int i=0; i < mIfftAddBuffer.Length; ++i) {
                mIfftAddBuffer[i] = outTime[outTime.Length - mIfftAddBuffer.Length + i].real;
            }
            outTime = null;

            return outReal;
        }
    }
}