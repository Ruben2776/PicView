using PicView.Core.Config;
using PicView.Core.DebugTools;
using PicView.Core.Gallery;
using R3;

namespace PicView.Core.ViewModels;

/// <summary>
/// The gallery settings every tab shares publish their changes (ADR-0020): each is a bindable over its stored value,
/// and its write-through is the one writer of that stored value, followed by one coalesced save. The write-throughs
/// start in <see cref="Start"/>, so a change before it writes and saves nothing (ADR-0034). The gallery's look (its
/// Thumbnail Stretch, Gallery Display Mode and item sizes) is not here: it belongs to each Main Window, in
/// <see cref="WindowGallerySettings"/>.
/// </summary>
public class GallerySharedSettingsViewModel
{
    private readonly Lock _startLock = new();
    private bool _isStarted;
    private readonly CoalescedSave _coalescedSave;

    /// <param name="save">Saves the settings; null means <see cref="SettingsManager.SaveSettingsAsync"/>.</param>
    public GallerySharedSettingsViewModel(Func<ValueTask<bool>>? save = null)
    {
        _coalescedSave = new CoalescedSave(save ?? SaveSettingsAsync, nameof(GallerySharedSettingsViewModel));

        GalleryItemSpacing = new BindableReactiveProperty<double>(Settings.Gallery.ItemSpacing);
        GalleryLineSpacing = new BindableReactiveProperty<double>(Settings.Gallery.LineSpacing);
        ShowMotionPhotoBadges = new BindableReactiveProperty<bool>(Settings.Gallery.ShowMotionPhotoBadges);
    }

    /// <summary>The save in flight, completed when idle. For tests.</summary>
    internal Task Saving => _coalescedSave.Saving;

    public BindableReactiveProperty<bool> ShowMotionPhotoBadges { get; }

    /// <summary>The largest Docked Gallery item size, the limit of a window's docked item size slider.</summary>
    public BindableReactiveProperty<double> DockedGalleryMaxItemSize { get; } =
        new(GalleryDefaults.MaxDockedGalleryItemHeight);

    /// <summary>The smallest Docked Gallery item size, the limit of a window's docked item size slider.</summary>
    public BindableReactiveProperty<double> DockedGalleryMinItemSize { get; } =
        new(GalleryDefaults.MinDockedGalleryItemHeight);

    /// <summary>The largest Expanded Gallery item size, the limit of a window's expanded item size slider.</summary>
    public BindableReactiveProperty<double> ExpandedGalleryMaxItemSize { get; } =
        new(GalleryDefaults.MaxExpandedGalleryItemHeight);

    /// <summary>The smallest Expanded Gallery item size, the limit of a window's expanded item size slider.</summary>
    public BindableReactiveProperty<double> ExpandedGalleryMinItemSize { get; } =
        new(GalleryDefaults.MinExpandedGalleryItemHeight);

    public BindableReactiveProperty<double> GalleryItemSpacing { get; }
    public BindableReactiveProperty<double> GalleryLineSpacing { get; }

    /// <summary>
    /// Starts the write-throughs of the gallery settings. Idempotent and safe from any thread; a concurrent caller
    /// returns only after the subscriptions exist.
    /// </summary>
    public void Start()
    {
        lock (_startLock)
        {
            if (_isStarted)
            {
                return;
            }

            // The write-throughs subscribe first, so a later subscriber that reads the record sees the new value (ADR-0020)
            GalleryItemSpacing.Skip(1).Subscribe(spacing =>
            {
                // A spacing below the minimum is not accepted: the slider is put back on the accepted one
                var accepted = GalleryDefaults.AcceptedItemSpacing(spacing);
                if (Math.Abs(accepted - spacing) > 0.001)
                {
                    GalleryItemSpacing.Value = accepted;
                }

                if (Math.Abs(Settings.Gallery.ItemSpacing - accepted) > 0.001)
                {
                    Settings.Gallery.ItemSpacing = accepted;
                    _coalescedSave.Soon();
                }
            }, DebugHelper.LogError(nameof(GallerySharedSettingsViewModel), nameof(GalleryItemSpacing)));

            GalleryLineSpacing.Skip(1).Subscribe(spacing =>
            {
                // A spacing below the minimum is not accepted: the slider is put back on the accepted one
                var accepted = GalleryDefaults.AcceptedLineSpacing(spacing);
                if (Math.Abs(accepted - spacing) > 0.001)
                {
                    GalleryLineSpacing.Value = accepted;
                }

                if (Math.Abs(Settings.Gallery.LineSpacing - accepted) > 0.001)
                {
                    Settings.Gallery.LineSpacing = accepted;
                    _coalescedSave.Soon();
                }
            }, DebugHelper.LogError(nameof(GallerySharedSettingsViewModel), nameof(GalleryLineSpacing)));

            ShowMotionPhotoBadges.Skip(1).Subscribe(show =>
            {
                if (Settings.Gallery.ShowMotionPhotoBadges != show)
                {
                    Settings.Gallery.ShowMotionPhotoBadges = show;
                    _coalescedSave.Soon();
                }
            }, DebugHelper.LogError(nameof(GallerySharedSettingsViewModel), nameof(ShowMotionPhotoBadges)));

            _isStarted = true;
        }
    }
}
