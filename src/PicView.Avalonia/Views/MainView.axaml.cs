﻿using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using PicView.Avalonia.Navigation;
using PicView.Avalonia.UI;
using PicView.Avalonia.ViewModels;
using ReactiveUI;

namespace PicView.Avalonia.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
        // TODO add visual feedback for drag and drop
        //AddHandler(DragDrop.DragOverEvent, DragOver);
        AddHandler(DragDrop.DropEvent, Drop);
        GotFocus += delegate
        {
            if (UIHelper.GetEditableTitlebar.IsOpen)
            {
                UIHelper.GetEditableTitlebar.CloseTitlebar();
            }
        };
        PointerPressed += delegate
        {
            if (UIHelper.GetEditableTitlebar.IsOpen)
            {
                UIHelper.GetEditableTitlebar.CloseTitlebar();
            }
        };
    }
    
    private async Task Drop(object? sender, DragEventArgs e)
    {
        if (DataContext is not MainViewModel vm)
            return;

        var data = e.Data.GetFiles();
        if (data == null)
        {
            // TODO Handle URL and folder drops
            return;
        }

        var storageItems = data as IStorageItem[] ?? data.ToArray();
        var firstFile = storageItems.FirstOrDefault();
        var path = RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? firstFile.Path.AbsolutePath : firstFile.Path.LocalPath;
        await NavigationHelper.LoadPicFromStringAsync(path, vm).ConfigureAwait(false);
        foreach (var file in storageItems.Skip(1))
        {
            // TODO Open each file in a new window if the setting to open in the same window is false
        }
    }
}