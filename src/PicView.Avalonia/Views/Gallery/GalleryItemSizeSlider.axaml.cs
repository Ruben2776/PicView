using System.Globalization;
using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using PicView.Avalonia.CustomControls;
using PicView.Core.DebugTools;
using PicView.Core.Gallery;
using PicView.Core.Localization;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Avalonia.Views.Gallery;

public partial class GalleryItemSizeSlider : AnimatedPopUp
{
    private readonly IDisposable? _disposable;

    /// <param name="gallery">The gallery of the tab shown in the Main Window the slider is shown in.</param>
    /// <param name="settings">That window's gallery settings, which the slider shows and changes. It has no Action,
    /// so it is the one writer of the size while it is dragged; nothing is saved, as the window stores its settings
    /// when it closes.</param>
    public GalleryItemSizeSlider(GalleryViewModel gallery, WindowGallerySettings settings)
    {
        InitializeComponent();
        var isExpanded = gallery.IsGalleryExpanded.CurrentValue;
        var size = isExpanded ? settings.ExpandedGalleryItemSize : settings.DockedGalleryItemSize;
        HeaderTextBlock.Text = isExpanded
            ? TranslationManager.Translation.ExpandedGalleryItemSize
            : TranslationManager.Translation.DockedGalleryItemSize;
        MainSlider.Minimum = isExpanded
            ? GalleryDefaults.MinExpandedGalleryItemHeight
            : GalleryDefaults.MinDockedGalleryItemHeight;
        MainSlider.Maximum = isExpanded
            ? GalleryDefaults.MaxExpandedGalleryItemHeight
            : GalleryDefaults.MaxDockedGalleryItemHeight;
        MainSlider.Value = size.Value;
        // Each step of a drag, as it happens; the first is the seed, which sets nothing new
        _disposable = MainSlider.GetObservable(RangeBase.ValueProperty).ToObservable()
            .Subscribe(x =>
                {
                    SetSize(settings, size, x);
                    ValueTextBlock.Text = x.ToString(CultureInfo.InvariantCulture);
                },
                DebugHelper.LogError(nameof(GalleryItemSizeSlider), nameof(MainSlider)));
    }

    // A window that closed while its slider was open takes no change (ADR-0038)
    private static void SetSize(WindowGallerySettings settings, BindableReactiveProperty<double> size, double value)
    {
        if (settings.IsDisposed)
        {
            return;
        }

        try
        {
            size.Value = value;
        }
        catch (ObjectDisposedException)
        {
            // The window closed between the check and the set
        }
    }

    private void CloseMenu(object? sender, RoutedEventArgs e)
    {
        _ = AnimatedClosing();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _disposable?.Dispose();
    }
}
