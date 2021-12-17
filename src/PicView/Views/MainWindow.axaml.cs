using System.Reactive.Disposables;
using Avalonia;
using Avalonia.Animation.Animators;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using Avalonia.Win32;
using PicView.ViewModels;

namespace PicView.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void TopLevel_OnOpened(object? sender, EventArgs e)
        {
            (DataContext as MainWindowViewModel)?.LoadCommand?.Execute(null);

            IWindowBaseImpl owner = new WindowImpl();
            var scaling = owner?.DesktopScaling ?? PlatformImpl?.DesktopScaling ?? 1;
            
            ClientSizeProperty.Changed.Subscribe(
            x =>
            {
                var newSize = new Size(
                    ClientSize.Width + (x.OldValue.Value.Width - x.NewValue.Value.Width) / 2,
                    ClientSize.Height + (x.OldValue.Value.Height - x.NewValue.Value.Height) / 2);
                var rect = new PixelRect(
                    PixelPoint.Origin,
                    PixelSize.FromSize(newSize, scaling));
                var screen = Screens.ScreenFromPoint(owner?.Position ?? Position);
                if (screen != null)
                {
                  Position = screen.WorkingArea.CenterRect(rect).Position;
                }
            });
        }

        private void InputElement_OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
        {
            if (e.Delta.Y > 0)
            {
                (DataContext as MainWindowViewModel)?.Prev?.Execute(null);
            }
            else
            {
                (DataContext as MainWindowViewModel)?.Next?.Execute(null);
            }
        }
    }
}