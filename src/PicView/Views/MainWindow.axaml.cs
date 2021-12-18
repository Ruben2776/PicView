using System.Reactive.Disposables;
using Avalonia;
using Avalonia.Animation.Animators;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using Avalonia.ReactiveUI;
using Avalonia.Win32;
using PicView.ViewModels;
using ReactiveUI;

namespace PicView.Views
{
    public partial class MainWindow : ReactiveWindow<MainWindowViewModel>
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
            
            ClientSizeProperty.Changed.Subscribe(size =>
            {
                var x = (size.OldValue.Value.Width - size.NewValue.Value.Width) / 2;
                var y = (size.OldValue.Value.Height - size.NewValue.Value.Height) / 2;

                Position = new PixelPoint(Position.X + (int)x, Position.Y + (int)y);
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