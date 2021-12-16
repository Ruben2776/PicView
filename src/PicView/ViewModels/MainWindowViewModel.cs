using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.Serialization;
using ReactiveUI;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using PicView.Navigation;
using PicView.Views;
using Size = Avalonia.Size;

namespace PicView.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        public MainWindowViewModel()
        {
            if (Avalonia.Application.Current.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            {
                return;
            }
            
            ExitCommand = ReactiveCommand.Create(desktop.MainWindow.Close);
            MinimizeCommand = ReactiveCommand.Create(() => desktop.MainWindow.WindowState = WindowState.Minimized);
            
            Next = ReactiveCommand.Create(async () =>
            {
                Iterator ??= new ImageIterator();
                
                Pic = await Iterator.Next().ConfigureAwait(false);
                SetValues(desktop);
            });
            
            Prev = ReactiveCommand.Create(async () =>
            {
                Iterator ??= new ImageIterator();
                
                Pic = await Iterator.Prev().ConfigureAwait(false);
                SetValues(desktop);
            });
            
            LoadCommand = ReactiveCommand.Create(async () =>
            {
                var args = Environment.GetCommandLineArgs();
                if (args.Length < 1)
                {
                    Iterator = new ImageIterator();
                    return;
                }
                
                FileInfo fileInfo = new(args[1]);
                Iterator = new ImageIterator(fileInfo);
                Pic = await Iterator.GetPicFromFileAsync(fileInfo).ConfigureAwait(false);
                SetValues(desktop);
            });
        }

        private void SetValues(IClassicDesktopStyleApplicationLifetime desktop)
        {
            var (width, height) =
                Data.Sizing.ImageSizeHelper.GetScaledImageSize(Pic.Size.Width, Pic.Size.Height, desktop.MainWindow);
            Width = width;
            Height = height;
            Title = $"{Pic.Size.Width} x {Pic.Size.Height}";
        }
        public ICommand? ExitCommand { get; }
        public ICommand? MinimizeCommand { get; }
        public ICommand? LoadCommand { get; }
        
        private ImageIterator? Iterator { get; set; }
        
        public ICommand? Next { get; }
        public ICommand? Prev { get; }
        public ICommand? First { get; }
        public ICommand? Last { get; }
        
        private string _title = "Loading...";
        public string Title
        {
            get => _title;
            set => this.RaiseAndSetIfChanged(ref _title, value);
        }
        
        private IImage? _pic;
        public IImage? Pic
        {
            get => _pic;
            set => this.RaiseAndSetIfChanged(ref _pic, value);
        }

        private double _width;
        public double Width
        {
            get => _width;
            set => this.RaiseAndSetIfChanged(ref _width, value);
        }
        
        private double _height;
        public double Height
        {
            get => _height;
            set => this.RaiseAndSetIfChanged(ref _height, value);
        }
    }
}
