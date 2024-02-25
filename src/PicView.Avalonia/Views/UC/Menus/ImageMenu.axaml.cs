using Avalonia.Controls;
using Avalonia.Input;

namespace PicView.Avalonia.Views.UC.Menus;

public partial class ImageMenu : UserControl
{
    public ImageMenu()
    {
        InitializeComponent();
        GoToPicBox.KeyDown += async (_, e) => await GoToPicBox_OnKeyDown(e);
    }

    private async Task GoToPicBox_OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            if (DataContext is not ViewModels.MainViewModel vm)
            {
                return;
            }

            if (!int.TryParse(GoToPicBox.Text, out var number))
            {
                return;
            }

            if (number < 1)
            {
                number = 0;
            }
            else if (number > vm.ImageIterator?.Pics?.Count)
            {
                number = vm.ImageIterator?.Pics?.Count - 1 ?? 0;
            }
            else
            {
                number--;
            }

            await vm.LoadPicAtIndex(number).ConfigureAwait(false);
        }
    }
}