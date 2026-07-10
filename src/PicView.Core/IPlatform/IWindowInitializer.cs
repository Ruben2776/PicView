using PicView.Core.ViewModels;

namespace PicView.Core.IPlatform;

public interface IWindowInitializer
{
    void ShowAboutWindow();

    Task ShowImageInfoWindow(MainWindowViewModel vm);

    Task ShowKeybindingsWindow();

    ValueTask ShowSettingsWindow();
    
    Task ShowEffectsWindow();
    
    void ShowSingleImageResizeWindow();
    
    ValueTask ShowBatchResizeWindow();
    
    void ShowFileAssociationsWindow();

    void ShowConvertWindow();
    
    Task ShowPrintWindow(string path, MainWindowViewModel vm);
}