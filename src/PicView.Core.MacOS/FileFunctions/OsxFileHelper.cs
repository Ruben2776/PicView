using PicView.Core.MacOS.AppleScripts;

namespace PicView.Core.MacOS.FileFunctions;

public static class OsxFileHelper
{
    public static async Task<bool> MoveFileToRecycleBinAsync(string filePath)
    {
        var appleScript = "tell application \"Finder\" to delete POSIX file \"" + filePath + "\"";
    
        return await AppleScriptManager.ExecuteAppleScriptAsync(appleScript).ConfigureAwait(false);
    }
}
