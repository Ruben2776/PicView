using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using static PicView.DeleteFiles;
using static PicView.Fields;
using static PicView.FileLists;
using static PicView.Tooltip;

namespace PicView
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
            {
#if DEBUG
                Trace.WriteLine("Extract function delete temp files");
#endif
                DeleteTempFiles();
            }


            // TODO find a way to make user set path and app
            // if installed in irregular way

            var Winrar = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) + "\\WinRAR\\WinRAR.exe";
            if (!File.Exists(Winrar))
            {
                Winrar = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86) + "\\WinRAR\\WinRAR.exe";
            }

            if (File.Exists(Winrar))
            {
                return Extract(path, Winrar, true);
            }

            var sevenZip = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) + "\\7-Zip\\7z.exe";
            if (!File.Exists(sevenZip))
            {
                sevenZip = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86) + "\\7-Zip\\7z.exe";
            }

            if (File.Exists(sevenZip))
            {
                return Extract(path, sevenZip, false);
            }

            return false;
        }

        /// <summary>
        /// Attemps to extract folder
        /// </summary>
        /// <param name="path">The path to the archived file</param>
        /// <param name="exe">Full path of the executeable</param>
        /// <param name="winrar">If WinRar or 7-Zip</param>
        private static bool Extract(string path, string exe, bool winrar)
        {
#if DEBUG
            if (CreateTempDirectory(path))
            {
                Trace.WriteLine("Created temp dir: " + TempZipPath);
            }
            else
            {
                return false;
            }
#else
            if (!CreateTempDirectory(path))
                return false;
#endif

            var arguments = winrar ?
                // Add WinRAR specifics
                "x -o- \"" + path + "\" "
                :
                // Add 7-Zip specifics
                "x \"" + path + "\" -o";

            arguments += TempZipPath + SupportedFilesFilter + " -r -aou";

            var x = Process.Start(new ProcessStartInfo
            {
                FileName = exe,
                Arguments = arguments,

#if DEBUG
                CreateNoWindow = false
#else
                CreateNoWindow = true
#endif
            });
            x.EnableRaisingEvents = true;
            x.Exited += delegate
            {
                SetDirectory();
            };

            if (x == null)
            {
                return false;
            }


            //return SetDirectory(path);
            return true;
        }

        private static bool CreateTempDirectory(string path)
        {
            TempZipFile = path;
            TempZipPath = Path.GetTempPath() + Path.GetRandomFileName();
            Directory.CreateDirectory(TempZipPath);

            return Directory.Exists(TempZipPath);
        }

        private static bool SetDirectory()
        {
            if (string.IsNullOrEmpty(TempZipPath))
            {
#if DEBUG
                Trace.WriteLine("SetDirectory empty zip path");
#endif
                return false;
            }

            // Set extracted files to Pics
            if (Directory.Exists(TempZipPath))
            {
                var directory = Directory.GetDirectories(TempZipPath);
                if (directory.Length > 0)
                {
                    TempZipPath = directory[0];
                }

                var extractedFiles = FileList(TempZipPath);
                if (extractedFiles.Count > 0)
                {
                    Pics = extractedFiles;
                }
                else
                {
                    return false;
                }

                // Start at first file
                FolderIndex = 0;

                // Add zipped files as recent file
                RecentFiles.SetZipped(TempZipFile);

                return true;
            }
            return false;
        }

        /// <summary>
        /// Attemps to recover from failed archive extraction
        /// </summary>
        internal static async Task<bool> RecoverFailedArchiveAsync()
        {

#if DEBUG
            Trace.WriteLine("Entered RecoverFailedArchiveAsync");
#endif

            if (Pics.Count > 0)
            {
                return true;
            }

            mainWindow.Bar.Text = "Unzipping...";
            mainWindow.Bar.ToolTip = mainWindow.Bar.Text;
            await Task.Delay(100).ConfigureAwait(true);

            // TempZipPath is not null = images being extracted
            short count = 0;
            do
            {
                if (SetDirectory())
                {
                    return true;
                }

                if (count > 3)
                {
                    var processed = false;
                    var getProcesses = Process.GetProcessesByName("7z");
                    if (getProcesses.Length > 0)
                    {
                        processed = true;
                    }

                    if (!processed)
                    {
                        getProcesses = Process.GetProcessesByName("Zip");
                        if (getProcesses.Length > 0)
                        {
                            processed = true;
                        }
                    }

                    if (processed)
                    {
                        // Kill it if it's asking for password
                        if (!getProcesses[0].HasExited)
                        {
                            if (getProcesses[0].Threads[0].ThreadState == ThreadState.Wait)
                            {
#if DEBUG
                                Trace.WriteLine("Process killed");
#endif
                                ToolTipStyle("Password protected archive not supported");
                                //Reload(true);
                                getProcesses[0].Kill();
                                return false;
                            }
                        }
                    }
                    break;
                    //Reload(true);
                    //return false;
                }

                switch (count)
                {
                    case 0:
                        await Task.Delay(200).ConfigureAwait(true);
                        break;

                    case 1:
                        await Task.Delay(400).ConfigureAwait(true);
                        break;

                    case 2:
                        await Task.Delay(700).ConfigureAwait(true);
                        break;

                    default:
                        await Task.Delay(1500).ConfigureAwait(true);
                        break;
                }
                count++;
            } while (Pics.Count < 1);

#if DEBUG
            Trace.WriteLine("RecoverFailedArchiveAsync processed");
#endif

            if (SetDirectory())
            {
                return true;
            }

            return false;
        }
    }
}