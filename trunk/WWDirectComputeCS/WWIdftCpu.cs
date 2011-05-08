﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WWDirectComputeCS {
    public class WWDftCpu {

        /// <summary>
        /// 1次元DFT
        /// </summary>
        /// <param name="from">r0,i0,r1,i1,…,r(N-1),i(N-1)(rは実数、iは虚数)のように隣接する2要素一組が1個の複素数データとなるように並べて渡す</param>
        /// <param name="to">1次元DFT結果が複素数で出てくる</param>
        /// <returns>0:成功。0以外:失敗。</returns>
        public int Dft1d(double[] from, out double[] to) {
            /*
             * W=e^(j*2pi/N)
             * Gp = (1/N) * seriesSum(Sk * W^(k*p), k, 0, N-1) (p=0,1,2,…,(N-1))
             * 
             * from == Sk
             * to   == Gp
             */

            // 要素数N
            int n = from.Length / 2;

            to = new double[n * 2];

            // Wのテーブル
            var w = new double[n * 2];
            for (int i=0; i < n; ++i) {
                w[i * 2 + 0] = Math.Cos(-i * 2.0 * Math.PI / n);
                w[i * 2 + 1] = Math.Sin(-i * 2.0 * Math.PI / n);
            }

            for (int p=0; p < n; ++p) {
                double gr = 0.0;
                double gi = 0.0;
                for (int k=0; k < n; ++k) {
                    int posSr = k * 2;
                    int posWr = 2 * ((p * k) % n);
                    double sR = from[posSr + 0];
                    double sI = from[posSr + 1];
                    double wR = w[posWr + 0];
                    double wI = w[posWr + 1];
                    gr += sR * wR - sI * wI;
                    gi += sR * wI + sI * wR;
                }
                to[p * 2 + 0] = gr / n;
                to[p * 2 + 1] = gi / n;
            }

            return 0;
        }

        /// <summary>
        /// 1次元IDFT
        /// </summary>
        /// <param name="from">r0,i0,r1,i1,…,r(N-1),i(N-1)(rは実数、iは虚数)のように隣接する2要素一組が1個の複素数データとなるように並べて渡す</param>
        /// <param name="to">1次元IDFT結果が複素数で出てくる</param>
        /// <returns>0:成功。0以外:失敗。</returns>
        public int Idft1d(double[] from, out double[] to) {
            /*
             * W=e^(-j*2pi/N)
             * Sk= seriesSum([ Gp * W^(-k*p) ], p, 0, N-1) (k=0,1,2,…,(N-1))
             * 
             * from == Gp
             * to   == Sk
             */

            // 要素数N
            int n = from.Length/2;

            to = new double[n*2];

            // Wのテーブル
            var w = new double[n*2];
            for (int i=0; i < n; ++i) {
                w[i * 2 + 0] = Math.Cos(i * 2.0 * Math.PI / n);
                w[i * 2 + 1] = Math.Sin(i * 2.0 * Math.PI / n);
            }

            for (int k=0; k < n; ++k) {
                double sr = 0.0;
                double si = 0.0;
                for (int p=0; p < n; ++p) {
                    int posGr = p * 2;
                    int posWr = 2 * ((p * k) % n);
                    double gR = from[posGr + 0];
                    double gI = from[posGr + 1];
                    double wR = w[posWr + 0];
                    double wI = w[posWr + 1];
                    sr += gR * wR - gI * wI;
                    si += gR * wI + gI * wR;
                }
                to[k * 2 + 0] = sr;
                to[k * 2 + 1] = si;
            }

            return 0;
        }

        /// <summary>
        /// IDFT結果をインパルス応答に変換する。
        /// </summary>
        /// <param name="from">IDFT結果(複素数。r0,i0,r1,i1…の順でデータを並べる)。要素数2n(nは2の倍数)</param>
        /// <param name="to">インパルス応答。要素数n-1</param>
        /// <returns>0:成功。0以外:失敗。</returns>
        public int IdftToImpulseResponse(double[] from, out double[] to) {
            int n = from.Length/2;
            System.Diagnostics.Debug.Assert(0 < n && ((n & 1) == 0));
            to = new double[n -1];

            for (int i=0; i < n / 2; ++i) {
                to[i] = from[2*(n / 2 -1 -i)] / n;
            }
            for (int i=1; i < n / 2; ++i) {
                to[n / 2 - 1 + i] = from[2 * i] / n;
            }

            return 0;
        }

        /// <summary>
        /// IDFT結果からFIR coeffを作成。
        /// </summary>
        /// <param name="idft">IDFT結果(複素数。r0,i0,r1,i1…の順でデータを並べる)。要素数2n(4の倍数)</param>
        /// <param name="window">窓関数。要素数n-1</param>
        /// <param name="coeff">[out]FIR coeff。要素数n-1</param>
        /// <returns>0:成功。0以外:失敗。</returns>
        public int IdftToFirCoeff(double[] idftArray, double[] window, out double[] coeff) {
            int n = idftArray.Length / 2;
            System.Diagnostics.Debug.Assert(0 < n && ((n & 1) == 0));
            System.Diagnostics.Debug.Assert(window != null && window.Length == n - 1);

            int result = IdftToImpulseResponse(idftArray, out coeff);
            if (0 != result) {
                return result;
            }

            for (int i=0; i < window.Length; ++i) {
                coeff[i] *= window[i];
            }
            return 0;
        }
    }
}
