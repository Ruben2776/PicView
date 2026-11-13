using PicView.Core.Config.ConfigFileManagement;

namespace PicView.Tests.Config;

[Collection("Sequential")]
public class TestConfigRootsTests
{
    [Fact]
    public void TestConfigRoots_DefaultRoots_AreNotTheRealFolders()
    {
        ConfigFolders.ResetRoots();

        Assert.False(ConfigFolders.RoamingDirectory.StartsWith(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), StringComparison.OrdinalIgnoreCase));
        Assert.False(ConfigFolders.LocalDirectory.StartsWith(
            AppDomain.CurrentDomain.BaseDirectory, StringComparison.OrdinalIgnoreCase));
    }
}
