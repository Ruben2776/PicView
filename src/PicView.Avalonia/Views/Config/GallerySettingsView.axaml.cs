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
        FollowGallerySettings();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _disposables.Dispose();
    }

    private void FollowGallerySettings()
    {
        _disposables.Dispose();
        _disposables = default;
        // The Active Main Window's, as the page is shown; none, or a closed one, shows nothing
        if (ActiveGallerySettings() is not { } settings)
        {
            return;
        }

        try
        {
            settings.ExpandedGalleryStretchMode
                .Subscribe(mode => SelectStretchMode(FullGalleryComboBox, mode),
                    DebugHelper.LogError(nameof(GallerySettingsView), nameof(FullGalleryComboBox)))
                .AddTo(ref _disposables);

            settings.DockedGalleryStretchMode
                .Subscribe(mode => SelectStretchMode(DockedGalleryComboBox, mode),
                    DebugHelper.LogError(nameof(GallerySettingsView), nameof(DockedGalleryComboBox)))
                .AddTo(ref _disposables);

            settings.ExpandedGalleryDisplayMode
                .Subscribe(SelectDisplayMode,
                    DebugHelper.LogError(nameof(GallerySettingsView), nameof(DisplayModeComboBox)))
                .AddTo(ref _disposables);
        }
        catch (ObjectDisposedException)
        {
            // The window closed between the check and the subscribe
            _disposables.Dispose();
            _disposables = default;
        }
    }

    /// <summary>
    /// The gallery settings of the Active Main Window, which the page shows and changes; null when none is open or it
    /// has closed.
    /// </summary>
    private WindowGallerySettings? ActiveGallerySettings() =>
        DataContext is CoreViewModel { MainWindows.ActiveWindow.Value.WindowSettings.Gallery: { IsDisposed: false } settings }
            ? settings
            : null;

    // A window that closes between the check and the command takes no change
    private static void Execute<T>(ReactiveCommand<T> command, T value)
    {
        try
        {
            command.Execute(value);
        }
        catch (ObjectDisposedException)
        {
            // The window closed between the check and the command
        }
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

    private void SelectDisplayMode(GalleryDisplayMode mode)
    {
        foreach (var item in DisplayModeComboBox.Items)
        {
            if (item is ComboBoxItem { Tag: GalleryDisplayMode itemMode } && itemMode == mode)
            {
                DisplayModeComboBox.SelectedItem = item;
                return;
            }
        }
    }

    private void DisplayModeComboBox_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DisplayModeComboBox.SelectedItem is not ComboBoxItem { Tag: GalleryDisplayMode mode } ||
            ActiveGallerySettings() is not { } settings)
        {
            return;
        }

        // The same command as the gallery's own display mode menus
        Execute(settings.SetExpandedDisplayModeCommand, mode);
    }

    private void StretchComboBox_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is not ComboBox { SelectedItem: ComboBoxItem { Tag: GalleryStretchMode mode } } comboBox ||
            ActiveGallerySettings() is not { } settings)
        {
            return;
        }

        // The same commands as the gallery's own Stretch menus
        var command = comboBox == DockedGalleryComboBox
            ? settings.SetDockedStretchModeCommand
            : settings.SetExpandedStretchModeCommand;
        Execute(command, mode);
    }
}
