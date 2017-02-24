using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using static PicView.lib.FileFunctions;
using static PicView.lib.Variables;
using PicView;

namespace PicView.lib
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
                WindowStyle = ProcessWindowStyle.Hidden
            });

            if (x == null) return;
            x.EnableRaisingEvents = true;
            x.Exited += (s, e) => Pics = FileList(TempZipPath);
            x.WaitForExit(200);
        }
    }
}
