using System;
using Avalonia.Controls;
using PicView.Core.Config;

namespace PicView.Avalonia.MacOS.Views;

public partial class MacMainWindow : Window
{
    public MacMainWindow()
    {
        InitializeComponent();
    }
    
    protected override async void OnClosing(WindowClosingEventArgs e)
    {
        e.Cancel = true;
        Hide();

        await SettingsHelper.SaveSettingsAsync().ConfigureAwait(false);
        Environment.Exit(0);
    }
}