namespace PicView.Avalonia.Services;

public interface IPlatformSpecificService
{
    void SetCursorPos(int x, int y);

    List<string> GetFiles(FileInfo fileInfo);

    int CompareStrings(string str1, string str2);

    void OpenWith(string path);

    void LocateOnDisk(string path);
    
    void ShowFileProperties(string path);

    void ShowAboutWindow();

    void ShowExifWindow();

    void ShowKeybindingsWindow();

    void ShowSettingsWindow();
    
    void ShowEffectsWindow();
    
    void ShowResizeWindow();
    
    void Print(string path);
}