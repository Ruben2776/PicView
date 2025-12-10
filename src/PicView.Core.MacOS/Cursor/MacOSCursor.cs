using PicView.Core.DebugTools;

namespace PicView.Core.MacOS.Cursor;

public class MacOSCursor
{
    public static int SetCursorPos(double x, double y)
    {
        var point = new NativeMethods.CGPoint(x, y);
        var result = NativeMethods.CGWarpMouseCursorPosition(point);
        
        if (result != 0)
        {
            DebugHelper.LogDebug(nameof(MacOSCursor), nameof(SetCursorPos), $"Failed to set cursor position: error code {result}");
        }
        return result;
    }
}