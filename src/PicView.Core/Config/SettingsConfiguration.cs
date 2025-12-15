using PicView.Core.Config.ConfigFileManagement;

namespace PicView.Core.Config;

public class SettingsConfiguration() : ConfigFile("UserSettings.json")
{
    public const double CurrentSettingsVersion = 1.7;

}

public class GlobalSettingsConfiguration() : ConfigFile("GlobalSettings.json")
{
    public const double CurrentSettingsVersion = 1.7;

}