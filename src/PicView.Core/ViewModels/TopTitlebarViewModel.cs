using R3;

namespace PicView.Core.ViewModels;

public class TopTitlebarViewModel
{
    public DropDownMenuViewModel DropDownMenu { get; } = new();
    
    public BindableReactiveProperty<bool> IsMainMenuVisible { get; } = new();
    public BindableReactiveProperty<bool> IsEditableTitlebarVisible { get; } = new(true);
    public BindableReactiveProperty<bool> IsGalleryButtonVisible { get; } = new(true);
    public BindableReactiveProperty<bool> IsMenuButtonVisible { get; } = new(true);
    public BindableReactiveProperty<bool> IsBtnPanelVisible { get; } = new(true);
    public BindableReactiveProperty<double> MaxItemWidth { get; } = new(double.NaN);

    public ReactiveCommand? OpenMenuCommand { get; private set; }
    
    public ReactiveCommand? ToggleDropDownMenuCommand { get; private set; }

    public TopTitlebarViewModel()
    {
        OpenMenuCommand = new ReactiveCommand(OpenMenu);
        ToggleDropDownMenuCommand = new ReactiveCommand(ToggleDropDownMenu);
    }

    public void ToggleDropDownMenu(Unit unit)
    {
        if (DropDownMenu is null) 
        {
            return;
        }

        if (DropDownMenu.IsDropDownMenuVisible.CurrentValue)
        {
            CloseDropDownMenu();
            return;
        }
        DropDownMenu.IsDropDownMenuVisible.Value = true;
        DropDownMenu.IsFileHistoryVisible.Value = Settings.Navigation.IsFileHistoryEnabled && !DropDownMenu.IsExpandedOptionsOpened.CurrentValue;
    }
    public void CloseDropDownMenu()
    {
        DropDownMenu?.IsDropDownMenuVisible.Value = false;
    }

    public void ToggleMenu()
    {
        if (IsMainMenuVisible.CurrentValue)
        {
            CloseMenu();
        }
        else
        {
            OpenMenu();
        }
    }

    public void OpenMenu(Unit unit)
        => OpenMenu();

    public void OpenMenu()
    {
        IsMainMenuVisible.Value = true;
        IsEditableTitlebarVisible.Value = false;
        IsGalleryButtonVisible.Value = false;
        IsMenuButtonVisible.Value = false;
    }

    public void CloseMenu()
    {
        IsMainMenuVisible.Value = false;
        IsEditableTitlebarVisible.Value = true;
        IsGalleryButtonVisible.Value = true;
        IsMenuButtonVisible.Value = true;
    }
}