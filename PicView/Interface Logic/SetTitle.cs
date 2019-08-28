using System.IO;
using System.Text;
using static PicView.Fields;
using static PicView.FileFunctions;
namespace PicView
{
    internal static class SetTitle
    {
        /// <summary>
        /// Returns string with file name, folder position,
        /// zoom, aspect ratio, resolution and file size
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        internal static string[] TitleString(int width, int height, int index)
        {
            var s1 = new StringBuilder();
            s1.Append(AppName).Append(" - ").Append(Path.GetFileName(Pics[index])).Append(" ").Append(index + 1).Append("/").Append(Pics.Count).Append(" files")
                    .Append(" (").Append(width).Append(" x ").Append(height).Append(StringAspect(width, height)).Append(GetSizeReadable(new FileInfo(Pics[index]).Length));

            if (!string.IsNullOrEmpty(ZoomPercentage))
                s1.Append(" - ").Append(ZoomPercentage);

            var array = new string[3];
            array[0] = s1.ToString();
            s1.Remove(0, AppName.Length + 3);   // Remove AppName + " - "
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
            mainWindow.Title = titleString[0];
            mainWindow.Bar.Text = titleString[1];
            mainWindow.Bar.ToolTip = titleString[1];
        }

        /// <summary>
        /// Returns string with file name,
        /// zoom, aspect ratio and resolution
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        internal static string[] TitleString(int width, int height, string path)
        {
            var s1 = new StringBuilder();
            s1.Append(AppName).Append(" - ").Append(path).Append(" (").Append(width).Append(" x ").Append(height).Append(StringAspect(width, height));

            if (!string.IsNullOrEmpty(ZoomPercentage))
                s1.Append(" - ").Append(ZoomPercentage);

            var array = new string[2];
            array[0] = s1.ToString();
            s1.Remove(0, AppName.Length + 3);   // Remove AppName + " - "
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
            mainWindow.Title = titleString[0];
            mainWindow.Bar.Text = titleString[1];
            mainWindow.Bar.ToolTip = titleString[1];
        }
    }
}
