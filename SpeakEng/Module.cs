using System.Runtime.InteropServices;
using System.Text;
namespace SpeakEng
{
    public class audio
    {
        public static WAVEHDR whdr;
        public static WAVEFORMAT format_wave;
        public static WAVEHDR outHdr;
        public static int bufferIn;
        public static int numSamples;
        public static int hWaveOut;
        public const short MMIO_READ = 0x0;
        public const int CALLBACK_FUNCTION = 0x30000;
        public const short WAVE_MAPPED = 0x4;
        public const short MMIO_FINDCHUNK = 0x10;
        public const short MMIO_FINDRIFF = 0x20;
        public const int SEEK_SET = 0;
        public const int SEEK_CUR = 1;
        public struct MMCKINFO
        {
            public int ckid;
            public int ckSize;
            public int fccType;
            public int dwDataOffset;
            public int dwFlags;
        }
        public struct mmioinfo
        {
            public int dwFlags;
            public int fccIOProc;
            public int pIOProc;
            public int wErrorRet;
            public int htask;
            public int cchBuffer;
            public string pchBuffer;
            public string pchNext;
            public string pchEndRead;
            public string pchEndWrite;
            public int lBufOffset;
            public int lDiskOffset;
            public string adwInfo;
            public int dwReserved1;
            public int dwReserved2;
            public int hmmio;
        }
        public struct WAVEFORMAT
        {
            public short wFormatTag;
            public short nChannels;
            public int nSamplesPerSec;
            public int nAvgBytesPerSec;
            public short nBlockAlign;
            public short wBitsPerSample;
            public short cbSize;
        }
        public struct WAVEHDR
        {
            public int lpData;
            public int dwBufferLength;
            public int dwBytesRecorded;
            public int dwUser;
            public int dwFlags;
            public int dwLoops;
            public int lpNext;
            public int Reserved;
        }
        [DllImport("winmm.dll", SetLastError = true)]
        public static extern int waveOutWrite(int hWaveOut, ref WAVEHDR lpWaveOutHdr, int uSize);
        [DllImport("winmm.dll", SetLastError = true)]
        public static extern int waveOutPrepareHeader(int hWaveIn, ref WAVEHDR lpWaveInHdr, int uSize);
        [DllImport("winmm.dll", SetLastError = true)]
        public static extern int mmioRead(int hmmio, int pch, int cch);
        [DllImport("winmm.dll", SetLastError = true)]
        public static extern int waveOutOpen(ref int lphWaveIn, int uDeviceID, ref WAVEFORMAT lpFormat, int dwCallback, int dwInstance, int dwFlags);
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern int GlobalAlloc(int wFlags, int dwBytes);
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern int GlobalLock(int hmem);
        [DllImport("winmm.dll", SetLastError = true)]
        public static extern int mmioAscend(int hmmio, ref MMCKINFO lpck, int uFlags);
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern int GlobalFree(int hmem);
        [DllImport("winmm.dll", SetLastError = true)]
        public static extern int mmioOpenA(string szFileName, ref mmioinfo lpmmioinfo, int dwOpenFlags);
        [DllImport("winmm.dll", SetLastError = true)]
        public static extern int mmioDescend(int hmmio, ref MMCKINFO lpck, int x, int uFlags);
        [DllImport("winmm.dll", SetLastError = true)]
        public static extern int mmioRead(int hmmio, ref WAVEFORMAT pch, int cch);
        [DllImport("winmm.dll", SetLastError = true)]
        public static extern int mmioClose(int hmmio, int uFlags);
        [DllImport("winmm.dll", SetLastError = true)]
        public static extern int mmioStringToFOURCCA(string sz, int uFlags);
        [DllImport("winmm.dll", SetLastError = true)]
        public static extern int mmioDescend(int hmmio, ref MMCKINFO lpck, ref MMCKINFO lpckParent, int uFlags);
        [DllImport("winmm.dll", SetLastError = true)]
        public static extern int mmioSeek(int hmmio, int lOffset, int iOrigin);

        public static void Play(short soundcard)
        {
            int rc = 0;
            int lFlags = 0;
            lFlags = CALLBACK_FUNCTION;
            if (soundcard != -1) lFlags = lFlags | WAVE_MAPPED;
            rc = waveOutOpen(ref hWaveOut, soundcard,            ref format_wave, 0, 0, lFlags);
            if (rc != 0) return;
            outHdr.lpData = bufferIn;
            outHdr.dwBufferLength =            numSamples * format_wave.nBlockAlign;
            outHdr.dwFlags = 0;
            outHdr.dwLoops = 0;
            waveOutPrepareHeader(hWaveOut, ref outHdr,            Marshal.SizeOf(outHdr));
            waveOutWrite(hWaveOut, ref outHdr, Marshal.SizeOf(outHdr));
        }
        static int hmem = 0;
        public static MMCKINFO mmckinfoParentIn = new MMCKINFO();
        public static MMCKINFO mmckinfoSubchunkIn = new MMCKINFO();
        static int hmmioIn = 0;
        static int dataOffset = 0;
        public static int OpenFile( string inFile)
        {
            mmckinfoParentIn = new MMCKINFO();
            mmckinfoSubchunkIn = new MMCKINFO();
            mmioinfo mmioinf = new mmioinfo();
            mmioinf.adwInfo = (new StringBuilder()).Append(' ', 4).ToString();
            hmmioIn = mmioOpenA(inFile, ref mmioinf, MMIO_READ);
            if (hmmioIn == 0) return -1;
            mmioDescend(hmmioIn, ref mmckinfoParentIn, 0,            MMIO_FINDRIFF);
            mmckinfoSubchunkIn.ckid = mmioStringToFOURCCA("fmt", 0);
            mmioDescend(hmmioIn, ref mmckinfoSubchunkIn,            ref mmckinfoParentIn, MMIO_FINDCHUNK);
            mmioRead(hmmioIn, ref format_wave,            Marshal.SizeOf(format_wave));
            mmioAscend(hmmioIn, ref mmckinfoSubchunkIn, 0);
            mmckinfoSubchunkIn.ckid = mmioStringToFOURCCA("data", 0);
            mmioDescend(hmmioIn, ref mmckinfoSubchunkIn,            ref mmckinfoParentIn,            MMIO_FINDCHUNK);
            dataOffset = mmioSeek(hmmioIn, 0, SEEK_CUR);
            return dataOffset;
        }
        public static void LoadFile( string inFile, int start, int length)
        {
            OpenFile( inFile);
            GlobalFree(hmem);
            hmem = GlobalAlloc(0x40, length);
            bufferIn = GlobalLock(hmem);
            mmioSeek(hmmioIn, start, SEEK_SET);
            mmioRead(hmmioIn, bufferIn, length);
            numSamples = length / format_wave.nBlockAlign;
            mmioClose(hmmioIn, 0);
        }
        public static void CloseFile()
        {
            GlobalFree(hmem);
            mmioClose(hmmioIn, 0);
        }

    }
}