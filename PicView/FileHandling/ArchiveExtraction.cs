using PicView.ChangeImage;
using PicView.ImageHandling;
using PicView.SystemIntegration;
using PicView.UILogic;
using System;
using System.Diagnostics;
using System.IO;
using static PicView.ChangeImage.Navigation;
using static PicView.FileHandling.FileLists;

namespace PicView.FileHandling
{
    internal static class ArchiveExtraction
    {
        private const string SupportedFilesFilter =
            " *.jpg *.jpeg *.jpe *.png *.bmp *.tif *.tiff *.gif *.ico *.jfif *.webp *.wbmp "
            + "*.psd *.psb "
            + "*.tga *.dds "
            + "*.svg "
            + "*.3fr *.arw *.cr2 *.crw *.dcr *.dng *.erf *.kdc *.mdc *.mef *.mos *.mrw *.nef *.nrw *.orf "
            + "*.pef *.raf *.raw *.rw2 *.srf *.x3f "
            + "*.pgm *.hdr *.cut *.exr *.dib *.heic *.emf *.wmf *.wpg *.pcx *.xbm *.xpm";

        /// <summary>
        /// File path for the extracted folder
        /// </summary>
        internal static string TempFilePath { get; set; }

        /// <summary>
        /// File path for the extracted zip file
        /// </summary>
        internal static string TempZipFile { get; set; }

        /// <summary>
        /// Attemps to extract folder
        /// </summary>
        /// <param name="path">The path to the archived file</param>
        /// <returns></returns>
        internal static bool Extract(string path)
        {
            var Winrar = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) + "\\WinRAR\\WinRAR.exe";
            if (!File.Exists(Winrar))
            {
                Winrar = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86) + "\\WinRAR\\WinRAR.exe";
            }

            if (File.Exists(Winrar) || NativeMethods.IsSoftwareInstalled("WinRAR"))  // TODO test if works
            {
                return Extract(path, Winrar, true);
            }

            var sevenZip = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) + "\\7-Zip\\7z.exe";
            if (!File.Exists(sevenZip))
            {
                sevenZip = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86) + "\\7-Zip\\7z.exe";
            }

            if (File.Exists(sevenZip) || NativeMethods.IsSoftwareInstalled("7-Zip")) // TODO test if works
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
            if (CreateTempDirectory(path))
            {
#if DEBUG
                Trace.WriteLine("Created temp dir: " + TempFilePath);
#endif
            }
            else { return false; }

            var arguments = winrar ?
                // Add WinRAR specifics
                "x -o- \"" + path + "\" "
                :
                // Add 7-Zip specifics
                "x \"" + path + "\" -o";

            arguments += TempFilePath + SupportedFilesFilter + " -r -aou";

            var x = Process.Start(new ProcessStartInfo
            {
                FileName = exe,
                Arguments = arguments,
                RedirectStandardOutput = true,

#if DEBUG
                CreateNoWindow = false
#else
                CreateNoWindow = true
#endif
            });

            if (x == null) { return false; }

            x.EnableRaisingEvents = true;
            x.BeginOutputReadLine();
            x.OutputDataReceived += async delegate
            {
                while (Pics.Count < 2)
                {
                    SetDirectory();
                }
                if (Pics.Count >= 2 && !x.HasExited)
                {
                    await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Send, (Action)(() =>
                    {
                        ConfigureWindows.GetMainWindow.MainImage.Source = Thumbnails.GetBitmapSourceThumb(Pics[0]);
                    }));
                    await Preloader.PreLoad(0).ConfigureAwait(false);
                }
            };
            x.Exited += async delegate
            {
                if (SetDirectory())
                {
                    await LoadPicAtIndexAsync(0).ConfigureAwait(false);

                    // Add zipped files as recent file
                    RecentFiles.Add(TempZipFile);

                    if (Properties.Settings.Default.FullscreenGallery)
                    {
                        await PicGallery.GalleryLoad.Load().ConfigureAwait(false);
                        Timers.PicGalleryTimerHack();
                    }
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

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
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
            if (Directory.Exists(TempFilePath))
            {
                var directory = Directory.GetDirectories(TempFilePath);
                if (directory.Length > 0)
                {
                    TempFilePath = directory[0];
                }

                var extractedFiles = FileList(TempFilePath);
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
            return false;
        }
    }
}