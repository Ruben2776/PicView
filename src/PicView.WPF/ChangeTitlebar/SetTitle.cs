using PicView.Core.FileHandling;
using PicView.Core.Localization;
using PicView.Core.Navigation;
using PicView.WPF.ChangeImage;
using PicView.WPF.UILogic;
using System.Diagnostics;
using System.IO;
using System.Windows.Threading;
using static PicView.WPF.ChangeImage.Navigation;
using static PicView.WPF.UILogic.TransformImage.ZoomLogic;

namespace PicView.WPF.ChangeTitlebar;

/// <summary>
/// Provides methods for setting titles in the application.
/// </summary>
internal static class SetTitle
{
    /// <summary>
    /// Sets the title string based on the specified width, height, index, and file information.
    /// </summary>
    /// <param name="width">The width of the image.</param>
    /// <param name="height">The height of the image.</param>
    /// <param name="index">The index of the image.</param>
    /// <param name="fileInfo">The file information of the image.</param>
    internal static void SetTitleString(int width, int height, int index, FileInfo? fileInfo)
    {
        var titleString = TitleHelper.GetTitle(width, height, index, fileInfo, ZoomValue, Pics);

        ConfigureWindows.GetMainWindow.Title = titleString[0];
        ConfigureWindows.GetMainWindow.TitleText.Text = titleString[1];
        ConfigureWindows.GetMainWindow.TitleText.ToolTip = titleString[2];
    }

    /// <summary>
    /// Sets the title string with default parameters.
    /// </summary>
    internal static void SetTitleString()
    {
        string[]? titleString;
        var preloadValue = PreLoader.Get(FolderIndex);
        var width = preloadValue?.BitmapSource?.PixelWidth ?? ConfigureWindows.GetMainWindow.MainImage.Source?.Width ?? 0;
        var height = preloadValue?.BitmapSource?.PixelHeight ?? ConfigureWindows.GetMainWindow.MainImage.Source?.Height ?? 0;
        FileInfo? fileInfo;
        // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
        if (ErrorHandling.CheckOutOfRange())
        {
            fileInfo = null;
        }
        else
        {
            if (Pics.Count < FolderIndex || Pics.Count < 1 || FolderIndex > Pics.Count)
            {
                fileInfo = null;
            }
            else
            {
                try
                {
                    fileInfo = new FileInfo(Pics[FolderIndex]);
                }
                catch (Exception exception)
                {
#if DEBUG
                    Trace.WriteLine($"{nameof(SetTitleString)} exception:\n{exception.Message}");
#endif
                    return;
                }
            }
        }

        if (fileInfo is null)
        {
            string title;
            var s = ConfigureWindows.GetMainWindow.TitleText.Text;
            if (!string.IsNullOrWhiteSpace(s.GetURL()))
            {
                if (Dispatcher.CurrentDispatcher.CheckAccess())
                {
                    title = s.GetURL();
                }
                else
                {
                    title = Dispatcher.CurrentDispatcher.Invoke(() => s.GetURL());
                }
                
            }
            else if (s.Contains(TranslationHelper.Translation.Base64Image))
            {
                title = TranslationHelper.Translation.Base64Image ?? "Base64Image";
            }
            else
            {
                title = TranslationHelper.Translation.ClipboardImage ?? "ClipboardImage";
            }
            titleString = TitleHelper.TitleString((int)width, (int)height, title, ZoomValue);
        }
        else
        {
            titleString = TitleHelper.GetTitle((int)width, (int)height, FolderIndex, fileInfo, ZoomValue, Pics);
        }

        ConfigureWindows.GetMainWindow.Title = titleString[0];
        ConfigureWindows.GetMainWindow.TitleText.Text = titleString[1];
        ConfigureWindows.GetMainWindow.TitleText.ToolTip = titleString[1];
    }

    /// <summary>
    /// Sets the title string based on the specified width, height, and path.
    /// </summary>
    /// <param name="width">The width of the image.</param>
    /// <param name="height">The height of the image.</param>
    /// <param name="path">The path of the image.</param>
    internal static void SetTitleString(int width, int height, string path)
    {
        var titleString = TitleHelper.TitleString(width, height, path, ZoomValue);
        ConfigureWindows.GetMainWindow.Title = titleString[0];
        ConfigureWindows.GetMainWindow.TitleText.Text = titleString[1];
        ConfigureWindows.GetMainWindow.TitleText.ToolTip = titleString[1];
    }

    /// <summary>
    /// Sets the loading string for the application.
    /// </summary>
    internal static void SetLoadingString()
    {
        if (TranslationHelper.GetTranslation("Loading") is not { } loading ||
            ConfigureWindows.GetMainWindow.Title == null || ConfigureWindows.GetMainWindow.TitleText == null)
        {
            return;
        }

        ConfigureWindows.GetMainWindow.Title = loading;
        ConfigureWindows.GetMainWindow.TitleText.Text = loading;
        ConfigureWindows.GetMainWindow.TitleText.ToolTip = loading;
    }
}