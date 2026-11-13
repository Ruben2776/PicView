namespace PicView.Core.Config.ConfigFileManagement;

public class ConfigFile(string configFileName)
{
    public string ConfigFileName { get; } = configFileName;

    public string RoamingConfigPath => Path.Combine(ConfigFolders.RoamingDirectory, ConfigFileName);

    public string LocalConfigPath => Path.Combine(ConfigFolders.LocalDirectory, ConfigFileName);

    /// <summary>
    /// Replaces the resolved path. Only set by test seams.
    /// </summary>
    internal string? PathOverride;

    /// <summary>
    /// The path of this config file in the resolved config folder.
    /// </summary>
    public string FilePath => PathOverride ?? Path.Combine(ConfigFolders.Current, ConfigFileName);
}
