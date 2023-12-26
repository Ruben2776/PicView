using Microsoft.VisualBasic.FileIO;
using System.Diagnostics;

namespace PicView.Core.FileHandling;

public static class FileDeletionHelper
{
    public static string DeleteFile(string file, bool recycle)
    {
        if (File.Exists(file) == false)
        {
            return string.Empty;
        }

        try
        {
            var toRecycleOption = recycle ? RecycleOption.SendToRecycleBin : RecycleOption.DeletePermanently;
            FileSystem.DeleteFile(file, UIOption.OnlyErrorDialogs, toRecycleOption);
        }
        catch (Exception e)
        {
#if DEBUG
            Trace.WriteLine("Delete exception \n" + e.Message);
#endif
            return e.Message;
        }

        return string.Empty;
    }
}