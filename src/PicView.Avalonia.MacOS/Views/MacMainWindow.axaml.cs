using Avalonia;
using Avalonia.Controls;
using PicView.Avalonia.ViewModels;
using PicView.Core.Config;
using PicView.Core.FileHandling;
using System;
using Avalonia.Input;

namespace PicView.Avalonia.MacOS.Views;

public partial class MacMainWindow : Window
{
    public MacMainWindow()
    {
        InitializeComponent();

        Loaded += delegate
        {
            ClientSizeProperty.Changed.Subscribe(size =>
            {
                if (!SettingsHelper.Settings.WindowProperties.AutoFit)
                {
                    return;
                }

                if (!size.OldValue.HasValue || !size.NewValue.HasValue)
                {
                    return;
                }

                if (size.OldValue.Value.Width == 0 || size.OldValue.Value.Height == 0 ||
                    size.NewValue.Value.Width == 0 || size.NewValue.Value.Height == 0)
                {
                    return;
                }

                var x = (size.OldValue.Value.Width - size.NewValue.Value.Width) / 2;
                var y = (size.OldValue.Value.Height - size.NewValue.Value.Height) / 2;

                Position = new PixelPoint(Position.X + (int)x, Position.Y + (int)y);
            });
        };
    }

    private void Control_OnSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        if (DataContext == null)
        {
            return;
        }

        if (e is { HeightChanged: false, WidthChanged: false })
        {
            return;
        }
        var wm = (MainViewModel)DataContext;
        wm.SetSize();
    }

    protected override async void OnClosing(WindowClosingEventArgs e)
    {
        e.Cancel = true;
        Hide();

        await SettingsHelper.SaveSettingsAsync();
        FileDeletionHelper.DeleteTempFiles();
        base.OnClosing(e);

        Environment.Exit(0);
    }
}