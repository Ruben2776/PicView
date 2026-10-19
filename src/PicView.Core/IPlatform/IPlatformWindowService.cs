namespace PicView.Core.IPlatform;

public interface IPlatformWindowService
{
    /// <summary>
    /// Gets or sets the width of the buttons on the respective title bar implementation.
    /// </summary>
    int CombinedTitleButtonsWidth { get; set; }

    /// <summary>
    /// Minimizes the window, which does not change its Window Mode.
    /// </summary>
    void Minimize();

    void ShowAboutWindow();

    void ShowImageInfoWindow();

    void ShowKeybindingsWindow();

    ValueTask ShowSettingsWindow();
    
    void ShowEffectsWindow();
    
    void ShowSingleImageResizeWindow();
    
    ValueTask ShowBatchResizeWindow();
    
    void ShowFileAssociationsWindow();

    void ShowConvertWindow();
    
    Task ShowPrintWindow(string path);
}
