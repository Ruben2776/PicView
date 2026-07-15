using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.FileSystem;
using PicView.Avalonia.Input;
using PicView.Avalonia.Functions;
using PicView.Avalonia.Interfaces;
using PicView.Avalonia.WindowBehavior;
using PicView.Core.Config;
using PicView.Core.DebugTools;
using PicView.Core.IPlatform;
using PicView.Core.Update;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Avalonia.Services;

public class WindowInitializer(IWindowProvider provider) : IWindowInitializer, IPlatformSpecificUpdate
{
    private Window? _aboutWindow;
    private Window? _batchResizeWindow;
    private Window? _convertWindow;
    private Window? _effectsWindow;
    private Window? _imageInfoWindow;
    private Window? _keybindingsWindow;
    private Window? _settingsWindow;
    private Window? _singleImageResizeWindow;
    private Window? _printPreviewWindow;
    private Window? _fileAssociationsWindow;

    public async Task HandlePlatformUpdate(UpdateInfo updateInfo, string tempPath)
    {
        await provider.HandlePlatformUpdate(updateInfo, tempPath);
    }

    public MainWindow CreateMainWindow()
    {
        var window = provider.CreateMainWindow();
        if (Application.Current?.DataContext is CoreViewModel core &&
            window.DataContext is MainWindowViewModel vm)
        {
            core.MainWindows.MainWindows.Add(vm);
            core.MainWindows.ActiveWindow.Value = vm;
        }
        return window;
    }

    public void ShowAboutWindow()
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
            if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop ||
                Application.Current.DataContext is not CoreViewModel core)
            {
                return;
            }

            if (_aboutWindow is null)
            {
                core.AboutView ??= new AboutViewModel(this);
                _aboutWindow = provider.CreateAboutWindow();
                _aboutWindow.DataContext = core;
                _aboutWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;

                if (desktop.MainWindow is not null)
                {
                    _aboutWindow.Show(desktop.MainWindow);
                }
                else
                {
                    _aboutWindow.Show();
                }

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
                    _aboutWindow.Activate();
                }
            }
        }
    }

    public async Task ShowImageInfoWindow(MainWindowViewModel vm)
    {
        if (_imageInfoWindow is null)
        {
            vm.InfoWindow ??= new ImageInfoWindowViewModel();
            if (vm.InfoWindow.ImageInfoWindowConfig?.WindowProperties is null)
            {
                vm.InfoWindow.ImageInfoWindowConfig = new ImageInfoWindowConfig();
                await vm.InfoWindow.ImageInfoWindowConfig.LoadAsync();
            }

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                vm.Exif ??= new ExifViewModel();
                _imageInfoWindow = provider.CreateImageInfoWindow(vm);
                WindowFunctions.InitializeWindowSizeAndPosition(_imageInfoWindow,
                    vm.InfoWindow.ImageInfoWindowConfig.WindowProperties);
                _imageInfoWindow.Show();
                _imageInfoWindow.Closing += async (_, _) =>
                {
                    _imageInfoWindow = null;
                    vm.Exif?.Dispose();
                    vm.Exif = null;
                    if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                    {
                        desktop.MainWindow?.Focus();
                    }

                    await vm.InfoWindow.ImageInfoWindowConfig.SaveAsync();
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
                    _imageInfoWindow.Activate();
                }
            });
        }
    }

    public async Task ShowKeybindingsWindow()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop
            || Application.Current.DataContext is not CoreViewModel core)
        {
            return;
        }

        if (_keybindingsWindow is null)
        {
            if (core.Keybindings is null)
            {
                core.Keybindings = new KeybindingsViewModel();
                core.Keybindings.ResetKeybindingsCommand = new ReactiveCommand(async (_, ct) =>
                {
                    core.MainWindows.ActiveWindow.CurrentValue.IsLoadingIndicatorShown.Value = true;
                    _keybindingsWindow?.Close();
                    await Task.Run(() =>
                    {
                        KeybindingManager.SetDefaultKeybindings(core.PlatformService);
                        FunctionsKeyHelper.ResetKeybindings(core.Keybindings);
                    }, ct);
                    await ShowKeybindingsWindow();
                    core.MainWindows.ActiveWindow.CurrentValue.IsLoadingIndicatorShown.Value = false;
                });

                _ = Task.Run(async () =>
                {
                    await KeybindingManager.LoadKeybindings(core.PlatformService);
                    FunctionsKeyHelper.LoadKeybindingsViewModel(core.Keybindings);
                });
            }

            if (core.Keybindings.WindowConfig is null)
            {
                core.Keybindings.WindowConfig = new KeybindingWindowConfig();
                await core.Keybindings.WindowConfig.LoadAsync();
            }

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                _keybindingsWindow = provider.CreateKeybindingsWindow(core.Keybindings.WindowConfig);
                _keybindingsWindow.DataContext = core;

                Show();
                _keybindingsWindow.Closing += async (_, _) =>
                {
                    if (_keybindingsWindow is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                    await core.Keybindings.WindowConfig.SaveAsync();
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

        return;

        void Show()
        {
            _keybindingsWindow?.Show(desktop.MainWindow);
        }
    }

    public async ValueTask ShowSettingsWindow()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop ||
            Application.Current.DataContext is not CoreViewModel core)
        {
            return;
        }

        if (core.SettingsViewModel is null)
        {
            core.SettingsViewModel = new SettingsViewModel(core.Translation);
            core.SettingsViewModel.Initialize(new ThemeService(), new LanguageService(), new ImageSettingsService());
            core.SettingsViewModel.WindowMargin.Subscribe(_ =>
            {
                WindowResizing.SetSize(desktop.MainWindow as MainWindow, WindowResizeReason.Application);
            }, DebugHelper.LogError(nameof(core.SettingsViewModel), nameof(core.SettingsViewModel.WindowMargin)));
        }

        if (core.SettingsViewModel.SettingsWindowConfig is null)
        {
            core.SettingsViewModel.SettingsWindowConfig = new SettingsWindowConfig();
            await core.SettingsViewModel.SettingsWindowConfig.LoadAsync();
        }

        if (_settingsWindow is null)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                _settingsWindow = provider.CreateSettingsWindow(core.SettingsViewModel.SettingsWindowConfig);
                _settingsWindow.DataContext = core;

                Show();
                _settingsWindow.Closing += async (_, _) =>
                {
                    _settingsWindow.Hide();
                    core.SettingsViewModel.SettingsWindowConfig.WindowProperties.LastTab = core.SettingsViewModel.GetLastTabId();
                    desktop.MainWindow?.Focus();   
                    _settingsWindow = null;
                    await core.SettingsViewModel.SettingsWindowConfig.SaveAsync();
                    await SaveSettingsAsync();
                };
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

        return;

        void Show()
        {
            WindowFunctions.InitializeWindowSizeAndPosition(_settingsWindow,
                core.SettingsViewModel.SettingsWindowConfig.WindowProperties);
            _settingsWindow?.Show();
        }
    }

    public async Task ShowEffectsWindow()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop ||
            Application.Current.DataContext is not CoreViewModel core)
        {
            return;
        }

        if (_effectsWindow is null)
        {
            core.Effects ??= new EffectsViewModel();
            if (core.Effects.WindowConfig is null)
            {
                core.Effects.WindowConfig = new EffectsWindowConfig();
                await core.Effects.WindowConfig.LoadAsync();
            }

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                _effectsWindow = provider.CreateEffectsWindow(core.Effects.WindowConfig);
                _effectsWindow.DataContext = core;
                WindowFunctions.InitializeWindowPosition(_effectsWindow, core.Effects.WindowConfig.WindowProperties);
                _effectsWindow.Show(desktop.MainWindow);
                _effectsWindow.Closing += async (_, _) =>
                {
                    desktop.MainWindow?.Focus();
                    _effectsWindow = null;
                    await core.Effects.WindowConfig.SaveAsync();
                };
            });
        }
        else
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (_effectsWindow.WindowState == WindowState.Minimized)
                {
                    WindowFunctions.ShowMinimizedWindow(_effectsWindow);
                }
                else
                {
                    _effectsWindow.Activate();
                }
            });
        }
    }

    public void ShowSingleImageResizeWindow()
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
            if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop ||
                Application.Current.DataContext is not CoreViewModel core)
            {
                return;
            }

            if (_singleImageResizeWindow is null)
            {
                var activeWindow = core.MainWindows.ActiveWindow.CurrentValue;
                activeWindow.ResizeImageViewModel ??= new ResizeImageViewModel();
                _singleImageResizeWindow = provider.CreateSingleImageResizeWindow(activeWindow);
                _singleImageResizeWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;

                _singleImageResizeWindow.Show(desktop.MainWindow);
                _singleImageResizeWindow.Closing += (_, _) =>
                {
                    desktop.MainWindow?.Focus();
                    _singleImageResizeWindow = null;
                };
            }
            else
            {
                if (_singleImageResizeWindow.WindowState == WindowState.Minimized)
                {
                    WindowFunctions.ShowMinimizedWindow(_singleImageResizeWindow);
                }
                else
                {
                    _singleImageResizeWindow.Activate();
                }
            }
        }
    }

    public async ValueTask ShowBatchResizeWindow()
    {
        if (Application.Current.DataContext is not CoreViewModel core)
        {
            return;
        }
        
        if (_batchResizeWindow is null)
        {
            core.BatchResize ??= new BatchResizeViewModel(
                FilePicker.SelectDirectory, FilePicker.SelectFile, core.MainWindows.ActiveWindow.CurrentValue.WindowTabs.ActiveTab.CurrentValue.FileInfo.CurrentValue,
                core.PlatformService.GetFiles);
            if (core.BatchResize.Config is null)
            {
                core.BatchResize.Config = new BatchResizeWindowConfig();
                await core.BatchResize.Config.LoadAsync();
            }
        
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                _batchResizeWindow = provider.CreateBatchResizeWindow(core.BatchResize.Config);
                _batchResizeWindow.DataContext = core;
                Show();
                _batchResizeWindow.Closing += (_, _) =>
                {
                    if (_batchResizeWindow is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                    _ = core.BatchResize.Config.SaveAsync();
                    _batchResizeWindow = null;
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
        
        return;
        
        void Show()
        {
            _batchResizeWindow?.Show();
        }
    }

    public void ShowConvertWindow()
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
            if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop ||
                Application.Current.DataContext is not CoreViewModel core)
            {
                return;
            }

            if (_convertWindow is null)
            {
                _convertWindow = provider.CreateConvertWindow();
                _convertWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                _convertWindow.DataContext = core.MainWindows.ActiveWindow.CurrentValue;
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
                    _convertWindow.Activate();
                }
            }
        }
    }

    public void ShowFileAssociationsWindow()
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
            if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop ||
                Application.Current.DataContext is not CoreViewModel core)
            {
                return;
            }

            if (core.AssociationsViewModel is null)
            {
                core.AssociationsViewModel = new FileAssociationsViewModel();
                _fileAssociationsWindow = provider.CreateFileAssociationsWindow();
                if (_fileAssociationsWindow is null)
                {
                    return;
                }

                _fileAssociationsWindow.DataContext = core;
                _fileAssociationsWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;

                if (desktop.MainWindow is not null)
                {
                    _fileAssociationsWindow.Show(desktop.MainWindow);
                }
                else
                {
                    _fileAssociationsWindow.Show();
                }
            }
            else
            {
                if (_fileAssociationsWindow.WindowState == WindowState.Minimized)
                {
                    WindowFunctions.ShowMinimizedWindow(_fileAssociationsWindow);
                }
                else
                {
                    _fileAssociationsWindow.Activate();
                }
            }
        }
    }

    public async Task ShowPrintWindow(string path, MainWindowViewModel vm)
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return;
        }

        if (_printPreviewWindow is null)
        {
            vm.PrintPreview ??= new PrintPreviewViewModel();
            
            if (vm.PrintPreview.PrintWindowConfig is null)
            {
                vm.PrintPreview.PrintWindowConfig = new PrintWindowConfig();
                await vm.PrintPreview.PrintWindowConfig.LoadAsync();
            }

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                _printPreviewWindow = provider.CreatePrintPreviewWindow(vm.PrintPreview.PrintWindowConfig);
                _printPreviewWindow.DataContext = vm;
                _printPreviewWindow.Show(desktop.MainWindow);
                
                _printPreviewWindow.Closing += (_, _) =>
                {
                    desktop.MainWindow?.Focus();
                    _printPreviewWindow = null;
                };
            });

            vm.PrintPreview.CancelCommand.Subscribe( _ => 
            {
                _printPreviewWindow?.Close();
            }).AddTo(vm.PrintPreview.Disposables);
            
            await provider.InitializePrintAsync(vm, path, _printPreviewWindow);
        }
        else
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (_printPreviewWindow.WindowState == WindowState.Minimized)
                {
                    WindowFunctions.ShowMinimizedWindow(_printPreviewWindow);
                }
                else
                {
                    _printPreviewWindow.Activate();
                }
            });
        }
    }
}
