using System.Diagnostics;
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
            MinimizeCommand = ReactiveCommand.Create(() =>
                desktop.MainWindow.WindowState = WindowState.Minimized);

            Next = ReactiveCommand.Create(async () =>
            {
                if (Iterator is null) { return; }

                var preloadValue = LoadImage(NavigateTo.Next);
                
                SetValues(desktop, preloadValue.FileInfo ?? throw new InvalidOperationException());
                
                await Iterator.PreloadAsync().ConfigureAwait(false);
            });
            
            Prev = ReactiveCommand.Create(async () =>
            {
                if (Iterator is null) { return; }

                var preloadValue = LoadImage(NavigateTo.Prev);
                
                SetValues(desktop, preloadValue.FileInfo ?? throw new InvalidOperationException());
                
                await Iterator.PreloadAsync().ConfigureAwait(false);
            });
            
            Last = ReactiveCommand.Create(async () =>
            {
                if (Iterator is null) { return; }
                
                var preloadValue = LoadImage(NavigateTo.Last);
                
                SetValues(desktop, preloadValue.FileInfo ?? throw new InvalidOperationException());
                
                await Iterator.PreloadAsync().ConfigureAwait(false);
            });
            
            First = ReactiveCommand.Create(async () =>
            {
                if (Iterator is null) { return; }
                
                var preloadValue = LoadImage(NavigateTo.First);
                
                SetValues(desktop, preloadValue.FileInfo ?? throw new InvalidOperationException());
                
                await Iterator.PreloadAsync().ConfigureAwait(false);
            });
            
            LoadCommand = ReactiveCommand.Create(async () =>
            {
                var args = Environment.GetCommandLineArgs();
                if (args.Length < 1) { return; }
                
                FileInfo fileInfo = new(args[1]);
                Iterator = new ImageIterator(fileInfo);
                Pic = await Iterator.GetPicFromFileAsync(fileInfo).ConfigureAwait(false);
                SetValues(desktop, fileInfo);

                await Iterator.PreloadAsync().ConfigureAwait(false);
            });
        }

        private Preloader.PreloadValue LoadImage(NavigateTo navigateTo)
        {
            var preloadValue = navigateTo switch
            {
                NavigateTo.Next => Iterator?.Next() ?? throw new InvalidOperationException(),
                NavigateTo.Prev => Iterator?.Prev() ?? throw new InvalidOperationException(),
                NavigateTo.First => Iterator?.First() ?? throw new InvalidOperationException(),
                NavigateTo.Last => Iterator?.Last() ?? throw new InvalidOperationException(),
                _ => throw new ArgumentOutOfRangeException(nameof(navigateTo), navigateTo, null)
            };
            while (preloadValue.IsLoading)
            {
                WindowTitle = 
                    Title = 
                        Tooltip = "Loading...";
            }

            Pic = preloadValue.Image;
            return preloadValue;
        }

        private void SetValues(IClassicDesktopStyleApplicationLifetime desktop, FileInfo fileInfo)
        {
            if (Pic == null || Iterator?.Pics == null) { throw new Exception(); }
            
            var sizes = 
                Data.Sizing.ImageSizeHelper.GetScaledImageSize(Pic.Size.Width, Pic.Size.Height, 0, desktop.MainWindow);
            TitleWidth = sizes[2];
            Width = sizes[0];
            Height = sizes[1];
            var data = Data.TextData.TitleHelper.TitleString(
                (int)Pic.Size.Width, (int)Pic.Size.Height, Iterator.FolderIndex, fileInfo, Iterator.Pics);
            WindowTitle = data[0];
            Title = data[1];
            Tooltip = data[2];
        }
        
        public ICommand? ExitCommand { get; }
        public ICommand? MinimizeCommand { get; }
        public ICommand? LoadCommand { get; }
        
        private ImageIterator? Iterator { get; set; }
        
        public ICommand? Next { get; }
        public ICommand? Prev { get; }
        public ICommand? First { get; }
        public ICommand? Last { get; }

        private string _windowTitle = "PicView";
        public string WindowTitle
        {
            get => _windowTitle;
            set => this.RaiseAndSetIfChanged(ref _windowTitle, value);
        }
        
        private string? _title;
        public string? Title
        {
            get => _title;
            set => this.RaiseAndSetIfChanged(ref _title, value);
        }
        
        private string? _tooltip;
        public string? Tooltip
        {
            get => _tooltip;
            set => this.RaiseAndSetIfChanged(ref _tooltip, value);
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
        
        private double _titleWidth;
        public double TitleWidth
        {
            get => _titleWidth;
            set => this.RaiseAndSetIfChanged(ref _titleWidth, value);
        }
    }
}
