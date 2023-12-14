namespace PicView.Core.Config;

public interface ISettingsManager
{
    AppSettings? AppSettings { get; set; }

    void LoadSettings();

    void SaveSettings();
}