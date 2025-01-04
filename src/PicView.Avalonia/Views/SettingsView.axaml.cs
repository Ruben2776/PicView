using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;

namespace PicView.Avalonia.Views;

public partial class SettingsView : UserControl
{
    private readonly Stack<TabItem?> _backStack = new();
    private readonly Stack<TabItem?> _forwardStack = new();
    private TabItem? _currentTab;
    
    public SettingsView()
    {
        InitializeComponent();
        // Add rounded corners on macOS
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            ResetSettingsButton.CornerRadius = new CornerRadius(0,0,0,8);
        }
        
        Loaded += delegate
        {
            MainTabControl.MinHeight = MainTabControl.Bounds.Height;
            MainTabControl.SelectionChanged += TabSelectionChanged;
            PointerPressed += OnMouseButtonDown;
        };
    }

    private void TabSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is not TabControl tabControl)
        {
            return;
        }

        if (tabControl.SelectedItem is not TabItem tabItem)
        {
            return;
        }
        
        if (_currentTab == tabItem)
        {
            return;
        }

        OnTabSelected(tabItem);
    }

    public void OnTabSelected(TabItem? selectedTab)
    {
        if (_currentTab != null)
        {
            _backStack.Push(_currentTab);
        }
    
        _currentTab = selectedTab;
        SelectTab(_currentTab);
    }
    
    public void GoBack()
    {
        if (_backStack.Count <= 0)
        {
            return;
        }

        _forwardStack.Push(_currentTab);
        _currentTab = _backStack.Pop();
        SelectTab(_currentTab);
    }

    public void GoForward()
    {
        if (_forwardStack.Count <= 0)
        {
            return;
        }

        _backStack.Push(_currentTab);
        _currentTab = _forwardStack.Pop();
        SelectTab(_currentTab);
    }

    private void SelectTab(TabItem? tab)
    {
        MainTabControl.SelectedItem = tab;
    }

    public void OnMouseButtonDown(object? sender, PointerPressedEventArgs e)
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return;
        }
        var topLevel = TopLevel.GetTopLevel(desktop.MainWindow);
        var prop = e.GetCurrentPoint(topLevel).Properties;
        if (prop.IsXButton1Pressed)  // Back button
        {
            GoBack();
        }
        else if (prop.IsXButton2Pressed)  // Forward button
        {
            GoForward();
        }
    }
}