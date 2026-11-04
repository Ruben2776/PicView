using PicView.Core.MacOS.AppleScripts;

namespace PicView.Core.MacOS.FileFunctions;

public static class FileProperties
{
    public static async Task<bool> ShowFilePropertiesAsync(string filePath)
    {
        // Create the AppleScript to open the "Get Info" dialog
        var appleScript = $"""
                           tell application "Finder"
                               open information window of (POSIX file "{filePath}" as alias)
                               activate
                           end tell
                           """;
        
        // Execute the script using the existing AppleScriptManager
        return await AppleScriptManager.ExecuteAppleScriptAsync(appleScript).ConfigureAwait(false);
    }
}
