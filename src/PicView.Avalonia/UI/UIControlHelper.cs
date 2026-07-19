using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Views.Main;
using PicView.Avalonia.Views.Menu;
using PicView.Avalonia.Views.UC;
using PicView.Core.ViewModels;
using DropDownMenu = PicView.Avalonia.Views.Menu.DropDownMenu;

namespace PicView.Avalonia.UI;

public class UIControlHelper
{
    public MainWindow? GetMainWindow { get; private set; }
    public MainView? GetMainView { get; private set; }
    public DraggableTabControl? GetMainTabControl { get; private set; }
    public Control? GetTitlebar { get; private set; }
    public EditableTitlebar? GetEditableTitlebar { get; private set; }
    public BottomBar? GetBottomBar { get; private set; }
    public DropDownMenu? GetDropDownMenu { get; private set; }
    public ToolTipMessage? GetToolTipMessage { get; private set; }
    public FileMenu? GetFileMenu { get; private set; }
    public SettingsMenu? GetSettingsMenu { get; private set; }

    public void Initialize(MainWindow mainWindow)
    {
        GetMainWindow = mainWindow;
        GetMainView = mainWindow.SharedMainView;
        GetTitlebar = mainWindow.SharedTitleBar;
        GetEditableTitlebar = mainWindow.SharedTitleBar.FindControl<EditableTitlebar>("EditableTitlebar");
        GetBottomBar = mainWindow.SharedBottomBar;
        GetToolTipMessage = GetMainView?.MainPanel.FindControl<ToolTipMessage>("ToolTipMessage");
        GetMainTabControl = GetMainView.MainTabControl;
    }
    
    public void AddDropDownMenu(MainWindow mainWindow)
    {
        var dropDownMenu = new DropDownMenu(mainWindow)
        {
            Name = "DropDownMenu",
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(3, 0, 3, 0),
            IsVisible = false,
            HorizontalAlignment = HorizontalAlignment.Right,
            ZIndex = 9
        };
        GetMainView.MainPanel.Children.Add(dropDownMenu);
        GetDropDownMenu = dropDownMenu;
    }

    public void AddFileMenu(MainWindowViewModel vm)
    {
        var fileMenu = new FileMenu
        {
            Name = "FileMenu",
            VerticalAlignment = VerticalAlignment.Bottom,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0,0,147,0),
            IsVisible = false,
            DataContext = vm,
            ZIndex = 9
        };
        GetMainView.MainPanel.Children.Add(fileMenu);
        GetFileMenu = fileMenu;
    }
    
    public void AddSettingsMenu(MainWindowViewModel vm)
    {
        var settingsMenu = new SettingsMenu
        {
            Name = "SettingsMenu",
            VerticalAlignment = VerticalAlignment.Bottom,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0,0,-102,0),
            IsVisible = false,
            DataContext = vm,
            ZIndex = 9
        };
        GetMainView.MainPanel.Children.Add(settingsMenu);
        GetSettingsMenu = settingsMenu;
    }
}