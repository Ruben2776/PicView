using PicView.Core.ImageDecoding;
using PicView.Core.Models;
using R3;

namespace PicView.Core.Animation;

/// <summary>
///     The playbacks of the animated images a tab shows: one for the primary image and one for the image beside it.
///     Each display makes new playbacks, so a file shown again starts from its first frame. They start at the tab's
///     Playback Speed, which carries from one shown file to the next; whether a playback is paused does not.
/// </summary>
public sealed class TabAnimation : IDisposable
{
    /// <summary>The Playback Speeds, slowest first. Speeding up and slowing down move one step.</summary>
    public static IReadOnlyList<double> SpeedSteps { get; } =
        [AnimationPlayback.MinSpeed, 0.5, AnimationPlayback.NormalSpeed, 2.0, AnimationPlayback.MaxSpeed];

    private readonly BindableReactiveProperty<AnimationPlayback?> _primary = new();
    private readonly BindableReactiveProperty<AnimationPlayback?> _secondary = new();
    private readonly BindableReactiveProperty<bool> _isActive = new();

    // Read where a load on another thread makes the next playbacks
    private double _speed = AnimationPlayback.NormalSpeed;

    public IReadOnlyBindableReactiveProperty<AnimationPlayback?> Primary => _primary;

    public IReadOnlyBindableReactiveProperty<AnimationPlayback?> Secondary => _secondary;

    /// <summary>Whether the tab shows an animated image, so there is something to pause, step or speed up.</summary>
    public IReadOnlyBindableReactiveProperty<bool> IsActive => _isActive;

    /// <summary>The tab's Playback Speed, which its playbacks play at and its next ones start at.</summary>
    public double Speed => Volatile.Read(ref _speed);

    /// <summary>The playback that speaks for both: the primary, else the secondary.</summary>
    public AnimationPlayback? Lead => _primary.Value ?? _secondary.Value;

    public void Dispose()
    {
        _primary.Dispose();
        _secondary.Dispose();
        _isActive.Dispose();
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

    /// <summary>Sets the tab's Playback Speed, clamped to the slowest and fastest step, for these playbacks and the next.</summary>
    public void SetSpeed(double speed)
    {
        if (double.IsNaN(speed))
        {
            return;
        }

        speed = Math.Clamp(speed, AnimationPlayback.MinSpeed, AnimationPlayback.MaxSpeed);
        Volatile.Write(ref _speed, speed);
        _primary.Value?.SetSpeed(speed);
        _secondary.Value?.SetSpeed(speed);
    }

    /// <summary>Moves to the next faster step, staying at the fastest.</summary>
    /// <returns>The Playback Speed now.</returns>
    public double SpeedUp()
    {
        var speed = Speed;
        foreach (var step in SpeedSteps)
        {
            if (step > speed)
            {
                SetSpeed(step);
                break;
            }
        }

        return Speed;
    }

    /// <summary>Moves to the next slower step, staying at the slowest.</summary>
    /// <returns>The Playback Speed now.</returns>
    public double SlowDown()
    {
        var speed = Speed;
        for (var i = SpeedSteps.Count - 1; i >= 0; i--)
        {
            if (SpeedSteps[i] < speed)
            {
                SetSpeed(SpeedSteps[i]);
                break;
            }
        }

        return Speed;
    }

    public void ResetSpeed() => SetSpeed(AnimationPlayback.NormalSpeed);

    internal void Show(ImageModel primary, ImageModel? secondary)
    {
        _secondary.Value = Create(secondary);
        _primary.Value = Create(primary);
        UpdateIsActive();
    }

    internal void ReplacePrimary(FileInfo file)
    {
        if (_primary.Value is { } current)
        {
            _primary.Value = new AnimationPlayback(file, current.ImageType, Speed);
        }
    }

    internal void ReplaceSecondary(FileInfo file)
    {
        if (_secondary.Value is { } current)
        {
            _secondary.Value = new AnimationPlayback(file, current.ImageType, Speed);
        }
    }

    internal void End()
    {
        _secondary.Value = null;
        _primary.Value = null;
        UpdateIsActive();
    }

    private void UpdateIsActive() => _isActive.Value = _primary.Value is not null || _secondary.Value is not null;

    private AnimationPlayback? Create(ImageModel? model) =>
        model is { FileInfo: { } file, ImageType: ImageType.AnimatedGif or ImageType.AnimatedWebp or ImageType.AnimatedAvif }
            ? new AnimationPlayback(file, model.ImageType, Speed)
            : null;
}
