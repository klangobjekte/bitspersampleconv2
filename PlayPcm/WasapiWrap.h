#pragma once

#include <Windows.h>
#include <MMDeviceAPI.h>
#include <AudioClient.h>
#include <AudioPolicy.h>
#include <vector>

#define WW_DEVICE_NAME_COUNT (256)

struct WWDeviceInfo {
    int id;
    wchar_t name[WW_DEVICE_NAME_COUNT];

    WWDeviceInfo(void) {
        id = -1;
        name[0] = 0;
    }

    WWDeviceInfo(int id, const wchar_t * name);
};

struct WWPcmData {
    int bitsPerSample;
    int nSamplesPerSec;
    int nChannels;
    int  nFrames;
    int  posFrame;

    BYTE *stream;

    void Init(int samples);
    void Term(void);

    ~WWPcmData(void);
};

struct WWSetupArg {
    int bitsPerSample;
    int nSamplesPerSec;
    int nChannels;
    int latencyInMillisec;
};

class WasapiWrap {
public:
    WasapiWrap(void);
    ~WasapiWrap(void);

    HRESULT Init(void);
    void Term(void);

    // device enumeration
    HRESULT DoDeviceEnumeration(void);
    int GetDeviceCount(void);
    bool GetDeviceName(int id, LPWSTR name, size_t nameBytes);

    // if you want to unchoose device, call ChooseDevice(-1)
    HRESULT ChooseDevice(int id);

    HRESULT Setup(const WWSetupArg & arg);
    void Unsetup(void);

    void SetOutputData(WWPcmData &pcmData);

    HRESULT Start(void);

    bool Run(int millisec);

    void Stop(void);

    int GetPosFrame(void);
    int GetTotalFrameNum(void);
    bool SetPosFrame(int v);

private:
    std::vector<WWDeviceInfo> m_deviceInfo;
    IMMDeviceCollection       *m_deviceCollection;
    IMMDevice                 *m_deviceToUse;

    HANDLE       m_shutdownEvent;
    HANDLE       m_audioSamplesReadyEvent;

    IAudioClient *m_audioClient;
    int          m_frameBytes;
    UINT32       m_bufferFrameNum;
    int          m_deviceBitsPerSample;
    int          m_dataBitsPerSample;
    int          m_sampleRate;

    IAudioRenderClient *m_renderClient;
    HANDLE       m_renderThread;
    WWPcmData    *m_pcmData;
    HANDLE       m_mutex;
    int          m_footerCount;
    bool         m_coInitializeSuccess;

    static DWORD WINAPI RenderEntry(LPVOID lpThreadParameter);

    DWORD RenderMain(void);

    bool AudioSamplesReadyProc(void);
};

