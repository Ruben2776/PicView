using Avalonia;
using Avalonia.Controls;
using PicView.Core.DebugTools;
using PicView.Core.Gallery;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Avalonia.Views.Config;

/// <summary>
/// The Settings window's gallery page. Its Thumbnail Stretch, Gallery Display Mode and item sizes show and change the
/// Active Main Window's, and follow when another Main Window becomes active; its spacing, line spacing and motion photo
/// badges are shared by every window.
/// </summary>
public partial class GallerySettingsView : UserControl
{
    private DisposableBag _disposables;
    private bool _isAttached;

    // Set while the page shows a window's values in its combo boxes, so that selection issues no command
    private bool _isShowingLook;

    public GallerySettingsView()
    {
        InitializeComponent();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _isAttached = true;
        FollowActiveWindow();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _isAttached = false;
        _disposables.Dispose();
        _disposables = default;
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (_isAttached)
        {
            FollowActiveWindow();
        }
    }

    private void FollowActiveWindow()
    {
        _disposables.Dispose();
        _disposables = default;
        if (DataContext is not CoreViewModel core)
        {
            return;
        }

        // The look of whichever Main Window is active: none, or a closed one, gives nothing
        core.MainWindows.ActiveWindow
            .Select(static window => window is null
                ? Observable.Empty<GalleryLook>()
                : window.WindowSettings.Follow(static settings => settings.Gallery.Look))
            .Switch()
            .Subscribe(ShowLook, DebugHelper.LogError(nameof(GallerySettingsView), nameof(FollowActiveWindow)))
            .AddTo(ref _disposables);
    }

    private void ShowLook(GalleryLook look)
    {
        _isShowingLook = true;
        try
        {
            Select(DockedGalleryComboBox, look.DockedStretch);
            Select(FullGalleryComboBox, look.ExpandedStretch);
            Select(DisplayModeComboBox, look.ExpandedDisplayMode);
        }
        finally
        {
            _isShowingLook = false;
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

    // Each item carries its value in its Tag, because the numbers of the stretch modes skip removed ones. A value
    // without an item selects nothing, so the previous window's choice does not stay on screen
    private static void Select<T>(ComboBox comboBox, T value) where T : struct, Enum
    {
        foreach (var item in comboBox.Items)
        {
            if (item is ComboBoxItem { Tag: T itemValue } && EqualityComparer<T>.Default.Equals(itemValue, value))
            {
                comboBox.SelectedItem = item;
                return;
            }
        }

        comboBox.SelectedItem = null;
    }

    private void DisplayModeComboBox_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_isShowingLook ||
            DisplayModeComboBox.SelectedItem is not ComboBoxItem { Tag: GalleryDisplayMode mode } ||
            ActiveGallerySettings() is not { } settings)
        {
            return;
        }

        // The same command as the gallery's own display mode menus
        Execute(settings.SetExpandedDisplayModeCommand, mode);
    }

    private void StretchComboBox_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_isShowingLook ||
            sender is not ComboBox { SelectedItem: ComboBoxItem { Tag: GalleryStretchMode mode } } comboBox ||
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
