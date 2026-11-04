using PicView.Core.MacOS.AppleScripts;

namespace PicView.Core.MacOS.FileAssociation;

public class AppInfo
{
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string BundleId { get; set; } = string.Empty;
}

public static class GetAssociatedFiles
{
    private static readonly string[] ExcludedBundleIds = 
    {
        "com.ruben2776.picview",
        "PicView" // Fallback name check
    };

    public static async Task<AppInfo[]> GetAssociatedFilesAsync(string filePath)
    {
        var appleScript = $@"
use AppleScript version ""2.4""
use scripting additions
use framework ""AppKit""

set filePath to ""{filePath}""
set fileURL to current application's NSURL's fileURLWithPath:filePath

set workspace to current application's NSWorkspace's sharedWorkspace()
set appsArray to workspace's URLsForApplicationsToOpenURL:fileURL

set resultList to {{}}
repeat with appURL in appsArray
    set appPath to appURL's |path|() as text
    set appName to appURL's lastPathComponent() as text
    
    -- Get bundle identifier
    set bundleId to """"
    try
        set appBundle to current application's NSBundle's bundleWithURL:appURL
        if appBundle is not missing value then
            set bundleId to appBundle's bundleIdentifier() as text
        end if
    end try
    
    set end of resultList to appName & ""|"" & appPath & ""|"" & bundleId
end repeat
return resultList";

        var result = await AppleScriptManager.ExecuteAppleScriptWithResultAsync(appleScript).ConfigureAwait(false);
        if (!string.IsNullOrEmpty(result))
        {
            var apps = result.Split(',');
            var appInfos = new List<AppInfo>();
            
            foreach (var app in apps)
            {
                var parts = app.Trim().Split('|');
                if (parts.Length < 2)
                {
                    continue;
                }

                var appName = parts[0].Trim();
                var appPath = parts[1].Trim();
                var bundleId = parts.Length > 2 ? parts[2].Trim() : string.Empty;
                    
                // Skip if this is our own app
                if (IsOwnApp(appName, bundleId))
                    continue;
                    
                appInfos.Add(new AppInfo
                {
                    Name = appName,
                    Path = appPath,
                    BundleId = bundleId
                });
            }
            return appInfos.ToArray();
        }
        
#if DEBUG
        Console.WriteLine("No applications found for this file type: " + filePath);
#endif
        return Array.Empty<AppInfo>();
    }
    
    private static bool IsOwnApp(string appName, string bundleId)
    {
        // Check by bundle identifier first (most reliable)
        if (!string.IsNullOrEmpty(bundleId) && 
            ExcludedBundleIds.Any(excluded => 
                string.Equals(bundleId, excluded, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        // Fallback: Check by app name
        return appName.Contains("PicView", StringComparison.OrdinalIgnoreCase);
    }
}