namespace PicView.Core.Config;

public interface ISettingsManager
{
    AppSettings? AppSettings { get; set; }

    Task LoadSettingsAsync();

    Task SaveSettingsAsync();
}