using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.UI;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.Views.UC.PopUps;

public partial class RenameDialog : AnimatedPopUp
{
    public RenameDialog()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        RenameBox.Focus();
        if (RenameBox.Text is not null)
        {
            RenameBox.CaretIndex = RenameBox.Text.Length;
        }
        
        CancelButton.Click += CloseMenu;
        CloseButton.Click += CloseMenu;
        ApplyButton.Click += ApplyButtonOnClick;
    }

    private void ApplyButtonOnClick(object? sender, RoutedEventArgs e)
    {
        _ = ConfirmAsync();
    }

    /// <summary>
    /// Renames the file and closes, as the Apply button does.
    /// </summary>
    public override async ValueTask<bool> ConfirmAsync()
    {
        _ = RenameHelper.RenameAction(DataContext as MainWindowViewModel, RenameBox.Text, TopLevel.GetTopLevel(this) as MainWindow);
        await AnimatedClosing().ConfigureAwait(true);
        return true;
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        Loaded -= OnLoaded;
        ApplyButton.Click -= ApplyButtonOnClick;
        CloseButton.Click -= CloseMenu;
        CancelButton.Click -= CloseMenu;
    }

    private void CloseMenu(object? sender, RoutedEventArgs e)
    {
        _ = AnimatedClosing();
    }
}
