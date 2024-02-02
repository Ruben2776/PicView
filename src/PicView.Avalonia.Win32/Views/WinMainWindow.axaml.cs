using Avalonia.Controls;
using PicView.Core.Config;
using System;
using System.Windows.Input;
using PicView.Avalonia.ViewModels;
using ReactiveUI;

namespace PicView.Avalonia.Win32.Views;

public partial class WinMainWindow : Window
{
    public WinMainWindow()
    {
        InitializeComponent();
        Loaded += delegate
        {
            if (DataContext == null)
            {
                return;
            }
            var wm = (MainViewModel)DataContext;
            wm.ShowInFolderCommand = ReactiveCommand.Create(() =>
            {
                Windows.FileHandling.FileExplorer.OpenFolderAndSelectFile(wm.FileInfo?.DirectoryName, wm.FileInfo?.Name);
            });
        };
    }

    protected override async void OnClosing(WindowClosingEventArgs e)
    {
        e.Cancel = true;
        Hide();

        await SettingsHelper.SaveSettingsAsync().ConfigureAwait(false);
        Environment.Exit(0);
    }
}