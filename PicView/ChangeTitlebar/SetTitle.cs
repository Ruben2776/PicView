using PicView.ChangeImage;
using PicView.FileHandling;
using PicView.UILogic;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using static PicView.ChangeImage.Navigation;
using static PicView.FileHandling.FileFunctions;
using static PicView.UILogic.TransformImage.ZoomLogic;

namespace PicView.ChangeTitlebar
{
    internal static class SetTitle
    {
        internal const string AppName = "PicView";

        /// <summary>
        /// Returns string with file name, folder position,
        /// zoom, aspect ratio, resolution and file size
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="index"></param>
        /// <param name="fileInfo"></param>
        /// <returns></returns>
        private static string[]? TitleString(int width, int height, int index, FileInfo? fileInfo)
        {
            // Check if file info is present or not
            if (fileInfo == null)
            {
                try
                {
                    fileInfo = new FileInfo(Pics[index]);
                }
                catch (Exception e)
                {
                    return ReturnError("FileInfo exception " + e.Message);
                }
            }

            // Check if file exists or not
            if (!fileInfo.Exists)
            {
                _ = ErrorHandling.ReloadAsync();
                return null;
            }

            // Check index validity
            if (Pics?.Count < index || index >= Pics.Count)
            {
                return ReturnError("index invalid");
            }

            var files = (string)(Pics.Count == 1 ?
                Application.Current.Resources["File"] :
                Application.Current.Resources["Files"]);

            var s1 = new StringBuilder(90);
            s1.Append(fileInfo.Name)
                .Append(' ')
                .Append(index + 1)
                .Append('/')
                .Append(Pics.Count)
                .Append(' ')
                .Append(files)
                .Append(" (")
                .Append(width)
                .Append(" x ")
                .Append(height)
                .Append(StringAspect(width, height))
                .Append(FileFunctions.GetSizeReadable(fileInfo.Length));

            // Check if ZoomPercentage is not empty
            if (!string.IsNullOrEmpty(ZoomPercentage))
            {
                s1.Append(", ")
                    .Append(ZoomPercentage);
            }

            s1.Append(" - ")
                .Append(AppName);

            var array = new string[3];
            array[0] = s1.ToString();
            s1.Remove(s1.Length - (AppName.Length + 3), AppName.Length + 3);   // Remove AppName + " - "
            array[1] = s1.ToString();
            s1.Replace(Path.GetFileName(Pics[index]), Pics[index]);
            array[2] = s1.ToString();
            return array;
        }

        private static string[]? ReturnError(string exception)
        {
#if DEBUG
            Trace.WriteLine(exception);
#endif

            return new[]
            {
                (string)Application.Current.Resources["UnexpectedError"],
                (string)Application.Current.Resources["UnexpectedError"],
                (string)Application.Current.Resources["UnexpectedError"]
            };
        }


        /// <summary>
        /// Sets title string with file name, folder position,
        /// zoom, aspect ratio, resolution and file size
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        internal static void SetTitleString(int width, int height, int index, FileInfo? fileInfo)
        {
            var titleString = TitleString(width, height, index, fileInfo);
            if (titleString == null)
            {
                _ = ErrorHandling.ReloadAsync();
                return;
            }

            ConfigureWindows.GetMainWindow.Title = titleString[0];
            ConfigureWindows.GetMainWindow.TitleText.Text = titleString[1];
            ConfigureWindows.GetMainWindow.TitleText.ToolTip = titleString[2];
        }

        /// <summary>
        /// Returns string with file name,
        /// zoom, aspect ratio and resolution
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        private static string[] TitleString(int width, int height, string path)
        {
            var s1 = new StringBuilder();
            s1.Append(path).Append(" (").Append(width).Append(" x ").Append(height).Append(StringAspect(width, height));

            if (!string.IsNullOrEmpty(ZoomPercentage))
            {
                s1.Append(", ").Append(ZoomPercentage);
            }

            s1.Append(" - ").Append(AppName);

            var array = new string[2];
            array[0] = s1.ToString();
            s1.Remove(s1.Length - (AppName.Length + 3), AppName.Length + 3);   // Remove AppName + " - "
            array[1] = s1.ToString();
            return array;
        }

        /// <summary>
        /// Sets title string with file name,
        /// zoom, aspect ratio and resolution
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        internal static void SetTitleString(int width, int height, string path)
        {
            var titleString = TitleString(width, height, path);
            ConfigureWindows.GetMainWindow.Title = titleString[0];
            ConfigureWindows.GetMainWindow.TitleText.Text = titleString[1];
            ConfigureWindows.GetMainWindow.TitleText.ToolTip = titleString[1];
        }

        /// <summary>
        /// Use name from title bar to set title string with,
        /// zoom, aspect ratio and resolution
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        internal static void SetTitleString(int width, int height)
        {
            var path = FileFunctions.GetURL(ConfigureWindows.GetMainWindow.TitleText.Text);

            path = string.IsNullOrWhiteSpace(path) ? Application.Current.Resources["Image"] as string : path;

            var titleString = TitleString(width, height, path);
            ConfigureWindows.GetMainWindow.Title = titleString[0];
            ConfigureWindows.GetMainWindow.TitleText.Text = titleString[1];
            ConfigureWindows.GetMainWindow.TitleText.ToolTip = titleString[1];
        }

        internal static void SetLoadingString()
        {
            var s = Application.Current.Resources["Loading"] as string;
            if (s == null || ConfigureWindows.GetMainWindow.Title == null || ConfigureWindows.GetMainWindow.TitleText == null)
            {
                return;
            }
            ConfigureWindows.GetMainWindow.Title = s;
            ConfigureWindows.GetMainWindow.TitleText.Text = s;
            ConfigureWindows.GetMainWindow.TitleText.ToolTip = s;
        }
    }
}