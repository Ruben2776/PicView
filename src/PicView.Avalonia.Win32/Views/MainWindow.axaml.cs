using Avalonia.Controls;
using PicView.Core.Config;
using System;

namespace PicView.Avalonia.Win32.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        e.Cancel = true;
        Hide();

        SettingsHelper.SaveSettingsAsync().ConfigureAwait(false);
        Environment.Exit(0);
    }
}