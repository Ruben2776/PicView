using System.Diagnostics;
using System.Reflection;
using PicView.Core.ProcessHandling;

namespace PicView.Core.Config;

public static class VersionHelper
{
    public static string? GetCurrentVersion()
    {
        try
        {
            var loc = ProcessHelper.GetPathToProcess();
            var fvi = FileVersionInfo.GetVersionInfo(loc);
            var productVersion = fvi.ProductVersion;
            return productVersion[..productVersion.IndexOf('+')];
        }
        catch (Exception e)
        {
#if DEBUG
            Console.WriteLine(e);
#endif
            var assembly = Assembly.GetExecutingAssembly();
            var assemblyVersion = assembly.GetName().Version;
            return $"{assemblyVersion.Major}.{assemblyVersion.Minor}.{assemblyVersion.Build}.{assemblyVersion.Revision}";
        }
    }
}