namespace PicView.Avalonia.Services;

public interface IPlatformSpecificService
{
    void SetCursorPos(int x, int y);

    List<string> GetFiles(FileInfo fileInfo);

    int CompareStrings(string str1, string str2);

    void ShowAboutWindow();

    void ShowExifWindow();
}