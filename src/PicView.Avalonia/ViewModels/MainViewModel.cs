using Avalonia.Controls.ApplicationLifetimes;
using ReactiveUI;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;

namespace PicView.Avalonia.ViewModels;

public class MainViewModel : ViewModelBase
{
    public string Greeting => "Welcome to Avalonia!";
    public ICommand? ExitCommand { get; }
    public ICommand? MinimizeCommand { get; }
    public ICommand? MaximizeCommand { get; }

    public MainViewModel()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return;
        }

        ExitCommand = ReactiveCommand.Create(desktop.MainWindow.Close);
        MinimizeCommand = ReactiveCommand.Create(() =>
            desktop.MainWindow.WindowState = WindowState.Minimized);
        MaximizeCommand = ReactiveCommand.Create(() =>
        {
            desktop.MainWindow.WindowState = desktop.MainWindow.WindowState == WindowState.Normal ? WindowState.Maximized : WindowState.Normal;
        });
    }
}