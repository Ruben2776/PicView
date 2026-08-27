using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using PicView.Core.DebugTools;
using PicView.Core.ImageDecoding;
using PicView.Core.Models;
using PicView.Core.MotionPhoto;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.MotionPhoto;

/// <summary>
/// Overlay that plays the embedded video of a motion photo on top of the still cover image.
/// Behavior: show the cover with a badge, play the video once when triggered, then freeze
/// back onto the cover (the badge remains so it can be replayed). Any failure degrades to
/// showing only the still image.
/// <para>
/// Video frames are produced as BGRA32 buffers by <see cref="MotionPhotoDecoder"/>
/// (a statically-linked, purpose-built FFmpeg) and rendered by
/// <see cref="MotionPhotoVideoSurface"/>, which works on every display stack including
/// Wayland and lets the video follow the normal Avalonia compositor. Playback is
/// video-only by design; motion photos never produce sound.
/// </para>
/// </summary>
public partial class MotionPhotoView : UserControl, IDisposable
{
    private Stream? _videoStream;
    private MotionPhotoDecoder? _decoder;
    private ImageModel? _model;
    private bool _isSessionBusy;
    private bool _firstFrameShownInSession;
    private bool _isDisposed;

    /// <summary>Raised on the UI thread when video playback starts (zoom/pan should be locked).</summary>
    public event EventHandler? PlaybackStarted;

    /// <summary>Raised on the UI thread when video playback stops (zoom/pan can be unlocked).</summary>
    public event EventHandler? PlaybackStopped;

    /// <summary>
    /// Raised on the UI thread when the first video frame is shown for a playback
    /// session, so the underlying still image can be hidden while the video covers it.
    /// </summary>
    public event EventHandler? FirstFrameShown;

    /// <summary>Whether video is currently playing or paused.</summary>
    public bool IsPlaying { get; private set; }

    /// <summary>Whether the current image is a playable motion photo (badge or video is shown).</summary>
    public bool IsMotionPhotoActive => IsVisible;

    /// <summary>
    /// Whether the auto-play setting may trigger playback for this view. Disabled for the
    /// secondary (side-by-side) view so two clips never start on their own at once.
    /// </summary>
    public bool AllowAutoPlay { get; set; } = true;

    public MotionPhotoView()
    {
        InitializeComponent();
        PlayBadge.Click += OnPlayBadgeClicked;
    }

    /// <summary>
    /// Called whenever a new image is displayed. Stops any running playback and prepares
    /// (or hides) the motion photo overlay for the new model. Null hides the overlay.
    /// </summary>
    public void OnImageChanged(ImageModel? model)
    {
        Stop();
        _model = model;

        if (model?.ImageType is ImageType.MotionPhoto &&
            model.MotionPhoto is not null &&
            FFmpegService.IsPlaybackSupported &&
            FFmpegService.TryInitialize())
        {
            IsVisible = true;
            PlayBadge.IsVisible = true;
            if (AllowAutoPlay && Settings.UIProperties.AutoPlayMotionPhotos)
            {
                _ = PlayAsync();
            }
        }
        else
        {
            IsVisible = false;
        }
    }

    /// <summary>
    /// Toggles between play and pause when playback is running, otherwise starts playback.
    /// Used by the Space keyboard shortcut.
    /// </summary>
    public void TogglePlayPause()
    {
        if (_decoder is not null && IsPlaying)
        {
            if (_decoder.IsPaused)
            {
                _decoder.Resume();
            }
            else
            {
                _decoder.Pause();
            }

            return;
        }

        if (IsVisible && !IsPlaying)
        {
            _ = PlayAsync();
        }
    }

    /// <summary>
    /// Stops playback and returns to the cover image. Returns true when playback was active.
    /// Used by the Escape keyboard shortcut.
    /// </summary>
    public bool StopIfPlaying()
    {
        if (!IsPlaying)
        {
            return false;
        }

        Stop();
        PlayBadge.IsVisible = true;
        return true;
    }

    /// <summary>
    /// Starts motion photo playback: extracts the video on demand, decodes it with the
    /// bundled FFmpeg and presents the frames once.
    /// </summary>
    public async Task PlayAsync()
    {
        if (_isSessionBusy || IsPlaying || _isDisposed)
        {
            return;
        }

        var model = _model;
        if (model?.ImageType is not ImageType.MotionPhoto || model.MotionPhoto is null || model.FileInfo is null)
        {
            return;
        }

        if (!FFmpegService.TryInitialize())
        {
            IsVisible = false;
            return;
        }

        _isSessionBusy = true;
        var cancellationToken = (DataContext as TabViewModel)?.GetTabCancellation().Token ?? default;
        Stream? stream = null;
        try
        {
            stream = await MotionPhotoExtractor.ExtractAsync(
                model.FileInfo, model.MotionPhoto, cancellationToken).ConfigureAwait(true);
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(MotionPhotoView), nameof(PlayAsync), e);
        }

        if (stream is null)
        {
            _isSessionBusy = false;
            // Extraction failed: degrade to the still image
            IsVisible = false;
            return;
        }

        try
        {
            _videoStream = stream;
            _decoder = MotionPhotoDecoder.Create(stream);
            if (_decoder is null)
            {
                CleanupSession();
                _isSessionBusy = false;
                return;
            }

            _firstFrameShownInSession = false;
            _decoder.FrameReady += OnFrameReady;
            _decoder.Ended += OnPlaybackEnded;
            _decoder.Failed += OnPlaybackEnded;
            _decoder.Play();

            // The surface stays hidden until the first decoded frame arrives, so the
            // still image remains untouched while decoding starts up (no visual pop).
            PlayBadge.IsVisible = false;
            IsPlaying = true;
            PlaybackStarted?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(MotionPhotoView), nameof(PlayAsync), e);
            CleanupSession();
            IsVisible = false;
        }
        finally
        {
            _isSessionBusy = false;
        }
    }

    /// <summary>
    /// Stops playback and releases all playback resources, returning to the cover image.
    /// The last decoded frame is cleared so it can never flash up when the surface is
    /// shown again (e.g. when replaying or when the next motion photo has a different
    /// video resolution).
    /// </summary>
    public void Stop()
    {
        var wasPlaying = IsPlaying;
        CleanupSession();
        VideoSurface.IsVisible = false;
        VideoSurface.Clear();
        IsPlaying = false;
        if (wasPlaying)
        {
            PlaybackStopped?.Invoke(this, EventArgs.Empty);
        }
    }

    private async void OnPlayBadgeClicked(object? sender, RoutedEventArgs e) => await PlayAsync();

    private void OnPlaybackEnded(object? sender, EventArgs e) =>
        Dispatcher.UIThread.Post(FreezeBackToCover);

    private void OnFrameReady(int index, IntPtr bgra, int byteCount) =>
        Dispatcher.UIThread.Post(() =>
        {
            var decoder = _decoder;
            if (decoder is null || _isDisposed)
            {
                decoder?.ReleaseBuffer(index);
                return;
            }

            try
            {
                VideoSurface.UpdateFrame(bgra, byteCount, decoder.Width, decoder.Height);
                if (!VideoSurface.IsVisible)
                {
                    VideoSurface.IsVisible = true;
                }

                if (!_firstFrameShownInSession)
                {
                    _firstFrameShownInSession = true;
                    FirstFrameShown?.Invoke(this, EventArgs.Empty);
                }
            }
            finally
            {
                decoder.ReleaseBuffer(index);
            }
        });

    private void FreezeBackToCover()
    {
        if (!IsPlaying)
        {
            return;
        }

        Stop();
        // Keep the badge visible so the clip can be replayed
        PlayBadge.IsVisible = true;
    }

    private void CleanupSession()
    {
        var decoder = _decoder;
        _decoder = null;
        if (decoder is not null)
        {
            decoder.FrameReady -= OnFrameReady;
            decoder.Ended -= OnPlaybackEnded;
            decoder.Failed -= OnPlaybackEnded;
            decoder.Dispose();
        }

        _videoStream?.Dispose();
        _videoStream = null;
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        PlayBadge.Click -= OnPlayBadgeClicked;
        Stop();
        VideoSurface.Clear();
        GC.SuppressFinalize(this);
    }
}
