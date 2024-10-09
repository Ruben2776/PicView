using System.Reactive.Linq;
using Avalonia.Input;
using Avalonia.Media;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Navigation;
using PicView.Avalonia.ViewModels;
using PicView.Core.Config;
using ReactiveUI;

namespace PicView.Avalonia.Views.UC.Menus;

public partial class ImageMenu  : AnimatedMenu
{
    public ImageMenu()
    {
        InitializeComponent();
        Loaded += delegate
        {
            if (SettingsHelper.Settings.Theme.GlassTheme)
            {
               GoToPicBox.Background = new SolidColorBrush(Color.Parse("#2CFFFFFF"));
            }
            else if (!SettingsHelper.Settings.Theme.Dark)
            {
                TopBorder.Background = Brushes.White;
            }
            GoToPicBox.KeyDown += async (_, e) => await GoToPicBox_OnKeyDown(e);
            this.WhenAnyValue(x => x.IsVisible)
                .Where(isVisible => !isVisible).Subscribe(_ => SlideShowButton.Flyout.Hide());
        };
    }

    private async Task GoToPicBox_OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            if (DataContext is not MainViewModel vm)
            {
                return;
            }

            if (!NavigationHelper.CanNavigate(vm))
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
            else if (number > vm.ImageIterator?.ImagePaths?.Count)
            {
                number = vm.ImageIterator?.ImagePaths?.Count - 1 ?? 0;
            }
            else
            {
                number--;
            }

            await vm.ImageIterator.IterateToIndex(number).ConfigureAwait(false);
        }
    }
}