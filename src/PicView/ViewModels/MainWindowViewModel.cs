using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.Serialization;
using ReactiveUI;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
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
            LoadCommand = ReactiveCommand.Create(async () =>
            {
                var args = Environment.GetCommandLineArgs();
                if (args.Length < 1) { return; }
                
                FileInfo fileInfo = new(args[1]);
                var pic = await Data.Imaging.ImageDecoder.ReturnPicAsync(fileInfo).ConfigureAwait(false);
                if (pic is not null)
                {
                    Pic = pic;
                    var (width, height) =
                        Data.Sizing.ImageSizeHelper.GetScaledImageSize(pic.Size.Width, pic.Size.Height, desktop.MainWindow);
                    Width = width;
                    Height = height;
                    Title = $"{width} x {height}";
                }
                else
                {
                    Title = "No image loaded";
                }
            });
        }
        public ICommand? ExitCommand { get; }
        public ICommand? MinimizeCommand { get; }
        public ICommand? LoadCommand { get; }
        
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
