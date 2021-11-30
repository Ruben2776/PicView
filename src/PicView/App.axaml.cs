using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using PicView.Styles.Themes;
using PicView.Styles.Themes.Dark;
using PicView.ViewModels;
using PicView.Views;

namespace PicView
{
    public class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
            LoadTheme();
        }

        private void LoadTheme()
        {
            // TODO add more themes that can be switched
            Styles.Add(new FluentDark());
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var w = desktop.MainWindow = new MainWindow();
                w.DataContext = new MainWindowViewModel(w);
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}