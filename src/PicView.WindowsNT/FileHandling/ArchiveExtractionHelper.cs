using System.Diagnostics;
using Microsoft.Win32;
using PicView.Core.FileHandling;

namespace PicView.WindowsNT.FileHandling;

public static class ArchiveExtractionHelper
{
    private const string Winrar = "WinRAR.exe";
    private const string SevenZip = "7z.exe";
    private const string Nanazip = "NanaZipG.exe";
    
    public static async Task<bool> ExtractWithLocalSoftwareAsync(string pathToArchiveFile, string tempDirectory)
    {
        var appNames = new[] { Winrar, SevenZip, Nanazip };
        var appPathNames = new[] { $@"\WinRAR\{Winrar}", $@"\7-Zip\{SevenZip}", $@"\Nanazip\{Nanazip}" };
        
        var appPath = GetExtractAppPath(appPathNames, appNames);
        if (appPath == null)
        {
            return false;
        }
        
        var is7Zip = appPath.Contains("7z", StringComparison.OrdinalIgnoreCase);
        var isNanazip = appPath.Contains("Nanazip", StringComparison.OrdinalIgnoreCase);
        var isWinrar = appPath.Contains("WinRAR", StringComparison.OrdinalIgnoreCase);
        if (!is7Zip && !isNanazip && !isWinrar)
        {
            return false;
        }
        
        string arguments;
        if (is7Zip || isNanazip)
        {
            arguments = $"x \"{pathToArchiveFile}\" -o";
        }
        else if (isWinrar)
        {
            arguments = $"x -o- \"{pathToArchiveFile}\" ";
        }
        else
        {
            return false;
        }

        var supportedFilesFilter = " *" + string.Join(" *", SupportedFiles.FileExtensions) + " ";
        arguments += tempDirectory + supportedFilesFilter + " -r -aou";
        
        var process = Process.Start(new ProcessStartInfo
        {
            FileName = appPath,
            Arguments = arguments,
            RedirectStandardOutput = true,
            CreateNoWindow = false,
            WorkingDirectory = Path.GetDirectoryName(appPath)
        });

        if (process == null)
        {
            return false;
        }

        await process.WaitForExitAsync();
        
        return process.ExitCode == 0;
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
            if (key == null) return null;

            foreach (var subKeyName in key.GetSubKeyNames())
            {
                using var subKey = key.OpenSubKey(subKeyName);
                if (subKey == null) continue;

                var installDir = subKey.GetValue("InstallLocation")?.ToString();
                if (installDir == null) continue;

                switch (subKeyName)
                {
                    case "7-Zip":
                        return Path.Combine(installDir, SevenZip);
                    case "WinRAR":
                        return Path.Combine(installDir, Winrar);
                    case "NanaZip":
                        return Path.Combine(installDir, Nanazip);
                }
            }
        }
        catch (Exception e)
        {
#if DEBUG
            Trace.WriteLine($"{nameof(GetExtractAppPath)} exception, \n {e.Message}");
#endif
            return appNames.Select(GetPathForExe).Where(registryPath => registryPath != null)
                .FirstOrDefault(File.Exists);
        }

        return appNames.Select(GetPathForExe).Where(registryPath => registryPath != null)
            .FirstOrDefault(File.Exists);

        string GetProgramFilePath(Environment.SpecialFolder specialFolder, IEnumerable<string> paths)
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
    }
        
    private static string? GetPathForExe(string fileName)
    {
        var localMachine = Registry.LocalMachine;
        var fileKey = localMachine.OpenSubKey(
            $@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\{fileName}");
        var result = fileKey?.GetValue(string.Empty);
        if (result == null)
        {
            return null;
        }

        fileKey.Close();

        return (string)result;
    }
}
