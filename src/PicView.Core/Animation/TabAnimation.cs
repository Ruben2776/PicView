using PicView.Core.ImageDecoding;
using PicView.Core.Models;
using R3;

namespace PicView.Core.Animation;

/// <summary>
///     The playbacks of the animated images a tab shows: one for the primary image and one for the image beside it.
///     Each display makes new playbacks, so a file shown again starts from its first frame.
/// </summary>
public sealed class TabAnimation : IDisposable
{
    private readonly BindableReactiveProperty<AnimationPlayback?> _primary = new();
    private readonly BindableReactiveProperty<AnimationPlayback?> _secondary = new();

    public IReadOnlyBindableReactiveProperty<AnimationPlayback?> Primary => _primary;

    public IReadOnlyBindableReactiveProperty<AnimationPlayback?> Secondary => _secondary;

    public void Dispose()
    {
        _primary.Dispose();
        _secondary.Dispose();
    }

    /// <summary>Pauses both playbacks, or resumes both when the primary (else the secondary) is paused.</summary>
    public void TogglePause()
    {
        var primary = _primary.Value;
        var secondary = _secondary.Value;
        var lead = primary ?? secondary;
        if (lead is null)
        {
            return;
        }

        var paused = !lead.IsPaused;
        primary?.SetPaused(paused);
        secondary?.SetPaused(paused);
    }

    public void Step(int frames)
    {
        _primary.Value?.Step(frames);
        _secondary.Value?.Step(frames);
    }

    public void SetSpeed(double speed)
    {
        _primary.Value?.SetSpeed(speed);
        _secondary.Value?.SetSpeed(speed);
    }

    internal void Show(ImageModel primary, ImageModel? secondary)
    {
        _secondary.Value = Create(secondary);
        _primary.Value = Create(primary);
    }

    internal void ReplacePrimary(FileInfo file)
    {
        if (_primary.Value is { } current)
        {
            _primary.Value = new AnimationPlayback(file, current.ImageType);
        }
    }

    internal void ReplaceSecondary(FileInfo file)
    {
        if (_secondary.Value is { } current)
        {
            _secondary.Value = new AnimationPlayback(file, current.ImageType);
        }
    }

    internal void End()
    {
        _secondary.Value = null;
        _primary.Value = null;
    }

    private static AnimationPlayback? Create(ImageModel? model) =>
        model is { FileInfo: { } file, ImageType: ImageType.AnimatedGif or ImageType.AnimatedWebp or ImageType.AnimatedAvif }
            ? new AnimationPlayback(file, model.ImageType)
            : null;
}
