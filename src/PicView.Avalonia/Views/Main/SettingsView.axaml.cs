using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Media;
using PicView.Avalonia.Input;
using PicView.Avalonia.UI;
using PicView.Avalonia.ViewModels;
using PicView.Avalonia.WindowBehavior;
using PicView.Core.Keybindings;
using PicView.Core.Sizing;
using PicView.Core.ViewModels;
using R3;

// ReSharper disable CompareOfFloatsByEqualityOperator

namespace PicView.Avalonia.Views.Main;

public partial class SettingsView : UserControl
{ 
    public SettingsView()
    {
        InitializeComponent();
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            FileAssociationsTabItem.IsEnabled = false;
        }

        Loaded += OnLoaded;
    }

    #region Properties

    private MainViewModel? ViewModel => DataContext as MainViewModel;

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e) => HandleMouseButtonNavigation(e);

    private static CompositeDisposable? _marginSubscription;
    private readonly Stack<TabItem?> _backStack = new();
    private readonly Stack<TabItem?> _forwardStack = new();
    private TabItem? _currentTab;

    #endregion

    #region Initialization

    private void OnLoaded(object? sender, EventArgs e)
    {
        if (DataContext is not MainViewModel)
        {
            return;
        }
        
        InitializeViewModel();
        SetupUI();
        AttachEventHandlers();
        LoadInitialSettings();
    }
    

    private void InitializeViewModel()
    {
        if (ViewModel is not { } vm)
        {
            return;
        }

        SelectInitialTab(vm);
        var settingsVm = vm.SettingsViewModel;
        Task.Run(() =>
        {
            settingsVm.InitializeNavigation(GoBack, GoForward);
            settingsVm.SubscriptionSettingsUpdate();
        });
        SubscribeToChanges(vm, settingsVm);
    }

    private void SelectInitialTab(MainViewModel vm)
    {
        MainTabControl.SelectedIndex = vm.Window.SettingsWindowConfig.WindowProperties.LastTab;
    }
    
    private void SetLastTab(MainViewModel vm)
    {
        vm.Window.SettingsWindowConfig.WindowProperties.LastTab = MainTabControl.SelectedIndex;
    }

    private static void SubscribeToChanges(MainViewModel vm, SettingsViewModel settingsVm)
    {
        _marginSubscription = new CompositeDisposable();
        Observable.EveryValueChanged(settingsVm.WindowMargin, x => x.CurrentValue, UIHelper.GetFrameProvider)
            .Skip(1)
            .Subscribe(x =>
            {
                Settings.WindowProperties.Margin = x;
                WindowResizing.SetSize(vm.PicViewer.PixelWidth.CurrentValue, vm.PicViewer.PixelHeight.CurrentValue, 0, 0, vm);
                WindowFunctions.CenterWindowOnScreen();
            }).AddTo(_marginSubscription);
    }

    private static void LoadInitialSettings()
    {
        Task.Run(() =>
        {
            if (string.IsNullOrWhiteSpace(CurrentSettingsPath))
            {
                _ = SaveSettingsAsync();
            }

            if (string.IsNullOrWhiteSpace(KeybindingFunctions.CurrentKeybindingsPath))
            {
                _ = KeybindingManager.UpdateKeyBindingsFile();
            }
        });
    }

    private void SetupUI()
    {
        Height = MainTabControl.MaxHeight = ScreenHelper.GetWindowMaxHeight() - SizeDefaults.TopBorderHeight;
        if (!Settings.Theme.Dark)
        {
            MainTabControl.Background = Brushes.Transparent;
        }
    }

    private void AttachEventHandlers()
    {
        CloseItem.Click += (_, _) => (VisualRoot as Window)?.Close();
        MainTabControl.SelectionChanged += OnTabSelectionChanged;
        PointerPressed += OnPointerPressed;
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        Disposable.Dispose(_marginSubscription, ViewModel.SettingsViewModel);
        ViewModel.SettingsViewModel = null;
    }

    #endregion

    #region Navigation

    private void HandleMouseButtonNavigation(PointerPressedEventArgs e)
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return;
        }

        var topLevel = TopLevel.GetTopLevel(desktop.MainWindow);
        var properties = e.GetCurrentPoint(topLevel).Properties;

        if (properties.IsXButton1Pressed)
        {
            GoBack();
        }

        if (properties.IsXButton2Pressed)
        {
            GoForward();
        }

        if (properties.IsRightButtonPressed)
        {
            ContextMenu.Open();
        }
    }

    private void OnTabSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (MainTabControl.SelectedItem is TabItem selectedTab && _currentTab != selectedTab)
        {
            HandleTabSelection(selectedTab);
        }
    }

    private void HandleTabSelection(TabItem selectedTab)
    {
        if (_currentTab != null)
        {
            _backStack.Push(_currentTab);
            _forwardStack.Clear(); // Clear forward history
        }

        _currentTab = selectedTab;
        SelectTab(_currentTab);
        UpdateNavigationButtons();
        SetLastTab(ViewModel);
    }

    public void GoBack()
    {
        if (!_backStack.TryPop(out var previousTab))
        {
            return;
        }

        _forwardStack.Push(_currentTab);
        _currentTab = previousTab;
        SelectTab(_currentTab);
        UpdateNavigationButtons();
    }

    public void GoForward()
    {
        if (!_forwardStack.TryPop(out var nextTab))
        {
            return;
        }

        _backStack.Push(_currentTab);
        _currentTab = nextTab;
        SelectTab(_currentTab);
        UpdateNavigationButtons();
    }

    private void SelectTab(TabItem? tab)
    {
        MainTabControl.SelectedItem = tab;
    }

    private void UpdateNavigationButtons()
    {
        if (ViewModel?.SettingsViewModel is not { } svm)
        {
            return;
        }

        svm.IsBackButtonEnabled.Value = _backStack.Count > 0;
        svm.IsForwardButtonEnabled.Value = _forwardStack.Count > 0;
    }

    #endregion
}