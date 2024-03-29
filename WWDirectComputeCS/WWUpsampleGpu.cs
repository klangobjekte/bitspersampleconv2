﻿using System.Runtime.InteropServices;

namespace WWDirectComputeCS {
    public class WWUpsampleGpu {
        [DllImport("WWDirectComputeDLL.dll")]
        private extern static int
        WWDCUpsample_Init(
            int convolutionN,
            float[] sampleFrom,
            int sampleTotalFrom,
            int sampleRateFrom,
            int sampleRateTo,
            int sampleTotalTo);

        [DllImport("WWDirectComputeDLL.dll")]
        private extern static int
        WWDCUpsample_InitWithResamplePosArray(
            int convolutionN,
            float[] sampleFrom,
            int sampleTotalFrom,
            int sampleRateFrom,
            int sampleRateTo,
            int sampleTotalTo,
            int[] resamplePosArray,
            double[] fractionArray);

        [DllImport("WWDirectComputeDLL.dll")]
        private extern static int
        WWDCUpsample_Dispatch(
            int startPos,
            int count);

        [DllImport("WWDirectComputeDLL.dll")]
        private extern static int
        WWDCUpsample_GetResultFromGpuMemory(
            [In, Out] float[] outputTo,
            int outputToElemNum);

        [DllImport("WWDirectComputeDLL.dll")]
        private extern static void
        WWDCUpsample_Term();

        /////////////////////////////////////////////////////////////////////

        /// <returns>HRESULT</returns>
        public int Init(
                int convolutionN,
                float[] sampleFrom,
                int sampleTotalFrom,
                int sampleRateFrom,
                int sampleRateTo,
                int sampleTotalTo) {
            return WWDCUpsample_Init(convolutionN, sampleFrom,
                sampleTotalFrom, sampleRateFrom, sampleRateTo, sampleTotalTo);
        }

        /// <returns>HRESULT</returns>
        public int Init(
                int convolutionN,
                float[] sampleFrom,
                int sampleTotalFrom,
                int sampleRateFrom,
                int sampleRateTo,
                int sampleTotalTo,
                int[] resamplePosArray,
                double[] fractionArray) {
            return WWDCUpsample_InitWithResamplePosArray(convolutionN, sampleFrom,
                sampleTotalFrom, sampleRateFrom, sampleRateTo, sampleTotalTo,
                resamplePosArray, fractionArray);
        }

        /// <summary>
        /// サンプルデータの一部分を処理する。
        /// </summary>
        /// <param name="startPos">処理開始位置 0～sampleTotalTo-1</param>
        /// <param name="count">処理するサンプル数</param>
        /// <returns>HRESULT</returns>
        public int ProcessPortion(
                int startPos,
                int count) {
            return WWDCUpsample_Dispatch(startPos, count);
        }

        /// <summary>
        /// 処理結果をGPUメモリからCPUメモリに持ってくる。
        /// </summary>
        /// <returns>HRESULT</returns>
        public int GetResultFromGpuMemory(
                ref float[] outputTo,
                int outputToElemNum) {
            return WWDCUpsample_GetResultFromGpuMemory(
                outputTo, outputToElemNum);
        }

        public void Term() {
            WWDCUpsample_Term();
        }

    }
}
