﻿// 日本語UTF-8
#pragma once

#include <stdint.h>

enum FlacDecodeResultType {
    /// ヘッダの取得やデータの取得に成功。
    FDRT_Success = 0,

    /// ファイルの最後まで行き、デコードを完了した。もうデータはない。
    FDRT_Completed = 1,

    // 以下、FLACデコードエラー。
    FDRT_DataNotReady               = -2,
    FDRT_WriteOpenFailed            = -3,
    FDRT_FlacStreamDecoderNewFailed = -4,

    FDRT_FlacStreamDecoderInitFailed = -5,
    FDRT_DecorderProcessFailed       = -6,
    FDRT_LostSync                    = -7,
    FDRT_BadHeader                   = -8,
    FDRT_FrameCrcMismatch            = -9,

    FDRT_Unparseable                = -10,
    FDRT_NumFrameIsNotAligned       = -11,
    FDRT_RecvBufferSizeInsufficient = -12,
    FDRT_OtherError                 = -13
};

/// FLACヘッダーを読み込んで、フォーマット情報を取得する。
/// 中のグローバル変数に貯める。APIの設計がスレッドセーフになってないので注意。
/// @return 0以上: 成功。デコーダーIDが戻る。負: FlacDecodeResultType参照。
extern "C" __declspec(dllexport)
int __stdcall
FlacDecodeDLL_DecodeStart(const char *fromFlacPath);

/// FlacDecodeを終了する。(DecodeStartで立てたスレを止めたりする)
/// DecodeStartが失敗を戻しても、成功を戻しても、呼ぶ必要がある。
extern "C" __declspec(dllexport)
void __stdcall
FlacDecodeDLL_DecodeEnd(int id);

/// チャンネル数。
/// DecodeStart成功後に呼ぶことができる。
extern "C" __declspec(dllexport)
int __stdcall
FlacDecodeDLL_GetNumOfChannels(int id);

/// 量子化ビット数。
/// DecodeStart成功後に呼ぶことができる。
extern "C" __declspec(dllexport)
int __stdcall
FlacDecodeDLL_GetBitsPerSample(int id);

/// サンプルレート。
/// DecodeStart成功後に呼ぶことができる。
extern "C" __declspec(dllexport)
int __stdcall
FlacDecodeDLL_GetSampleRate(int id);

/// サンプル(==frame)総数。
/// DecodeStart成功後に呼ぶことができる。
extern "C" __declspec(dllexport)
int64_t __stdcall
FlacDecodeDLL_GetNumSamples(int id);

/// リザルトコード FlacDecodeResultType を取得。
/// ファイルの最後まで行った場合
///   GetLastError==FDRT_Completedで、GetNextPcmDataの戻り値は取得できたフレーム数となる。
extern "C" __declspec(dllexport)
int __stdcall
FlacDecodeDLL_GetLastResult(int id);

/// ブロックサイズを取得。
/// FlacDecodeDLL_GetNextPcmData()のnumFrameはこのサイズの倍数である必要がある。
extern "C" __declspec(dllexport)
int __stdcall
FlacDecodeDLL_GetNumFramesPerBlock(int id);


/// 次のPCMデータをnumFrameサンプルだけbuff_returnに詰める。
/// 最後のデータでなくても、numFrameが取得できないこともあるので注意。
/// @return エラーの場合、-1が戻る。0以上の場合、取得できたサンプル数。FDRT_Completedは、正常終了に分類されている。
/// @retval 0 0が戻った場合、取得できたデータが0サンプルであった(成功)。
extern "C" __declspec(dllexport)
int __stdcall
FlacDecodeDLL_GetNextPcmData(int id, int numFrame, char *buff_return);
