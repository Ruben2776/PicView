using System.Runtime.CompilerServices;
using PicView.Core.Config.ConfigFileManagement;

namespace PicView.Tests;

/// <summary>
/// Points the default config folders at a temp folder before any test runs, so no test reads or writes the real
/// config folders next to the executable or in the user's roaming application data.
/// </summary>
internal static class TestConfigRoots
{
    public static string Root { get; private set; } = string.Empty;

#pragma warning disable CA2255 // The test assembly must redirect the config folders before any test touches them
    [ModuleInitializer]
#pragma warning restore CA2255
    internal static void Initialize()
    {
        Root = Path.Combine(Path.GetTempPath(), "PicView_TestConfig_" + Guid.NewGuid().ToString("N"));
        var local = Path.Combine(Root, "local");
        var roaming = Path.Combine(Root, "roaming");
        Directory.CreateDirectory(local);
        Directory.CreateDirectory(roaming);
        ConfigFolders.UseDefaultRoots(local, roaming);
        AppDomain.CurrentDomain.ProcessExit += (_, _) =>
        {
            try
            {
                Directory.Delete(Root, true);
            }
            catch
            {
                // Best effort cleanup
            }
        };
    }
}
