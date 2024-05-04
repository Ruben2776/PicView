using Microsoft.VisualBasic.FileIO;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace PicView.Core.FileHandling;

public static class FileDeletionHelper
{
    public static string DeleteFileWithErrorMsg(string file, bool recycle)
    {
        if (File.Exists(file) == false)
        {
            return string.Empty;
        }

        try
        {
            var toRecycleOption = recycle ? RecycleOption.SendToRecycleBin : RecycleOption.DeletePermanently;
            
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                FileSystem.DeleteFile(file, UIOption.OnlyErrorDialogs, toRecycleOption);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                File.Delete(file);
            }
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

    /// <summary>
    /// Deletes the temporary files when an archived file has been opened
    /// </summary>
    public static void DeleteTempFiles()
    {
        if (!Directory.Exists(ArchiveHelper.TempFilePath))
        {
            return;
        }

        try
        {
            Array.ForEach(Directory.GetFiles(ArchiveHelper.TempFilePath), File.Delete);
#if DEBUG
            Trace.WriteLine("Temp zip files deleted");
#endif

            Directory.Delete(ArchiveHelper.TempFilePath);
#if DEBUG
            Trace.WriteLine("Temp zip folder " + ArchiveHelper.TempFilePath + " deleted");
#endif
        }
        catch (Exception exception)
        {
#if DEBUG
            Trace.WriteLine($"{nameof(DeleteTempFiles)} caught exception:\n{exception.Message}");
#endif
            return;
        }

        ArchiveHelper.TempZipFile = ArchiveHelper.TempFilePath = null;
    }
}