using Avalonia.Controls.ApplicationLifetimes;
using ReactiveUI;
using System.Windows.Input;
using Avalonia.Controls;

namespace PicView.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private string _title = "Loading...";
        public string Title
        {
            get => _title;
            set => this.RaiseAndSetIfChanged(ref _title, value);
        }

        public ICommand ExitCommand { get; }
        public ICommand MinimizeCommand { get; }
        public MainWindowViewModel(Window window)
        {
            ExitCommand = ReactiveCommand.Create(window.Close);

            MinimizeCommand = ReactiveCommand.Create(() => window.WindowState = WindowState.Minimized);
        }
    }
}
