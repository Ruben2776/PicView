using PicView.Core.Config.ConfigFileManagement;

namespace PicView.Tests.Config;

public class KnownFileNamesTests
{
    /// <summary>
    /// A config file missing from the list is left behind when the user switches config folders, so its contents seem
    /// lost.
    /// </summary>
    [Fact]
    public void KnownFileNames_EveryConfigFile_IsListed()
    {
        var configFileTypes = typeof(ConfigFile).Assembly.GetTypes()
            .Where(type => type.IsSubclassOf(typeof(ConfigFile)) && !type.IsAbstract && !type.ContainsGenericParameters)
            .ToList();
        Assert.NotEmpty(configFileTypes);

        foreach (var type in configFileTypes)
        {
            var file = (ConfigFile)Activator.CreateInstance(type, nonPublic: true)!;
            Assert.Contains(file.ConfigFileName, ConfigFolders.KnownFileNames);
        }
    }

    /// <summary>
    /// Renaming a config file loses what users have saved in it, so a rename must be a deliberate edit here.
    /// </summary>
    [Fact]
    public void KnownFileNames_MatchTodaysFileNames()
    {
        string[] expected =
        [
            "UserSettings.json", "keybindings.json", "FileHistory.json", "Favorites.json",
            "SettingsWindow.json", "ImageInfoWindow.json", "KeybindingWindowConfig.json", "EffectsWindow.json",
            "ResizeWindow.json", "BatchResizeWindow.json", "ConvertWindow.json", "PrintWindow.json"
        ];

        Assert.Equal(expected.Order(StringComparer.Ordinal), ConfigFolders.KnownFileNames.Order(StringComparer.Ordinal));
    }
}
