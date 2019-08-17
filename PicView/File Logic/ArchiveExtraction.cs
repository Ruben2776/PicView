using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using static PicView.Error_Handling.Error_Handling;
using static PicView.File_Logic.DeleteFiles;
using static PicView.Helpers.Variables;
using static PicView.Interface_Logic.Interface;

namespace PicView.File_Logic
{
    internal static class ArchiveExtraction
    {
        /// <summary>
        /// Attemps to extract folder
        /// </summary>
        /// <param name="path">The path to the archived file</param>
        /// <returns></returns>
        internal static bool Extract(string path)
        {
            if (!string.IsNullOrWhiteSpace(TempZipPath))
                DeleteTempFiles();

            var Winrar = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) + "\\WinRAR\\WinRAR.exe";
            if (!File.Exists(Winrar))
                Winrar = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86) + "\\WinRAR\\WinRAR.exe";
            if (File.Exists(Winrar))
            {
                Extract(path, Winrar, true);
                return true;
            }

            var sevenZip = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) + "\\7-Zip\\7z.exe";
            if (!File.Exists(sevenZip))
                sevenZip = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86) + "\\7-Zip\\7z.exe";
            if (File.Exists(sevenZip))
            {
                Extract(path, sevenZip, false);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Attemps to extract folder
        /// </summary>
        /// <param name="path">The path to the archived file</param>
        /// <param name="exe">Full path of the executeable</param>
        /// <param name="winrar">If WinRar or 7-Zip</param>
        private static void Extract(string path, string exe, bool winrar)
        {
            TempZipPath = Path.GetTempPath() + Path.GetRandomFileName();
            Directory.CreateDirectory(TempZipPath);

            var arguments = winrar ?
                // Add WinRAR specifics
                "x -o- \"" + path + "\" "
                :
                // Add 7-Zip specifics
                "x \"" + path + "\" -o";

            arguments += TempZipPath + SupportedFiles + " -r -aou";

            var x = Process.Start(new ProcessStartInfo
            {
                FileName = exe,
                Arguments = arguments,

                #if DEBUG
                WindowStyle = ProcessWindowStyle.Normal
                #else
                WindowStyle = ProcessWindowStyle.Hidden
                #endif
            });

            if (x == null) return;
            x.WaitForExit(750);
        }

        /// <summary>
        /// Attemps to recover from failed archive extraction
        /// </summary>
        internal static async Task<bool> RecoverFailedArchiveAsync()
        {
            if (Pics.Count > 0)
                return true;

            if (string.IsNullOrWhiteSpace(TempZipPath))
            {
                // Unexped result
                Reload(true);
                return false;
            }

            // TempZipPath is not null = images being extracted
            short count = 0;
            mainWindow.Bar.Text = "Unzipping...";
            do
            {
                var processed = false;
                var getProcesses = Process.GetProcessesByName("7z");
                if (getProcesses.Length > 0)
                    processed = true;

                getProcesses = Process.GetProcessesByName("Zip");
                if (getProcesses.Length > 0)
                    processed = true;

                if (!processed)
                {
                    Reload(true);
                    return false;
                }

                // Kill it if it's asking for password
                if (!getProcesses[0].HasExited)
                    if (getProcesses[0].Threads[0].ThreadState == System.Diagnostics.ThreadState.Wait)
                    {
                        ToolTipStyle("Password protected archive not supported");
                        Reload(true);
                        getProcesses[0].Kill();
                        return false;
                    }

                if (count > 3)
                {
                    Reload(true);
                    return false;
                }
                switch (count)
                {
                    case 0:
                        await Task.Delay(400);
                        break;

                    case 1:
                        await Task.Delay(600);
                        break;

                    case 2:
                        await Task.Delay(750);
                        break;

                    default:
                        await Task.Delay(2500);
                        break;
                }
                count++;
            } while (Pics.Count < 1);

            return Directory.Exists(TempZipPath);
        }
    }
}
