using PicView.Core.Config.ConfigFileManagement;

namespace PicView.Tests;

/// <summary>
/// Keeps one of the user's own config files as it was around a test that saves it: backs up each copy of the file
/// PicView could save to when the test starts, and restores it when the test ends. A file the test created where
/// there was none is deleted again.
/// </summary>
internal class ConfigFileBackup : IDisposable
{
    private const string BackupExtension = ".testbackup";

    private readonly (string Path, bool Existed)[] _files;

    public ConfigFileBackup(ConfigFile file)
    {
        _files =
        [
            .. new[] { file.RoamingConfigPath, file.LocalConfigPath }
                .Distinct(StringComparer.Ordinal)
                .Select(path => (path, BackUp(path)))
        ];
    }

    /// <remarks>
    /// Waits for <see cref="ConfigFolders.Gate" /> first: a window saves its config as it closes without the close
    /// waiting for it, so a save may still hold the file, or land after the restore, when a test cleans up.
    /// </remarks>
    public void Dispose()
    {
        ConfigFolders.Gate.Wait();
        try
        {
            foreach (var (path, existed) in _files)
            {
                var backup = path + BackupExtension;
                if (existed)
                {
                    File.Copy(backup, path, overwrite: true);
                    File.Delete(backup);
                }
                else if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }
        finally
        {
            ConfigFolders.Gate.Release();
        }
    }

    private static bool BackUp(string path)
    {
        var backup = path + BackupExtension;
        // A backup left behind by a run that was stopped holds the user's file, so it is restored first
        if (File.Exists(backup))
        {
            File.Copy(backup, path, overwrite: true);
        }

        if (!File.Exists(path))
        {
            return false;
        }

        File.Copy(path, backup, overwrite: true);
        return true;
    }
}
