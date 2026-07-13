using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Views.Main;
using PicView.Avalonia.Views.UC;
using PicView.Core.ViewModels;

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
}