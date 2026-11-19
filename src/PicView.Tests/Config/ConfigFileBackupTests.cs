using PicView.Core.Config.ConfigFileManagement;

namespace PicView.Tests.Config;

[Collection("Sequential")]
public class ConfigFileBackupTests
{
    [Fact]
    public async Task Dispose_WhileAConfigSaveIsInFlight_WaitsForItThenRemovesTheFileTheTestMade()
    {
        var file = new ConfigFile("ConfigFileBackupTests_" + Guid.NewGuid().ToString("N") + ".json");
        var backup = new ConfigFileBackup(file);
        var path = file.RoamingConfigPath;
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        Task dispose;
        // A save in flight holds the gate and has the file open, as a window's save does when it closes
        await ConfigFolders.Gate.WaitAsync(TestContext.Current.CancellationToken);
        try
        {
            await using (new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                dispose = Task.Run(backup.Dispose, TestContext.Current.CancellationToken);
                await Task.WhenAny(dispose, Task.Delay(TimeSpan.FromMilliseconds(300),
                    TestContext.Current.CancellationToken));

                Assert.False(dispose.IsCompleted, "The backup restored while a save still held the file");
            }
        }
        finally
        {
            ConfigFolders.Gate.Release();
        }

        await dispose.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

        Assert.False(File.Exists(path));
    }
}
