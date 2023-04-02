using PicView.ChangeImage;
using PicView.PicGallery;
using PicView.Properties;
using PicView.SystemIntegration;
using PicView.UILogic;
using System.Diagnostics;
using System.IO;
using System.Windows.Threading;
using static PicView.ChangeImage.Navigation;
using static PicView.FileHandling.FileLists;

namespace PicView.FileHandling
{
    internal static class ArchiveExtraction
    {
        /// <summary>
        /// File path for the extracted folder
        /// </summary>
        internal static string? TempFilePath { get; set; }

        /// <summary>
        /// File path for the extracted zip file
        /// </summary>
        internal static string? TempZipFile { get; set; }

        /// <summary>
        /// Attemps to extract folder
        /// </summary>
        /// <param name="path">The path to the archived file</param>
        /// <returns></returns>
        internal static bool Extract(string path)
        {
            string[] appNames = { "WinRAR.exe", "7z.exe" };
            string[] appPathNames = { "\\WinRAR\\WinRAR.exe", "\\7-Zip\\7z.exe" };

            string? extractAppPath = GetExtractAppPath(appPathNames, appNames);
            if (extractAppPath == null)
            {
                return false;
            }

            return Extract(path, extractAppPath, extractAppPath.Contains("WinRAR", StringComparison.OrdinalIgnoreCase));
        }

        private static string? GetExtractAppPath(string[] commonPaths, string[] appNames)
        {
            if (appNames == null || commonPaths == null)
            {
                return null;
            }

            string x86Path = GetProgramFilePath(Environment.SpecialFolder.ProgramFilesX86, commonPaths);
            if (!string.IsNullOrEmpty(x86Path))
            {
                return x86Path;
            }

            string x64Path = GetProgramFilePath(Environment.SpecialFolder.ProgramFiles, commonPaths);
            if (!string.IsNullOrEmpty(x64Path))
            {
                return x64Path;
            }

            foreach (string appName in appNames)
            {
                string? registryPath = FileFunctions.GetPathForExe(appName);
                if (registryPath == null)
                {
                    return null;
                }
                if (File.Exists(registryPath))
                {
                    return registryPath;
                }
            }

            return null;
        }

        private static string GetProgramFilePath(Environment.SpecialFolder specialFolder, string[] paths)
        {
            foreach (string path in paths)
            {
                string fullPath = Environment.GetFolderPath(specialFolder) + path;
                if (File.Exists(fullPath))
                {
                    return fullPath;
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// Attemps to extract folder
        /// </summary>
        /// <param name="path">The path to the archived file</param>
        /// <param name="exe">Full path of the executeable</param>
        /// <param name="winrar">If WinRar or 7-Zip</param>
        private static bool Extract(string path, string exe, bool isWinrar)
        {
            if (!CreateTempDirectory(path))
            {
                return false;
            }

#if DEBUG
            Trace.WriteLine("Created temp dir: " + TempFilePath);
#endif

            BackupPath = ErrorHandling.CheckOutOfRange() == false ? Pics[FolderIndex] : null;

            var arguments = isWinrar
                ? $"x -o- \"{path}\" " // WinRAR
                : $"x \"{path}\" -o"; // 7-Zip

            var supportedFilesFilter = " *" + string.Join(" *", SupportedFiles.FileExtensions) + " ";
            arguments += TempFilePath + supportedFilesFilter + " -r -aou";

            var process = Process.Start(new ProcessStartInfo
            {
                FileName = exe,
                Arguments = arguments,
                RedirectStandardOutput = true,
                CreateNoWindow = false
            });

            if (process == null) { return false; }

            var previewed = false;

            process.EnableRaisingEvents = true;
            process.BeginOutputReadLine();
            process.OutputDataReceived += (_, _) =>
            {
                // Fix it if files are in sub directory
                while (!process.HasExited)
                {
                    if (previewed) return;

                    if (SetDirectory() && Pics.Count >= 1) 
                    {
                        LoadPic.LoadingPreview(new FileInfo(Pics[0]));
                        previewed = true;
                    }
                }
            };

            process.Exited += async (_, _) =>
            {
                if (SetDirectory())
                {
                    await LoadPic.LoadPicAtIndexAsync(0).ConfigureAwait(false);

                    GetFileHistory ??= new FileHistory();
                    GetFileHistory.Add(TempZipFile);

                    if (Settings.Default.FullscreenGalleryHorizontal)
                    {
                        await GalleryLoad.LoadAsync().ConfigureAwait(false);
                    }
                }
                else
                {
                    await ErrorHandling.ReloadAsync(true).ConfigureAwait(false);
                }
            };

            return true;
        }

        internal static bool CreateTempDirectory(string path)
        {
            TempZipFile = path;
            TempFilePath = Path.GetTempPath() + Path.GetRandomFileName();
            Directory.CreateDirectory(TempFilePath);

            return Directory.Exists(TempFilePath);
        }

        private static bool SetDirectory()
        {
            if (string.IsNullOrEmpty(TempFilePath))
            {
#if DEBUG
                Trace.WriteLine("SetDirectory empty zip path");
#endif
                return false;
            }

            // Set extracted files to Pics
            if (!Directory.Exists(TempFilePath)) { return false; }

            var directory = Directory.GetDirectories(TempFilePath);
            if (directory.Length > 0)
            {
                TempFilePath = directory[0];
            }

            var extractedFiles = FileList(new FileInfo(TempFilePath));
            if (extractedFiles.Count > 0)
            {
                Pics = extractedFiles;
            }
            else
            {
                return false;
            }

            return true;
        }
    }
}