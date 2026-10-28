using System.Globalization;
using Avalonia;
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
    private IDisposable? _disposable;
    /// <param name="gallery">The gallery of the tab shown in the Main Window the slider is shown in.</param>
    public GalleryItemSizeSlider(GalleryViewModel gallery)
    {
        InitializeComponent();
        if (Application.Current.DataContext is not CoreViewModel core)
        {
            return;
        }

        var gallerySettings = core.GallerySettings;
        var isExpanded = gallery.IsGalleryExpanded.CurrentValue;
        if (isExpanded)
        {
            HeaderTextBlock.Text = TranslationManager.Translation.ExpandedGalleryItemSize;
            MainSlider.Minimum = GalleryDefaults.MinExpandedGalleryItemHeight;
            MainSlider.Maximum = GalleryDefaults.MaxExpandedGalleryItemHeight;
            MainSlider.Value = Settings.Gallery.ExpandedGalleryItemSize;
            _disposable = Observable.EveryValueChanged(MainSlider, size => size.Value)
                .Subscribe(x =>
                {
                    gallerySettings.ExpandedGalleryItemSize.Value = x;
                    ValueTextBlock.Text = x.ToString(CultureInfo.InvariantCulture);
                },
                DebugHelper.LogError(nameof(GalleryItemSizeSlider), nameof(MainSlider)));
        }
        else
        {
            HeaderTextBlock.Text = TranslationManager.Translation.DockedGalleryItemSize;
            MainSlider.Minimum = GalleryDefaults.MinDockedGalleryItemHeight;
            MainSlider.Maximum = GalleryDefaults.MaxDockedGalleryItemHeight;
            MainSlider.Value = Settings.Gallery.DockedGalleryItemSize;
            _disposable = Observable.EveryValueChanged(MainSlider, size => size.Value)
            .Subscribe(x =>
                {
                    gallerySettings.DockedGalleryItemSize.Value = x;
                    ValueTextBlock.Text = x.ToString(CultureInfo.InvariantCulture);
                },
                DebugHelper.LogError(nameof(GalleryItemSizeSlider), nameof(MainSlider)));
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
