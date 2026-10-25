using Avalonia;
using Avalonia.Controls;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.FileSystem;
using PicView.Avalonia.Input;
using PicView.Avalonia.Interfaces;
using PicView.Avalonia.Printing;
using PicView.Avalonia.WindowBehavior;
using PicView.Core.Config;
using PicView.Core.DebugTools;
using PicView.Core.ImageEffects;
using PicView.Core.IPlatform;
using PicView.Core.Update;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Avalonia.Services;

/// <summary>
/// Opens a Main Window's Tool Windows, which belong to it, and the App Windows, which belong to the whole app.
/// </summary>
/// <param name="mainWindow">The Main Window whose Tool Windows this opens.</param>
public class WindowInitializer(IWindowProvider provider, MainWindow mainWindow) : IPlatformSpecificUpdate
{
    private readonly SecondaryWindowOpener _toolWindows = new(() => mainWindow, provider, () => mainWindow,
        () => (mainWindow.DataContext as MainWindowViewModel)?.Core);

    /// <summary>
    /// Opens the App Windows, shared by every Main Window. A window opened outside the lifecycle, as in some tests,
    /// opens them as its own.
    /// </summary>
    private SecondaryWindowOpener AppWindows => mainWindow.Lifecycle?.AppWindows ?? _toolWindows;

    /// <summary>
    /// Opens Batch Resize, the Tool Window that belongs to the whole app. A window opened outside the lifecycle opens
    /// it as its own.
    /// </summary>
    private SecondaryWindowOpener BatchResizeWindows => mainWindow.Lifecycle?.AppToolWindows ?? _toolWindows;

    /// <summary>
    /// The Tool Window this main window opened most recently that is still open, else the newest App Window.
    /// </summary>
    public Window? NewestOpenWindow =>
        _toolWindows.NewestOpenWindow ?? BatchResizeWindows.NewestOpenWindow ?? AppWindows.NewestOpenWindow;

    /// <summary>
    /// Closes this window's Tool Windows, as it closes.
    /// </summary>
    public void CloseToolWindows() => _toolWindows.CloseAll();

    public async Task HandlePlatformUpdate(UpdateInfo updateInfo, string tempPath)
    {
        await provider.HandlePlatformUpdate(updateInfo, tempPath).ConfigureAwait(false);
    }

    public void ShowAboutWindow()
    {
        if (CurrentCore() is not { } core)
        {
            return;
        }

        core.AboutView ??= new AboutViewModel(this);
        _ = AppWindows.OpenAsync(SecondaryWindowKind.About, new SecondaryWindowSpec(
            factory => factory.CreateAboutWindow())
        {
            Configure = window =>
            {
                window.DataContext = core;
                window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }
        });
    }

    public void ShowImageInfoWindow(MainWindowViewModel vm)
    {
        vm.InfoWindow ??= new ImageInfoWindowViewModel(vm);
        if (vm.InfoWindow.ImageInfoWindowConfig?.WindowProperties is null)
        {
            vm.InfoWindow.ImageInfoWindowConfig = new ImageInfoWindowConfig();
            vm.InfoWindow.ImageInfoWindowConfig.Load();
        }

        vm.Exif ??= new ExifViewModel();
        _ = _toolWindows.OpenAsync(SecondaryWindowKind.ImageInfo, new SecondaryWindowSpec(
            factory => factory.CreateImageInfoWindow(vm))
        {
            Configure = window => WindowFunctions.InitializeWindowSizeAndPosition(window,
                vm.InfoWindow.ImageInfoWindowConfig.WindowProperties),
            OnClosing = async _ =>
            {
                vm.Exif?.Dispose();
                vm.Exif = null;
                await vm.InfoWindow.ImageInfoWindowConfig.SaveAsync().ConfigureAwait(false);
                vm.InfoWindow.Dispose();
                vm.InfoWindow = null;
            }
        });
    }

    public void ShowKeybindingsWindow()
    {
        if (CurrentCore() is not { } core)
        {
            return;
        }

        if (core.Keybindings is null)
        {
            core.Keybindings = new KeybindingsViewModel();
            Keymaps.Load(core.PlatformService);
        }

        if (core.Keybindings.WindowConfig?.WindowProperties is null)
        {
            core.Keybindings.WindowConfig = new KeybindingWindowConfig();
            core.Keybindings.WindowConfig.Load();
        }

        _ = AppWindows.OpenAsync(SecondaryWindowKind.Keybindings, new SecondaryWindowSpec(
            factory => factory.CreateKeybindingsWindow(core.Keybindings.WindowConfig))
        {
            Configure = window => window.DataContext = core,
            OnClosing = async window =>
            {
                if (window is IDisposable disposable)
                {
                    disposable.Dispose();
                }

                await core.Keybindings.WindowConfig.SaveAsync().ConfigureAwait(false);
            }
        });
    }

    public async ValueTask ShowSettingsWindow()
    {
        if (CurrentCore() is not { } core)
        {
            return;
        }

        if (core.SettingsViewModel is null)
        {
            core.SettingsViewModel = new SettingsViewModel(core.Translation);
            core.SettingsViewModel.Initialize(new ThemeService(), new LanguageService(), new ImageSettingsService());
            core.SettingsViewModel.WindowMargin.Skip(1).Subscribe(_ =>
            {
                foreach (var window in mainWindow.Lifecycle?.Open ?? [mainWindow])
                {
                    window.WindowFitter.Refit();
                }
            }, DebugHelper.LogError(nameof(core.SettingsViewModel), nameof(core.SettingsViewModel.WindowMargin)));
        }

        if (core.SettingsViewModel.SettingsWindowConfig is null)
        {
            core.SettingsViewModel.SettingsWindowConfig = new SettingsWindowConfig();
            core.SettingsViewModel.SettingsWindowConfig.Load();
        }

        var config = core.SettingsViewModel.SettingsWindowConfig;
        await AppWindows.OpenAsync(SecondaryWindowKind.Settings, new SecondaryWindowSpec(
            factory => factory.CreateSettingsWindow(config))
        {
            Configure = window =>
            {
                window.DataContext = core;
                WindowFunctions.InitializeWindowSizeAndPosition(window, config.WindowProperties);
            },
            OnClosing = async window =>
            {
                window.Hide();
                config.WindowProperties.LastTab = core.SettingsViewModel.GetLastTabId();
                await config.SaveAsync().ConfigureAwait(false);
                await SaveSettingsAsync().ConfigureAwait(false);
            }
        }).ConfigureAwait(false);
    }

    public async Task ShowEffectsWindow()
    {
        if (CurrentCore() is not { } core)
        {
            return;
        }

        core.Effects ??= new EffectsViewModel(
            core.MainWindows.ActiveWindow
                .Select(window => window?.WindowTabs.ActiveTab.AsObservable() ?? Observable.Return<TabViewModel>(null!))
                .Switch()
                .Select(tab => tab?.FileInfo.AsObservable() ?? Observable.Return<FileInfo?>(null))
                .Switch(),
            ImageEffectsHelper.ApplyEffects);
        if (core.Effects.WindowConfig is null)
        {
            core.Effects.WindowConfig = new EffectsWindowConfig();
            await core.Effects.WindowConfig.LoadAsync().ConfigureAwait(false);
        }

        var config = core.Effects.WindowConfig;
        await _toolWindows.OpenAsync(SecondaryWindowKind.Effects, new SecondaryWindowSpec(
            factory => factory.CreateEffectsWindow(config), WindowOwner.MainWindowExceptMacOS)
        {
            Configure = window =>
            {
                window.DataContext = core;
                WindowFunctions.InitializeWindowPosition(window, config.WindowProperties);
            },
            OnClosing = async _ => await config.SaveAsync().ConfigureAwait(false)
        }).ConfigureAwait(false);
    }

    public void ShowSingleImageResizeWindow()
    {
        if (CurrentCore() is not { } core)
        {
            return;
        }

        var activeWindow = core.MainWindows.ActiveWindow.CurrentValue;
        activeWindow.ResizeImageViewModel ??= new ResizeImageViewModel();
        _ = _toolWindows.OpenAsync(SecondaryWindowKind.SingleImageResize, new SecondaryWindowSpec(
            factory => factory.CreateSingleImageResizeWindow(activeWindow), WindowOwner.MainWindowExceptMacOS)
        {
            Configure = window => window.WindowStartupLocation = WindowStartupLocation.CenterOwner
        });
    }

    public async ValueTask ShowBatchResizeWindow()
    {
        if (CurrentCore() is not { } core)
        {
            return;
        }

        var windows = BatchResizeWindows;
        if (windows.IsOpen(SecondaryWindowKind.BatchResize))
        {
            // Brought to the front, keeping the files and settings it is editing
            await windows.OpenAsync(SecondaryWindowKind.BatchResize, new SecondaryWindowSpec(_ => null))
                .ConfigureAwait(false);
            return;
        }

        var config = new BatchResizeWindowConfig();
        // Back on the UI thread, which reads the Main Window's tab
        await config.LoadAsync().ConfigureAwait(true);
        var batch = new BatchResizeViewModel(FilePicker.SelectDirectory, FilePicker.SelectFile,
            core.PlatformService.GetFiles, core.PlatformService.LocateOnDisk,
            config.WindowProperties ?? new BatchResizeWindowConfig.BatchResizeWindowProperties());
        // The files around what this Main Window shows
        if ((mainWindow.DataContext as MainWindowViewModel)?.WindowTabs.ActiveTab.CurrentValue is { } tab)
        {
            batch.Prefill(tab.FileInfo?.CurrentValue, tab.Model.PixelWidth, tab.Model.PixelHeight);
        }

        core.BatchResize = batch;
        await windows.OpenAsync(SecondaryWindowKind.BatchResize, new SecondaryWindowSpec(
            factory => factory.CreateBatchResizeWindow(config))
        {
            Configure = window =>
            {
                window.DataContext = core;
                // After its opener lets go of it, so the last window to close runs App Shutdown
                window.Closed += async (_, _) =>
                {
                    if (mainWindow.Lifecycle is { } lifecycle)
                    {
                        await lifecycle.ShutdownIfNothingIsOpenAsync().ConfigureAwait(true);
                    }
                };
            },
            OnClosing = async window =>
            {
                if (window is IDisposable disposable)
                {
                    disposable.Dispose();
                }

                batch.RememberSettings();
                batch.Dispose();
                if (core.BatchResize == batch)
                {
                    core.BatchResize = null;
                }

                await config.SaveAsync().ConfigureAwait(false);
            }
        }).ConfigureAwait(false);
    }

    public void ShowConvertWindow()
    {
        if (CurrentCore() is not { } core)
        {
            return;
        }

        _ = _toolWindows.OpenAsync(SecondaryWindowKind.Convert, new SecondaryWindowSpec(
            factory => factory.CreateConvertWindow(), WindowOwner.MainWindowExceptMacOS)
        {
            Configure = window =>
            {
                window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                window.DataContext = mainWindow.DataContext;
            }
        });
    }

    public void ShowFileAssociationsWindow()
    {
        if (CurrentCore() is not { } core)
        {
            return;
        }

        // The view model outlives the window, so reopening only creates the window again
        core.AssociationsViewModel ??= new FileAssociationsViewModel();
        _ = AppWindows.OpenAsync(SecondaryWindowKind.FileAssociations, new SecondaryWindowSpec(
            factory => factory.CreateFileAssociationsWindow())
        {
            Configure = window =>
            {
                window.DataContext = core;
                window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }
        });
    }

    /// <summary>
    /// Opens the Print window on a Print Job of what this Main Window shows. The window shows with its spinner first;
    /// the Print Job then loads off the UI thread.
    /// </summary>
    public async Task ShowPrintWindow(MainWindowViewModel vm)
    {
        // The window opens at its remembered size and place, so this small file is the only thing read before it shows
        var config = new PrintWindowConfig();
        await config.LoadAsync().ConfigureAwait(false);

        var preview = new PrintPreviewViewModel { PrintWindowConfig = config };
        await _toolWindows.OpenAsync(SecondaryWindowKind.PrintPreview, new SecondaryWindowSpec(
            factory => factory.CreatePrintPreviewWindow(config), WindowOwner.MainWindowExceptMacOS)
        {
            Configure = window =>
            {
                vm.PrintPreview = preview;
                window.DataContext = vm;
            },
            OnOpened = window =>
            {
                preview.Disposables.Add(PrintJob.Open(vm, preview, provider.CreateSystemPrinters(),
                    new PdfSavePathPicker(window), config.WindowProperties, window.Close));
                return Task.CompletedTask;
            }
        }).ConfigureAwait(false);

        if (vm.PrintPreview != preview)
        {
            // The Print window was open already, with its own Print Job
            preview.Dispose();
        }
    }

    private CoreViewModel? CurrentCore() => (mainWindow.DataContext as MainWindowViewModel)?.Core;
}
