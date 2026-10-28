using Avalonia;
using Avalonia.Controls;
using PicView.Core.DebugTools;
using PicView.Core.Gallery;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Avalonia.Views.Config;

public partial class GallerySettingsView : UserControl
{
    private DisposableBag _disposables;

    public GallerySettingsView()
    {
        InitializeComponent();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        FollowStretchModes();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _disposables.Dispose();
    }

    private void FollowStretchModes()
    {
        _disposables.Dispose();
        _disposables = default;
        if (DataContext is not CoreViewModel core)
        {
            return;
        }

        core.GallerySettings.ExpandedGalleryStretchMode
            .Subscribe(mode => SelectStretchMode(FullGalleryComboBox, mode),
                DebugHelper.LogError(nameof(GallerySettingsView), nameof(FullGalleryComboBox)))
            .AddTo(ref _disposables);

        core.GallerySettings.DockedGalleryStretchMode
            .Subscribe(mode => SelectStretchMode(DockedGalleryComboBox, mode),
                DebugHelper.LogError(nameof(GallerySettingsView), nameof(DockedGalleryComboBox)))
            .AddTo(ref _disposables);
    }

    // Each item carries its stretch mode in its Tag, because the numbers of the stretch modes skip removed ones
    private static void SelectStretchMode(ComboBox comboBox, GalleryStretchMode mode)
    {
        foreach (var item in comboBox.Items)
        {
            if (item is ComboBoxItem { Tag: GalleryStretchMode itemMode } && itemMode == mode)
            {
                comboBox.SelectedItem = item;
                return;
            }
        }
    }

    private void StretchComboBox_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is not ComboBox { SelectedItem: ComboBoxItem { Tag: GalleryStretchMode mode } } comboBox ||
            DataContext is not CoreViewModel core)
        {
            return;
        }

        // The same commands as the gallery's own Stretch menus
        var command = comboBox == DockedGalleryComboBox
            ? core.GallerySettings.SetDockedStretchModeCommand
            : core.GallerySettings.SetExpandedStretchModeCommand;
        command.Execute(mode);
    }
}
