using System.Runtime.InteropServices;
using PicView.Core.DebugTools;

namespace PicView.Avalonia.MotionPhoto;

/// <summary>
/// Loads the statically-linked picview-ffmpeg native library bundled next to the
/// application and exposes its exports. Initialization is lazy and failure is
/// remembered, so a missing native library simply degrades motion photos to regular
/// still images instead of breaking the viewer. This type never throws.
/// <para>
/// The native library is a purpose-built FFmpeg build (mov/mp4 demuxer + h264/hevc
/// decoders + libswscale, no audio) wrapped behind a tiny C ABI, so no FFmpeg types
/// or struct layouts leak into managed code.
/// </para>
/// </summary>
public static class FFmpegService
{
    /// <summary>whence value with which the native side queries the stream length.</summary>
    public const int AvSeekSize = 0x10000;

    /// <summary>Reads up to <paramref name="size"/> bytes; returns bytes read, 0 on EOF, negative on error.</summary>
    public delegate int PvReadCallback(IntPtr opaque, IntPtr buffer, int size);

    /// <summary>Seeks the stream (whence 0/1/2 = set/cur/end) or returns its length for <see cref="AvSeekSize"/>.</summary>
    public delegate long PvSeekCallback(IntPtr opaque, long offset, int whence);

    [StructLayout(LayoutKind.Sequential)]
    public struct PvVideoInfo
    {
        public int Width;
        public int Height;
        public double Fps;
        public double DurationSec;
    }

    public delegate IntPtr PvOpenCallback(IntPtr opaque, PvReadCallback read, PvSeekCallback seek, out PvVideoInfo info);
    public delegate int PvDecodeNextCallback(IntPtr session, IntPtr dst, int dstCapacity, out double pts, out int width, out int height);
    public delegate void PvCloseCallback(IntPtr session);

    private static readonly object InitLock = new();
    private static IntPtr _library;
    private static bool _initialized;
    private static bool _initFailed;

    internal static PvOpenCallback PvOpen { get; private set; } = null!;
    internal static PvDecodeNextCallback PvDecodeNext { get; private set; } = null!;
    internal static PvCloseCallback PvClose { get; private set; } = null!;

    /// <summary>
    /// Video playback is supported on all desktop platforms: the native library is
    /// bundled per runtime identifier and frames are decoded in software into BGRA32
    /// buffers rendered by the Avalonia compositor.
    /// </summary>
    public static bool IsPlaybackSupported =>
        OperatingSystem.IsWindows() || OperatingSystem.IsLinux() || OperatingSystem.IsMacOS();

    /// <summary>
    /// Attempts to load the native library. Returns false when playback is
    /// unavailable; callers should fall back to the still image.
    /// </summary>
    public static bool TryInitialize()
    {
        if (_initialized)
        {
            return true;
        }

        if (!IsPlaybackSupported || _initFailed)
        {
            return false;
        }

        lock (InitLock)
        {
            if (_initialized)
            {
                return true;
            }

            if (_initFailed)
            {
                return false;
            }

            try
            {
                var libraryPath = GetNativeLibraryPath();
                if (libraryPath is null || !File.Exists(libraryPath))
                {
                    _initFailed = true;
                    return false;
                }

                _library = NativeLibrary.Load(libraryPath);
                PvOpen = Marshal.GetDelegateForFunctionPointer<PvOpenCallback>(
                    NativeLibrary.GetExport(_library, "pv_open"));
                PvDecodeNext = Marshal.GetDelegateForFunctionPointer<PvDecodeNextCallback>(
                    NativeLibrary.GetExport(_library, "pv_decode_next"));
                PvClose = Marshal.GetDelegateForFunctionPointer<PvCloseCallback>(
                    NativeLibrary.GetExport(_library, "pv_close"));
                _initialized = true;
                return true;
            }
            catch (Exception e)
            {
                DebugHelper.LogDebug(nameof(FFmpegService), nameof(TryInitialize), e);
                _initFailed = true;
                return false;
            }
        }
    }

    /// <summary>
    /// The native library is deployed to "ffmpeg/&lt;rid&gt;" next to the application by
    /// the platform packaging (one self-contained binary per runtime identifier).
    /// </summary>
    private static string? GetNativeLibraryPath()
    {
        string rid;
        string libraryName;
        if (OperatingSystem.IsWindows())
        {
            rid = RuntimeInformation.ProcessArchitecture is Architecture.Arm64 ? "win-arm64" : "win-x64";
            libraryName = "picview-ffmpeg.dll";
        }
        else if (OperatingSystem.IsLinux())
        {
            rid = RuntimeInformation.ProcessArchitecture is Architecture.Arm64 ? "linux-arm64" : "linux-x64";
            libraryName = "libpicviewffmpeg.so";
        }
        else if (OperatingSystem.IsMacOS())
        {
            rid = RuntimeInformation.ProcessArchitecture is Architecture.Arm64 ? "osx-arm64" : "osx-x64";
            libraryName = "libpicviewffmpeg.dylib";
        }
        else
        {
            return null;
        }

        return Path.Combine(AppContext.BaseDirectory, "ffmpeg", rid, libraryName);
    }
}
