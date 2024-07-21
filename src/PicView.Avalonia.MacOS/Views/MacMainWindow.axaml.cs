using Avalonia.Controls;
using PicView.Avalonia.ViewModels;
using System;
using PicView.Avalonia.UI;

namespace PicView.Avalonia.MacOS.Views;

public partial class MacMainWindow : Window
{
    public MacMainWindow()
    {
        InitializeComponent();

        Loaded += delegate
        {
            // Keep window position when resizing
            ClientSizeProperty.Changed.Subscribe(size =>
            {
                WindowHelper.HandleWindowResize(this, size);
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
        var vm = (MainViewModel)DataContext;
        WindowHelper.SetSize(vm);
    }

    protected override async void OnClosing(WindowClosingEventArgs e)
    {
        e.Cancel = true;
        await WindowHelper.WindowClosingBehavior(this);
        base.OnClosing(e);
    }
}