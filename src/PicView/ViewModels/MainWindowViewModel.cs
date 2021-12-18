using System.Diagnostics;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.Serialization;
using ReactiveUI;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using PicView.Data.Imaging;
using PicView.Navigation;
using PicView.Views;

namespace PicView.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        public MainWindowViewModel()
        {
            if (Application.Current.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            {
                return;
            }
            
            ExitCommand = ReactiveCommand.Create(desktop.MainWindow.Close);
            MinimizeCommand = ReactiveCommand.Create(() =>
                desktop.MainWindow.WindowState = WindowState.Minimized);

            Task NextTask(ImageIterator.Values _) => SetValues(Iterator?.GetValues(NavigateTo.Next));
            Next = ReactiveCommand.CreateFromTask((Func<ImageIterator.Values?, Task>) NextTask);
            
            Task PrevTask(ImageIterator.Values _) => SetValues(Iterator?.GetValues(NavigateTo.Prev));
            Prev = ReactiveCommand.CreateFromTask((Func<ImageIterator.Values?, Task>) PrevTask);
            
            Task LastTask(ImageIterator.Values _) => SetValues(Iterator?.GetValues(NavigateTo.Last));
            Last = ReactiveCommand.CreateFromTask((Func<ImageIterator.Values?, Task>) LastTask);
            
            Task FirstTask(ImageIterator.Values _) => SetValues(Iterator?.GetValues(NavigateTo.First));
            First = ReactiveCommand.CreateFromTask((Func<ImageIterator.Values?, Task>) FirstTask);
            
            LoadCommand = ReactiveCommand.Create(LoadTask);
        }

        private async Task LoadTask()
        {
            var args = Environment.GetCommandLineArgs();
            if (args.Length < 1) { return; }
                
            FileInfo fileInfo = new(args[1]);
            Iterator = new ImageIterator(fileInfo);
            var values = await Iterator.GetValuesFromFileAsync(fileInfo).ConfigureAwait(false);
            await SetValues(values).ConfigureAwait(false);
        }
        
        private async Task SetValues(ImageIterator.Values? values)
        {
            if (values is null) { return; }
            
            values.Image ??= await ImageDecoder.GetPicAsync(values.FileInfo).ConfigureAwait(false);
            
            Pic = values.Image;
                
            if (values.Sizes != null)
            {
                Width = values.Sizes[0];
                Height = values.Sizes[1];
                TitleWidth = values.Sizes[2];
            }

            WindowTitle = values?.Titles[0] ?? "Loading...";
            Title = values?.Titles[1] ?? "Loading...";
            Tooltip = values?.Titles[2] ?? "Loading...";
            
            if (Iterator is null) { return; }
            
            await Iterator.PreloadAsync().ConfigureAwait(false);
        }
        
        public ICommand? ExitCommand { get; }
        public ICommand? MinimizeCommand { get; }
        public ICommand? LoadCommand { get; }
        
        private ImageIterator? Iterator { get; set; }
        
        public ReactiveCommand<ImageIterator.Values?, Unit>? Next { get; }
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
