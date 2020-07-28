using PicView.UILogic.Loading;
using System.IO;
using System.Text;
using System.Windows;
using static PicView.ChangeImage.Navigation;
using static PicView.FileHandling.FileFunctions;
using static PicView.UILogic.TransformImage.ZoomLogic;

namespace PicView.UILogic
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
        /// <returns></returns>
        private static string[] TitleString(int width, int height, int index)
        {
            FileInfo fileInfo;
            try
            {
                fileInfo = new FileInfo(Pics[index]);
            }
            catch (System.Exception)
            {
                return null;
            }

            var files = Pics.Count == 1 ?
                Application.Current.Resources["File"] : Application.Current.Resources["Files"];

            var s1 = new StringBuilder(90);
            s1.Append(fileInfo.Name).Append(' ').Append(index + 1).Append('/').Append(Pics.Count).Append(' ')
                .Append(files).Append(" (").Append(width).Append(" x ").Append(height)
                .Append(StringAspect(width, height))
                .Append(GetSizeReadable(fileInfo.Length));

            if (!string.IsNullOrEmpty(ZoomPercentage))
            {
                s1.Append(", ").Append(ZoomPercentage);
            }

            s1.Append(" - ").Append(AppName);

            var array = new string[3];
            array[0] = s1.ToString();
            s1.Remove(s1.Length - (AppName.Length + 3), AppName.Length + 3);   // Remove AppName + " - "
            array[1] = s1.ToString();
            s1.Replace(Path.GetFileName(Pics[index]), Pics[index]);
            array[2] = s1.ToString();
            return array;
        }

        /// <summary>
        /// Sets title string with file name, folder position,
        /// zoom, aspect ratio, resolution and file size
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        internal static void SetTitleString(int width, int height, int index)
        {
            var titleString = TitleString(width, height, index);
            if (titleString == null)
            {
                return;
            }

            LoadWindows.GetMainWindow.Title = titleString[0];
            LoadWindows.GetMainWindow.TitleText.Text = titleString[1];
            LoadWindows.GetMainWindow.TitleText.ToolTip = titleString[2];
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
            LoadWindows.GetMainWindow.Title = titleString[0];
            LoadWindows.GetMainWindow.TitleText.Text = titleString[1];
            LoadWindows.GetMainWindow.TitleText.ToolTip = titleString[1];
        }

        /// <summary>
        /// Use name from title bar to set title string with,
        /// zoom, aspect ratio and resolution
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        internal static void SetTitleString(int width, int height)
        {
            string path = Library.Utilities.GetURL(LoadWindows.GetMainWindow.TitleText.Text);

            path = string.IsNullOrWhiteSpace(path) ? Application.Current.Resources["Image"] as string : path;

            var titleString = TitleString(width, height, path);
            LoadWindows.GetMainWindow.Title = titleString[0];
            LoadWindows.GetMainWindow.TitleText.Text = titleString[1];
            LoadWindows.GetMainWindow.TitleText.ToolTip = titleString[1];
        }

        internal static void SetLoadingString()
        {
            LoadWindows.GetMainWindow.Title = Application.Current.Resources["Loading"] as string;
            LoadWindows.GetMainWindow.TitleText.Text = Application.Current.Resources["Loading"] as string;
            LoadWindows.GetMainWindow.TitleText.ToolTip = Application.Current.Resources["Loading"] as string;
        }
    }
}