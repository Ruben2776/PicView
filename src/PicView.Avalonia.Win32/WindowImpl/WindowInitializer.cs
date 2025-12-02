using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using PicView.Avalonia.FileSystem;
using PicView.Avalonia.Functions;
using PicView.Avalonia.Input;
using PicView.Avalonia.Interfaces;
using PicView.Avalonia.Navigation;
using PicView.Avalonia.Update;
using PicView.Avalonia.ViewModels;
using PicView.Avalonia.Win32.PlatformUpdate;
using PicView.Avalonia.Win32.Printing;
using PicView.Avalonia.Win32.Views;
using PicView.Avalonia.WindowBehavior;
using PicView.Core.Config;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Avalonia.Win32.WindowImpl;

public class WindowInitializer : IPlatformSpecificUpdate
{
    private AboutWindow? _aboutWindow;
    private BatchResizeWindow? _batchResizeWindow;
    private ConvertWindow? _convertWindow;
    private EffectsWindow? _effectsWindow;
    private ImageInfoWindow? _imageInfoWindow;
    private KeybindingsWindow? _keybindingsWindow;
    private SettingsWindow? _settingsWindow;
    private SingleImageResizeWindow? _singleImageResizeWindow;
    private PrintPreviewWindow? _printPreviewWindow;

    public async Task HandlePlatofrmUpdate(UpdateInfo updateInfo, string tempPath)
    {
        await WinUpdateHelper.HandleWindowsUpdate(updateInfo, tempPath);
    }

    public void ShowAboutWindow(MainViewModel vm)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            Set();
        }
        else
        {
            Dispatcher.UIThread.InvokeAsync(Set);
        }

        return;

        void Set()
        {
            if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            {
                return;
            }

            if (_aboutWindow is null)
            {
                _aboutWindow = new AboutWindow
                {
                    DataContext = vm,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };
                _aboutWindow.Show(desktop.MainWindow);
                _aboutWindow.Closing += (_, _) => _aboutWindow = null;
            }
            else
            {
                if (_aboutWindow.WindowState == WindowState.Minimized)
                {
                    WindowFunctions.ShowMinimizedWindow(_aboutWindow);
                }
                else
                {
                    _aboutWindow.Show();
                }
            }

            _ = FunctionsMapper.CloseMenus();
        }
    }

    public async Task ShowImageInfoWindow(MainViewModel vm)
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return;
        }

        if (_imageInfoWindow is null)
        {
            if (vm.Window.ImageInfoWindowConfig?.WindowProperties is null)
            {
                vm.Window.ImageInfoWindowConfig = new ImageInfoWindowConfig();
                await vm.Window.ImageInfoWindowConfig.LoadAsync();
            }

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                vm.Exif ??= new ExifViewModel();
                vm.InfoWindow = new ImageInfoWindowViewModel();
                _imageInfoWindow = new ImageInfoWindow(vm.Window.ImageInfoWindowConfig)
                {
                    DataContext = vm
                };
                Show();
                _imageInfoWindow.Closing += (_, _) =>
                {
                    _imageInfoWindow = null;
                    vm.Exif?.Dispose();
                    vm.Exif = null;
                    vm.InfoWindow.Dispose();
                    vm.InfoWindow = null;
                };
            });
        }
        else
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (_imageInfoWindow.WindowState == WindowState.Minimized)
                {
                    WindowFunctions.ShowMinimizedWindow(_imageInfoWindow);
                }
                else
                {
                    Show();
                }
            });
        }

        await FunctionsMapper.CloseMenus();

        return;

        void Show()
        {
            WindowFunctions.InitializeWindowSizeAndPosition(_imageInfoWindow,
                vm.Window.ImageInfoWindowConfig.WindowProperties);
            _imageInfoWindow.Show(desktop.MainWindow);
        }
    }

    public async Task ShowKeybindingsWindow(MainViewModel vm)
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return;
        }

        if (_keybindingsWindow is null)
        {
            if (vm.Window.KeybindingWindowConfig?.WindowProperties is null)
            {
                vm.Window.KeybindingWindowConfig = new KeybindingWindowConfig();
                await vm.Window.KeybindingWindowConfig.LoadAsync();
            }
            
            if (vm.Keybindings is null)
            {
                vm.Keybindings = new KeybindingsViewModel();
                vm.Keybindings.ResetKeybindingsCommand = new ReactiveCommand(async (_, _) =>
                {
                    _keybindingsWindow.Close();
                    await Task.Run(() =>
                    {
                        KeybindingManager.SetDefaultKeybindings(vm.PlatformService);
                        FunctionsKeyHelper.ResetKeybindings(vm.Keybindings);
                    }, CancellationToken.None);
                    await ShowKeybindingsWindow(vm);
                });
            }
            
            _ = Task.Run(() =>
            {
                FunctionsKeyHelper.LoadKeybindingsViewModel(vm.Keybindings);
            });

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                _keybindingsWindow = new KeybindingsWindow(vm.Window.KeybindingWindowConfig)
                {
                    DataContext = vm
                };
                Show();
                _keybindingsWindow.Closing += (_, _) =>
                {
                    _keybindingsWindow?.Dispose();
                    _keybindingsWindow = null;
                };
            });
        }
        else
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (_keybindingsWindow.WindowState == WindowState.Minimized)
                {
                    WindowFunctions.ShowMinimizedWindow(_keybindingsWindow);
                }
                else
                {
                    Show();
                }
            });
        }

        await FunctionsMapper.CloseMenus();

        return;

        void Show()
        {
            WindowFunctions.InitializeWindowSizeAndPosition(_keybindingsWindow,
                vm.Window.KeybindingWindowConfig.WindowProperties);
            _keybindingsWindow.Show(desktop.MainWindow);
        }
    }

    public async Task ShowSettingsWindow(MainViewModel vm)
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return;
        }

        if (vm.Window.SettingsWindowConfig?.WindowProperties is null)
        {
            vm.Window.SettingsWindowConfig = new SettingsWindowConfig();
            await vm.Window.SettingsWindowConfig.LoadAsync();
        }

        if (_settingsWindow is null)
        {
            vm.AssociationsViewModel ??= new FileAssociationsViewModel();
            vm.SettingsViewModel ??= new SettingsViewModel();
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                _settingsWindow = new SettingsWindow(vm.Window.SettingsWindowConfig)
                {
                    DataContext = vm,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };

                Show();
                _settingsWindow.Closing += (_, _) => _settingsWindow = null;
            });
        }
        else
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (_settingsWindow.WindowState == WindowState.Minimized)
                {
                    WindowFunctions.ShowMinimizedWindow(_settingsWindow);
                }
                else
                {
                    Show();
                }
            });
        }

        await FunctionsMapper.CloseMenus();

        return;

        void Show()
        {
            WindowFunctions.InitializeWindowSizeAndPosition(_settingsWindow,
                vm.Window.SettingsWindowConfig.WindowProperties);
            _settingsWindow.Show(desktop.MainWindow);
        }
    }

    public void ShowSingleImageResizeWindow(MainViewModel vm)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            Set();
        }
        else
        {
            Dispatcher.UIThread.InvokeAsync(Set);
        }

        return;

        void Set()
        {
            if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            {
                return;
            }

            if (_singleImageResizeWindow is null)
            {
                _singleImageResizeWindow = new SingleImageResizeWindow
                {
                    DataContext = vm,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };
                _singleImageResizeWindow.Show(desktop.MainWindow);
                _singleImageResizeWindow.Closing += (_, _) => _singleImageResizeWindow = null;
            }
            else
            {
                if (_singleImageResizeWindow.WindowState == WindowState.Minimized)
                {
                    WindowFunctions.ShowMinimizedWindow(_singleImageResizeWindow);
                }
                else
                {
                    _singleImageResizeWindow.Show();
                }
            }

            _ = FunctionsMapper.CloseMenus();
        }
    }

    public async Task ShowBatchResizeWindow(MainViewModel vm)
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return;
        }

        if (_batchResizeWindow is null)
        {
            if (vm.Window.BatchResizeWindowConfig?.WindowProperties is null)
            {
                vm.Window.BatchResizeWindowConfig = new BatchResizeWindowConfig();
                await vm.Window.BatchResizeWindowConfig.LoadAsync();
            }

            vm.BatchResizeViewModel = new BatchResizeViewModel(NavigationManager.CanNavigate(vm),
                FilePicker.SelectDirectory, FilePicker.SelectFile, vm.PicViewer.FileInfo.CurrentValue,
                vm.PlatformService.GetFiles);
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                _batchResizeWindow = new BatchResizeWindow(vm.Window.BatchResizeWindowConfig)
                {
                    DataContext = vm,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };
                Show();
                _batchResizeWindow.Closing += (_, _) =>
                {
                    _batchResizeWindow.Dispose();
                    _batchResizeWindow = null;
                    vm.BatchResizeViewModel.Dispose();
                    vm.BatchResizeViewModel = null;
                };
            });
        }
        else
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (_batchResizeWindow.WindowState == WindowState.Minimized)
                {
                    WindowFunctions.ShowMinimizedWindow(_batchResizeWindow);
                }
                else
                {
                    Show();
                }
            });

        }

        await FunctionsMapper.CloseMenus();
        
        return;

        void Show()
        {
            WindowFunctions.InitializeWindowSizeAndPosition(_batchResizeWindow,
                vm.Window.BatchResizeWindowConfig.WindowProperties);
            _batchResizeWindow.Show(desktop.MainWindow);
        }
    }

    public void ShowEffectsWindow(MainViewModel vm)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            Set();
        }
        else
        {
            Dispatcher.UIThread.InvokeAsync(Set);
        }

        return;

        void Set()
        {
            if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            {
                return;
            }

            if (_effectsWindow is null)
            {
                _effectsWindow = new EffectsWindow
                {
                    DataContext = vm,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };
                _effectsWindow.Show(desktop.MainWindow);
                _effectsWindow.Closing += (_, _) => _effectsWindow = null;
            }
            else
            {
                if (_effectsWindow.WindowState == WindowState.Minimized)
                {
                    WindowFunctions.ShowMinimizedWindow(_effectsWindow);
                }
                else
                {
                    _effectsWindow.Show();
                }
            }

            _ = FunctionsMapper.CloseMenus();
        }
    }

    public void ShowConvertWindow(MainViewModel vm)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            Set();
        }
        else
        {
            Dispatcher.UIThread.InvokeAsync(Set);
        }

        return;

        void Set()
        {
            if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            {
                return;
            }

            if (_convertWindow is null)
            {
                _convertWindow = new ConvertWindow
                {
                    DataContext = vm,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };
                _convertWindow.Show(desktop.MainWindow);
                _convertWindow.Closing += (_, _) => _convertWindow = null;
            }
            else
            {
                if (_convertWindow.WindowState == WindowState.Minimized)
                {
                    WindowFunctions.ShowMinimizedWindow(_convertWindow);
                }
                else
                {
                    _convertWindow.Show();
                }
            }

            _ = FunctionsMapper.CloseMenus();
        }
    }

    public void ShowPrintPreviewWindow(MainViewModel vm, string path)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            Set();
        }
        else
        {
            Dispatcher.UIThread.InvokeAsync(Set);
        }

        return;

        void Set()
        {
            if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            {
                return;
            }

            if (_printPreviewWindow is null)
            {
                vm.PrintPreview = new PrintPreviewViewModel();

                // TODO: Move this initialization to its own dedicated class

                _printPreviewWindow = new PrintPreviewWindow
                {
                    DataContext = vm,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };

                vm.PrintPreview.PrintCommand.SubscribeAwait(async (_, _) =>
                {
                    await _printPreviewWindow?.RunPrintAsync(vm);
                })
                .AddTo(vm.PrintPreview.Disposables);

                vm.PrintPreview.CancelCommand.SubscribeAwait(async (_, _) =>
                {
                    await Dispatcher.UIThread.InvokeAsync(() => _printPreviewWindow?.Close());
                }).AddTo(vm.PrintPreview.Disposables);

                _printPreviewWindow.Show(desktop.MainWindow);
                _printPreviewWindow.Closing += (_, _) => _printPreviewWindow = null;

                Task.Run(() =>
                {
                    PrintInitialization.Initialize(vm, path, _printPreviewWindow);
                });
            }
            else
            {
                if (_printPreviewWindow.WindowState == WindowState.Minimized)
                {
                    WindowFunctions.ShowMinimizedWindow(_printPreviewWindow);
                }
                else
                {
                    _printPreviewWindow.Show();
                }
            }

            _ = FunctionsMapper.CloseMenus();
        }
    }
}