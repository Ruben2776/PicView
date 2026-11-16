using PicView.Core.Comparison;
using PicView.Core.Config;
using PicView.Core.DebugTools;
using PicView.Core.ImageDecoding;
using PicView.Core.Models;
using R3;

namespace PicView.Core.ViewModels;

/// <summary>
/// A tab's comparison: which two images show as A and B, and the per-tab swap, pin, overrides and alignment.
/// A is the override for A or else the primary, B the override for B or else the secondary; a swap exchanges them.
/// The comparison preferences write through to <see cref="AppSettings.Comparison"/>, each followed by a save.
/// </summary>
public sealed class ComparisonViewModel : IDisposable
{
    private readonly Action _save;

    private ImageModel _primary;
    private ImageModel? _secondary;

    private ImageModel? _overrideA;
    private ImageModel? _overrideB;

    // Whether this owns the override in each slot: a picked override is owned, a pinned primary is not
    private bool _ownsA, _ownsB;

    private (double X, double Y) _alignA1, _alignB1, _alignA2;

    private bool _isDisposed;

    private readonly BindableReactiveProperty<bool> _isSwapped = new();
    private readonly BindableReactiveProperty<bool> _isPinned = new();
    private readonly BindableReactiveProperty<int> _alignmentStep = new();
    private readonly BindableReactiveProperty<bool> _hasAlignment = new();
    private readonly BindableReactiveProperty<SimilarityMatrix?> _alignment = new();
    private readonly IReadOnlyBindableReactiveProperty<bool> _isAligning;

    private readonly BindableReactiveProperty<object?> _imageA = new();
    private readonly BindableReactiveProperty<object?> _imageB = new();
    private readonly BindableReactiveProperty<ImageType?> _imageTypeA = new();
    private readonly BindableReactiveProperty<ImageType?> _imageTypeB = new();
    private readonly BindableReactiveProperty<FileInfo?> _fileInfoA = new();
    private readonly BindableReactiveProperty<FileInfo?> _fileInfoB = new();

    public BindableReactiveProperty<ComparisonMode> Mode { get; }
    public BindableReactiveProperty<double> OverlayOpacity { get; }
    public BindableReactiveProperty<double> SplitRatio { get; }
    public BindableReactiveProperty<double> BlinkInterval { get; }
    public BindableReactiveProperty<bool> IsSplitHorizontal { get; }
    public BindableReactiveProperty<bool> IsBlinkFast { get; }
    public BindableReactiveProperty<bool> IsStretchFill { get; }

    public IReadOnlyBindableReactiveProperty<bool> IsOverlay { get; }
    public IReadOnlyBindableReactiveProperty<bool> IsSplit { get; }
    public IReadOnlyBindableReactiveProperty<bool> IsBlink { get; }
    public IReadOnlyBindableReactiveProperty<bool> IsDifference { get; }
    public IReadOnlyBindableReactiveProperty<bool> IsSynced { get; }

    public IReadOnlyBindableReactiveProperty<bool> IsSwapped => _isSwapped;
    public IReadOnlyBindableReactiveProperty<bool> IsPinned => _isPinned;
    public IReadOnlyBindableReactiveProperty<bool> IsAligning => _isAligning;
    public IReadOnlyBindableReactiveProperty<bool> HasAlignment => _hasAlignment;

    /// <summary>0 when not aligning; 1 to 4 when the next point is A1, B1, A2 or B2.</summary>
    public IReadOnlyBindableReactiveProperty<int> AlignmentStep => _alignmentStep;

    /// <summary>The transform mapping B onto A, or null when there is no alignment.</summary>
    public IReadOnlyBindableReactiveProperty<SimilarityMatrix?> Alignment => _alignment;

    public IReadOnlyBindableReactiveProperty<object?> ImageA => _imageA;
    public IReadOnlyBindableReactiveProperty<object?> ImageB => _imageB;
    public IReadOnlyBindableReactiveProperty<ImageType?> ImageTypeA => _imageTypeA;
    public IReadOnlyBindableReactiveProperty<ImageType?> ImageTypeB => _imageTypeB;
    public IReadOnlyBindableReactiveProperty<FileInfo?> FileInfoA => _fileInfoA;
    public IReadOnlyBindableReactiveProperty<FileInfo?> FileInfoB => _fileInfoB;

    public bool IsDisposed => _isDisposed;

    public ComparisonViewModel(ImageModel primary, ImageModel? secondary, Action save)
    {
        _primary = primary;
        _secondary = secondary;
        _save = save;

        var saved = Settings.Comparison!;
        Mode = new BindableReactiveProperty<ComparisonMode>(saved.Mode);
        OverlayOpacity = new BindableReactiveProperty<double>(saved.OverlayOpacity);
        SplitRatio = new BindableReactiveProperty<double>(saved.SplitRatio);
        BlinkInterval = new BindableReactiveProperty<double>(saved.BlinkInterval);
        IsSplitHorizontal = new BindableReactiveProperty<bool>(saved.IsSplitHorizontal);
        IsBlinkFast = new BindableReactiveProperty<bool>(saved.IsBlinkFast);
        IsStretchFill = new BindableReactiveProperty<bool>(saved.IsStretchFill);

        Follow(Mode, static value => Settings.Comparison!.Mode = value);
        Follow(OverlayOpacity, static value => Settings.Comparison!.OverlayOpacity = value);
        Follow(SplitRatio, static value => Settings.Comparison!.SplitRatio = value);
        Follow(BlinkInterval, static value => Settings.Comparison!.BlinkInterval = value);
        Follow(IsSplitHorizontal, static value => Settings.Comparison!.IsSplitHorizontal = value);
        Follow(IsBlinkFast, static value => Settings.Comparison!.IsBlinkFast = value);
        Follow(IsStretchFill, static value => Settings.Comparison!.IsStretchFill = value);

        IsOverlay = Is(ComparisonMode.Overlay);
        IsSplit = Is(ComparisonMode.Split);
        IsBlink = Is(ComparisonMode.Blink);
        IsDifference = Is(ComparisonMode.Difference);
        IsSynced = Is(ComparisonMode.Synced);

        _isAligning = _alignmentStep.Select(static step => step != 0)
            .DistinctUntilChanged()
            .ToReadOnlyBindableReactiveProperty();

        UpdateShown();
    }

    /// <summary>The mode after <paramref name="mode"/>, wrapping from the last to the first.</summary>
    public static ComparisonMode Next(ComparisonMode mode) => (ComparisonMode)(((int)mode + 1) % 5);

    public void SetMode(ComparisonMode mode) => Mode.Value = mode;

    public void NextMode() => Mode.Value = Next(Mode.Value);

    /// <summary>Exchanges A and B, unless neither shows an image.</summary>
    public void Swap()
    {
        if (_imageA.Value is null && _imageB.Value is null)
        {
            return;
        }

        _isSwapped.Value = !_isSwapped.Value;
        UpdateShown();
    }

    /// <summary>
    /// Pinning keeps the overrides when the pair changes; with no override for A, the current primary becomes it.
    /// Unpinning clears both overrides.
    /// </summary>
    public void TogglePin()
    {
        var pin = !_isPinned.Value;
        _isPinned.Value = pin;
        if (pin)
        {
            if (_overrideA is null && _primary.Image is not null)
            {
                _overrideA = _primary;
                _ownsA = false;
            }
        }
        else
        {
            ReleaseOverrides();
        }
        UpdateShown();
    }

    /// <summary>Shows <paramref name="model"/> in <paramref name="slot"/>. This takes ownership of it.</summary>
    public void SetOverride(ComparisonSlot slot, ImageModel model)
    {
        if (slot is ComparisonSlot.A)
        {
            Release(ref _overrideA, ref _ownsA, model);
            _overrideA = model;
            _ownsA = true;
        }
        else
        {
            Release(ref _overrideB, ref _ownsB, model);
            _overrideB = model;
            _ownsB = true;
        }
        UpdateShown();
    }

    public void ClearOverrides()
    {
        ReleaseOverrides();
        _isSwapped.Value = false;
        _isPinned.Value = false;
        ResetAlignment();
        UpdateShown();
    }

    public void ResetAlignment()
    {
        _alignmentStep.Value = 0;
        _hasAlignment.Value = false;
        _alignment.Value = null;
    }

    /// <summary>Starts picking alignment points, dropping any alignment, or stops picking them.</summary>
    public void ToggleAligning()
    {
        if (_alignmentStep.Value != 0)
        {
            _alignmentStep.Value = 0;
            return;
        }

        _alignment.Value = null;
        _hasAlignment.Value = false;
        _alignmentStep.Value = 1;
    }

    /// <summary>
    /// Takes the next alignment point, in the order A1, B1, A2, B2. The fourth computes the alignment, when the points
    /// allow one, and stops aligning.
    /// </summary>
    public void AddAlignmentPoint(double x, double y)
    {
        switch (_alignmentStep.Value)
        {
            case 1:
                _alignA1 = (x, y);
                _alignmentStep.Value = 2;
                break;
            case 2:
                _alignB1 = (x, y);
                _alignmentStep.Value = 3;
                break;
            case 3:
                _alignA2 = (x, y);
                _alignmentStep.Value = 4;
                break;
            case 4:
                if (SimilarityTransform.TryCompute(_alignA1, _alignA2, _alignB1, (x, y), out var matrix))
                {
                    _alignment.Value = matrix;
                    _hasAlignment.Value = true;
                }
                _alignmentStep.Value = 0;
                break;
        }
    }

    /// <summary>
    /// The tab shows a new pair: the swap and the alignment reset, and unless pinned the overrides are cleared.
    /// </summary>
    public void OnPairChanged(ImageModel primary, ImageModel? secondary)
    {
        _primary = primary;
        _secondary = secondary;
        _isSwapped.Value = false;
        ResetAlignment();
        if (!_isPinned.Value)
        {
            ReleaseOverrides();
        }
        UpdateShown();
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }
        _isDisposed = true;

        ReleaseOverrides();

        Mode.Dispose();
        OverlayOpacity.Dispose();
        SplitRatio.Dispose();
        BlinkInterval.Dispose();
        IsSplitHorizontal.Dispose();
        IsBlinkFast.Dispose();
        IsStretchFill.Dispose();
        IsOverlay.Dispose();
        IsSplit.Dispose();
        IsBlink.Dispose();
        IsDifference.Dispose();
        IsSynced.Dispose();
        _isSwapped.Dispose();
        _isPinned.Dispose();
        _isAligning.Dispose();
        _alignmentStep.Dispose();
        _hasAlignment.Dispose();
        _alignment.Dispose();
        _imageA.Dispose();
        _imageB.Dispose();
        _imageTypeA.Dispose();
        _imageTypeB.Dispose();
        _fileInfoA.Dispose();
        _fileInfoB.Dispose();
    }

    private IReadOnlyBindableReactiveProperty<bool> Is(ComparisonMode mode)
        => Mode.Select(current => current == mode)
            .DistinctUntilChanged()
            .ToReadOnlyBindableReactiveProperty();

    private void Follow<T>(BindableReactiveProperty<T> preference, Action<T> write)
    {
        preference.Skip(1).Subscribe(value =>
        {
            write(value);
            _save();
        }, DebugHelper.LogError(nameof(ComparisonViewModel), nameof(Follow)));
    }

    private void ReleaseOverrides()
    {
        Release(ref _overrideA, ref _ownsA, null);
        Release(ref _overrideB, ref _ownsB, null);
    }

    /// <summary>Clears an override, disposing it when owned and it is not <paramref name="keep"/>.</summary>
    private static void Release(ref ImageModel? model, ref bool owns, ImageModel? keep)
    {
        if (owns && model is not null && !ReferenceEquals(model, keep))
        {
            model.Dispose();
        }
        model = null;
        owns = false;
    }

    private void UpdateShown()
    {
        var a = _overrideA ?? _primary;
        var b = _overrideB ?? _secondary;
        if (_isSwapped.Value)
        {
            (a, b) = (b, a);
        }

        _imageA.Value = a?.Image;
        _imageTypeA.Value = a?.ImageType;
        _fileInfoA.Value = a?.FileInfo;

        _imageB.Value = b?.Image;
        _imageTypeB.Value = b?.ImageType;
        _fileInfoB.Value = b?.FileInfo;
    }
}
