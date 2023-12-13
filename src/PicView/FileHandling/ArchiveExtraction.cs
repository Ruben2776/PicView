using Microsoft.Win32;
using PicView.ChangeImage;
using PicView.PicGallery;
using PicView.Properties;
using PicView.UILogic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using PicView.Core.FileHandling;
using PicView.UILogic.Sizing;
using static PicView.ChangeImage.Navigation;
using static PicView.FileHandling.FileLists;

namespace PicView.FileHandling
{
    internal static class ArchiveExtraction
    {
        /// <summary>
        /// File archivePath for the extracted folder
        /// </summary>
        internal static string? TempFilePath { get; set; }

        /// <summary>
        /// File archivePath for the extracted zip file
        /// </summary>
        internal static string? TempZipFile { get; set; }

        /// <summary>
        /// Attempts to extract folder
        /// </summary>
        /// <param name="archivePath">The archivePath to the archived file</param>
        /// <returns></returns>
        internal static bool Extract(string archivePath)
        {
            string[] appNames = { "WinRAR.exe", "7z.exe" };
            string[] appPathNames = { "\\WinRAR\\WinRAR.exe", "\\7-Zip\\7z.exe" };

            var extractAppPath = GetExtractAppPath(appPathNames, appNames);
            return extractAppPath != null && Extract(archivePath, extractAppPath,
                extractAppPath.Contains("WinRAR", StringComparison.OrdinalIgnoreCase));
        }

        private static string? GetExtractAppPath(string[] commonPaths, string[] appNames)
        {
            if (appNames == null || commonPaths == null)
            {
                return null;
            }

            var x86Path = GetProgramFilePath(Environment.SpecialFolder.ProgramFilesX86, commonPaths);
            if (!string.IsNullOrEmpty(x86Path))
            {
                return x86Path;
            }

            var x64Path = GetProgramFilePath(Environment.SpecialFolder.ProgramFiles, commonPaths);
            if (!string.IsNullOrEmpty(x64Path))
            {
                return x64Path;
            }

            const string registryKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(registryKey);
                foreach (var subKeyName in key.GetSubKeyNames())
                {
                    using var subKey = key.OpenSubKey(subKeyName);
                    if (subKeyName.Equals("7-Zip") || subKeyName.Equals("WinRAR"))
                    {
                        var installDir = subKey.GetValue("InstallLocation").ToString();

                        return Path.Combine(installDir, "7z.exe");
                    }

                    if (subKeyName.Equals("WinRAR"))
                    {
                        var installDir = subKey.GetValue("InstallLocation").ToString();

                        return Path.Combine(installDir, "WinRAR.exe");
                    }
                }
            }
            catch (Exception e)
            {
#if DEBUG
                Trace.WriteLine($"{nameof(GetExtractAppPath)} exception, \n {e.Message}");
#endif
                return appNames.Select(FileFunctions.GetPathForExe).Where(registryPath => registryPath != null)
                    .FirstOrDefault(File.Exists);
            }

            return appNames.Select(FileFunctions.GetPathForExe).Where(registryPath => registryPath != null)
                .FirstOrDefault(File.Exists);
        }

        private static string GetProgramFilePath(Environment.SpecialFolder specialFolder, IEnumerable<string> paths)
        {
            foreach (var path in paths)
            {
                var fullPath = Environment.GetFolderPath(specialFolder) + path;
                if (File.Exists(fullPath))
                {
                    return fullPath;
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// Attempts to extract folder
        /// </summary>
        /// <param name="archivePath">The path to the archived file</param>
        /// <param name="exe">Full path of the executable</param>
        /// <param name="isWinrar">If WinRar or 7-Zip</param>
        private static bool Extract(string archivePath, string exe, bool isWinrar)
        {
            if (!CreateTempDirectory(archivePath))
            {
                return false;
            }

#if DEBUG
            Trace.WriteLine("Created temp dir: " + TempFilePath);
#endif

            BackupPath = ErrorHandling.CheckOutOfRange() == false ? Pics[FolderIndex] : null;

            try
            {
                var arguments = isWinrar
                    ? $"x -o- \"{archivePath}\" " // WinRAR
                    : $"x \"{archivePath}\" -o"; // 7-Zip

                var supportedFilesFilter = " *" + string.Join(" *", SupportedFiles.FileExtensions) + " ";
                arguments += TempFilePath + supportedFilesFilter + " -r -aou";

                var process = Process.Start(new ProcessStartInfo
                {
                    FileName = exe,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    CreateNoWindow = false,
                    WorkingDirectory = Path.GetDirectoryName(exe)
                });

                if (process == null)
                {
                    return false;
                }

                var previewed = false;

                process.EnableRaisingEvents = true;
                process.BeginOutputReadLine();
                process.OutputDataReceived += (_, _) =>
                {
                    // Fix it if files are in sub directory
                    while (!process.HasExited)
                    {
                        if (previewed) return;

                        if (!SetDirectory() || Pics.Count < 1) continue;
                        LoadPic.LoadingPreview(new FileInfo(Pics[0]));
                        previewed = true;
                    }
                };

                process.Exited += async (_, _) =>
                {
                    if (SetDirectory())
                    {
                        await LoadPic.LoadPicAtIndexAsync(0).ConfigureAwait(false);

                        if (Settings.Default.IsBottomGalleryShown)
                        {
                            if (UC.GetPicGallery is null)
                            {
                                await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(() =>
                                {
                                    GalleryToggle.ShowBottomGallery();
                                    ScaleImage.FitImage(ConfigureWindows.GetMainWindow.MainImage.Source.Width, ConfigureWindows.GetMainWindow.MainImage.Source.Height);
                                });

                                await GalleryLoad.LoadAsync().ConfigureAwait(false);
                            }
                            else
                            {
                                await UC.GetPicGallery?.Dispatcher.InvokeAsync(() =>
                                {
                                    UC.GetPicGallery.Visibility = Visibility.Visible;
                                    ScaleImage.FitImage(ConfigureWindows.GetMainWindow.MainImage.Source.Width, ConfigureWindows.GetMainWindow.MainImage.Source.Height);
                                });
                            }
                        }

                        GetFileHistory ??= new FileHistory();
                        GetFileHistory.Add(TempZipFile);

                        if (Settings.Default.IsBottomGalleryShown)
                        {
                            await GalleryLoad.ReloadGalleryAsync().ConfigureAwait(false);
                        }
                    }
                    else
                    {
                        await ErrorHandling.ReloadAsync(true).ConfigureAwait(false);
                    }
                };
            }
            catch (Exception e)
            {
#if DEBUG
                Trace.WriteLine($"{nameof(GetExtractAppPath)} exception, \n {e.Message}");
#endif
                Tooltip.ShowTooltipMessage(e.Message, true, TimeSpan.FromSeconds(5));
                return false;
            }

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
                Trace.WriteLine("SetDirectory empty zip archivePath");
#endif
                return false;
            }

            // Set extracted files to Pics
            if (!Directory.Exists(TempFilePath))
            {
                return false;
            }

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