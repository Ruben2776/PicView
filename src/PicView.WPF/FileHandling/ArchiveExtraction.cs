using System.Diagnostics;
using System.IO;
using System.Windows;
using Microsoft.Win32;
using PicView.Core.Config;
using PicView.Core.FileHandling;
using PicView.WPF.ChangeImage;
using PicView.WPF.PicGallery;
using PicView.WPF.UILogic;
using PicView.WPF.UILogic.Sizing;
using static PicView.WPF.ChangeImage.Navigation;
using static PicView.WPF.FileHandling.FileLists;

namespace PicView.WPF.FileHandling;

internal static class ArchiveExtraction
{
    /// <summary>
    /// Attempts to extract folder
    /// </summary>
    /// <param name="pathToArchiveFile">The archivePath to the archived file</param>
    /// <returns></returns>
    internal static bool Extract(string pathToArchiveFile)
    {
        string[] appNames = { "WinRAR.exe", "7z.exe", "Nanazip.exe" };
        string[] appPathNames = { "\\WinRAR\\WinRAR.exe", "\\7-Zip\\7z.exe", "\\Nanazip\\Nanazip.exe" };

        var extractAppPath = GetExtractAppPath(appPathNames, appNames);
        return extractAppPath != null && Extract(pathToArchiveFile, extractAppPath,
            extractAppPath.Contains("WinRAR", StringComparison.OrdinalIgnoreCase),
            extractAppPath.Contains("7z", StringComparison.OrdinalIgnoreCase),
            extractAppPath.Contains("Nanazip", StringComparison.OrdinalIgnoreCase));
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
                switch (subKeyName)
                {
                    case "7-Zip":
                        {
                            var installDir = subKey.GetValue("InstallLocation").ToString();

                            return Path.Combine(installDir, "7z.exe");
                        }
                    case "WinRAR":
                        {
                            var installDir = subKey.GetValue("InstallLocation").ToString();

                            return Path.Combine(installDir, "WinRAR.exe");
                        }
                    case "NanaZip":
                        {
                            var installDir = subKey.GetValue("InstallLocation").ToString();

                            return Path.Combine(installDir, "NanaZip.exe");
                        }
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
    /// <param name="isWinrar">If WinRar</param>
    /// <param name="is7zip">If 7-Zip</param>
    /// <param name="isNanazip">If Nanazip</param>
    private static bool Extract(string archivePath, string exe, bool isWinrar, bool is7zip, bool isNanazip)
    {
        if (!ArchiveHelper.CreateTempDirectory(archivePath))
        {
            return false;
        }

#if DEBUG
        Trace.WriteLine("Created temp dir: " + ArchiveHelper.TempFilePath);
#endif

        BackupPath = ErrorHandling.CheckOutOfRange() == false ? Pics[FolderIndex] : null;

        try
        {
            string arguments;
            if (isWinrar)
            {
                arguments = $"x -o- \"{archivePath}\" "; // WinRAR
            }
            else if (is7zip)
            {
                arguments = $"x \"{archivePath}\" -o"; // 7-Zip
            }
            else if (isNanazip)
            {
                arguments = $"extract \"{archivePath}\" -o"; // Nanazip
            }
            else
            {
                // Handle unsupported archive format
                return false;
            }

            var supportedFilesFilter = " *" + string.Join(" *", SupportedFiles.FileExtensions) + " ";
            arguments += ArchiveHelper.TempFilePath + supportedFilesFilter + " -r -aou";

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

            process.EnableRaisingEvents = true;
            process.BeginOutputReadLine();
            process.OutputDataReceived += (_, _) =>
            {
                ProcessOutPut(process);
            };

            process.Exited += async (_, _) =>
            {
                await ProcessExited().ConfigureAwait(false);
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

    private static bool SetDirectory()
    {
        if (string.IsNullOrEmpty(ArchiveHelper.TempFilePath))
        {
#if DEBUG
            Trace.WriteLine("SetDirectory empty zip archivePath");
#endif
            return false;
        }

        // Set extracted files to Pics
        if (!Directory.Exists(ArchiveHelper.TempFilePath))
        {
            return false;
        }

        var directory = Directory.GetDirectories(ArchiveHelper.TempFilePath);
        if (directory.Length > 0)
        {
            ArchiveHelper.TempFilePath = directory[0];
        }

        var extractedFiles = FileList(new FileInfo(ArchiveHelper.TempFilePath));
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

    private static async Task ProcessExited()
    {
        if (SetDirectory())
        {
            await LoadPic.LoadPicAtIndexAsync(0).ConfigureAwait(false);

            if (SettingsHelper.Settings.Gallery.IsBottomGalleryShown)
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

            FileHistoryNavigation.Add(ArchiveHelper.TempZipFile);

            if (SettingsHelper.Settings.Gallery.IsBottomGalleryShown)
            {
                await GalleryLoad.ReloadGalleryAsync().ConfigureAwait(false);
            }
        }
        else
        {
            if (FileHelper.IsDirectoryEmpty(ArchiveHelper.TempFilePath))
            {
                try
                {
                    Directory.Delete(ArchiveHelper.TempFilePath);
                }
                catch (Exception e)
                {
#if DEBUG
                    Trace.WriteLine($"{nameof(ProcessExited)} exception, \n {e.Message}");
#endif
                }

                if (!string.IsNullOrWhiteSpace(BackupPath))
                {
                    await LoadPic.LoadPicFromStringAsync(BackupPath).ConfigureAwait(false);
                }
                else
                {
                    ErrorHandling.UnexpectedError();
                }
            }
        }
    }

    private static void ProcessOutPut(Process process)
    {
        var previewed = false;
        // Fix it if files are in sub directory
        while (!process.HasExited)
        {
            if (previewed) return;

            if (!SetDirectory() || Pics.Count < 1) continue;
            LoadPic.LoadingPreview(new FileInfo(Pics[0]));
            previewed = true;
        }
    }
}