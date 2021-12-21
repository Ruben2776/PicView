using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using PicView.Shortcuts;
using PicView.ViewModels;

namespace PicView.Views
{
    public class MainWindow : ReactiveWindow<MainWindowViewModel>
    {
        public MainWindow()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
            Opened += (_,_) => TopLevel_OnOpened();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void TopLevel_OnOpened()
        {
            (DataContext as MainWindowViewModel)?.LoadCommand?.Execute(null);
            
            // Keep window position when resizing
            ClientSizeProperty.Changed.Subscribe(size =>
            {
                var x = (size.OldValue.Value.Width - size.NewValue.Value.Width) / 2;
                var y = (size.OldValue.Value.Height - size.NewValue.Value.Height) / 2;

                Position = new PixelPoint(Position.X + (int)x, Position.Y + (int)y);
            });
            
            var vm = (DataContext as MainWindowViewModel);
            KeyDown += (_, e) => MainShortcuts.HandleKeyDown(vm, e);
            KeyUp += (_, e) => MainShortcuts.HandleKeyUp(vm, e);
            PointerWheelChanged += (_, e) => MainShortcuts.HandlePointerWheel(vm, e);
        }
        
        private void MoveWindow(object? sender, PointerPressedEventArgs e)
        {
            if (VisualRoot is null) { return; }
            
            var hostWindow = (Window)VisualRoot;
            hostWindow.BeginMoveDrag(e);
        }
    }
}