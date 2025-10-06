using ImageMagick;
using PicView.Avalonia.Navigation;
using PicView.Avalonia.ViewModels;
using PicView.Core.DebugTools;
using PicView.Core.FileHandling;
using PicView.Core.ImageDecoding;
using PicView.Core.Localization;
using PicView.Core.Models;
using PicView.Core.Titles;

namespace PicView.Avalonia.UI;

public static class TitleManager
{
    /// <summary>
    ///     Sets the title of the window and the title displayed in the UI to the appropriate
    ///     value based on the current state of the application.
    /// </summary>
    /// <param name="vm">The main view model instance.</param>
    /// <remarks>Can be used to refresh the title when files are added or removed.</remarks>
    public static void SetTitle(MainViewModel vm)
    {
        var pWidth = vm.PicViewer.PixelWidth.Value;
        var pHeight = vm.PicViewer.PixelHeight.Value;
        var fileInfo = vm.PicViewer.FileInfo.Value;
        
        if (!NavigationManager.CanNavigate(vm))
        {
            string title;
            var s = vm.PicViewer.WindowTitle.Value;
            var url = s.GetURL();
            if (!string.IsNullOrWhiteSpace(url))
            {
                title = url;
            }
            else if (s.Contains(TranslationManager.Translation.Base64Image))
            {
                title = TranslationManager.Translation.Base64Image;
            }
            else
            {
                title = TranslationManager.Translation.ClipboardImage!;
            }

            var singleImageWindowTitles =
                ImageTitleFormatter.GenerateTitleForSingleImage(pWidth, pHeight, title,
                    vm.PicViewer.RotationAngle.CurrentValue);
            vm.PicViewer.WindowTitle.Value = singleImageWindowTitles.BaseTitle;
            vm.PicViewer.Title.Value = singleImageWindowTitles.TitleWithAppName;
            vm.PicViewer.TitleTooltip.Value = singleImageWindowTitles.TitleWithAppName;
            return;
        }

        if (NavigationManager.TiffNavigationInfo is not null)
        {
            SetTiffTitle(NavigationManager.TiffNavigationInfo, pWidth, pHeight,
                NavigationManager.GetCurrentIndex, vm.PicViewer.FileInfo.Value, vm);
            return;
        }

        if (Settings.ImageScaling.ShowImageSideBySide)
        {
            try
            {
                var imageModel1 = new ImageModel
                {
                    FileInfo = vm.PicViewer.FileInfo.Value,
                    PixelWidth = vm.PicViewer.PixelWidth.Value,
                    PixelHeight = vm.PicViewer.PixelHeight.Value
                };
                var nextFileName = NavigationManager.GetNextFileName;
                using var magickImage = new MagickImage();
                magickImage.Ping(nextFileName);
                var imageModel2 = new ImageModel
                {
                    FileInfo = new FileInfo(nextFileName),
                    PixelWidth = (int)magickImage.Width,
                    PixelHeight = (int)magickImage.Height
                };
                SetSideBySideTitle(vm, imageModel1, imageModel2);
            }
            catch (Exception e)
            {
                // TODO: fix SVG ping exception
                DebugHelper.LogDebug(nameof(TiffManager), nameof(SetTitle), e);
            }
            return;
        }

        var windowTitles = ImageTitleFormatter.GenerateTitleStrings(pWidth, pHeight,
            NavigationManager.GetCurrentIndex,
            fileInfo, vm.PicViewer.ZoomValue.CurrentValue, NavigationManager.GetCollection);
        ApplyTitles(vm, windowTitles);
    }

    /// <summary>
    ///     Sets the title of the window and the title displayed in the UI
    ///     to a temporary "Loading..." title while the application is loading current image.
    /// </summary>
    /// <param name="vm">The main view model instance.</param>
    public static void SetLoadingTitle(MainViewModel vm)
    {
        vm.PicViewer.TitleTooltip.Value = vm.PicViewer.WindowTitle.Value = $"{TranslationManager.Translation.Loading} - PicView";
        vm.PicViewer.Title.Value = TranslationManager.Translation.Loading ?? string.Empty;
    }

    /// <summary>
    ///     Sets the window title and UI title based on the provided image model.
    /// </summary>
    /// <param name="vm">The main view model instance.</param>
    /// <param name="imageModel">The image model containing information about the current image.</param>
    /// <remarks>If the image model or its file info is null, an error message is set as the title.</remarks>
    public static void SetTitle(MainViewModel vm, ImageModel? imageModel)
    {
        if (!ValidateImageModel(imageModel, vm))
        {
            if (vm.PicViewer.FileInfo is null)
            {
                ReturnError(vm);
                return;
            }
            imageModel = new ImageModel
            {
                FileInfo = vm.PicViewer.FileInfo.Value,
                PixelWidth = vm.PicViewer.PixelWidth.Value,
                PixelHeight = vm.PicViewer.PixelHeight.Value
            };
        }

        var windowTitles = ImageTitleFormatter.GenerateTitleStrings(imageModel.PixelWidth, imageModel.PixelHeight,
            NavigationManager.GetCurrentIndex,
            imageModel.FileInfo, vm.PicViewer.ZoomValue.CurrentValue, NavigationManager.GetCollection);
        ApplyTitles(vm, windowTitles);
    }
    
    public static void SetTitleSlim(MainViewModel vm, int width, int height, int index, IReadOnlyList<FileInfo> collection)
    {
        var windowTitles = ImageTitleFormatter.GenerateTitleStrings(width, height, index, collection[index], 0, collection);
        ApplyTitles(vm, windowTitles);
    }

    /// <summary>
    ///     Sets the title of the window and the title displayed in the UI
    ///     based on the provided TIFF navigation info and image model.
    /// </summary>
    /// <param name="tiffNavigationInfo">The TIFF navigation info object.</param>
    /// <param name="width">The width of the image in pixels.</param>
    /// <param name="height">The height of the image in pixels.</param>
    /// <param name="index">The index of the image in the list.</param>
    /// <param name="fileInfo">The FileInfo object representing the image file.</param>
    /// <param name="vm">The main view model instance.</param>
    /// <remarks>
    ///     This method is used to set the title of the window and the title displayed in the UI
    ///     for TIFF images. The image title is generated using the TIFF navigation info and image model.
    /// </remarks>
    public static void SetTiffTitle(TiffManager.TiffNavigationInfo tiffNavigationInfo, int width, int height, int index,
        FileInfo fileInfo, MainViewModel vm)
    {
        var singeImageWindowTitles = ImageTitleFormatter.GenerateTiffTitleStrings(width, height, index, fileInfo,
            tiffNavigationInfo, 1, NavigationManager.GetCollection);
        ApplyTitles(vm, singeImageWindowTitles);
    }

    /// <summary>
    ///     Sets the title of the window and the title displayed in the UI
    ///     based on the provided image model and main view model instance.
    ///     If the image is a TIFF with multiple pages, the title is generated
    ///     using the TIFF navigation info and image model. Otherwise, the single
    ///     image title is generated using the image model.
    /// </summary>
    /// <param name="imageModel">The image model containing the image information.</param>
    /// <param name="vm">The main view model instance.</param>
    /// <remarks>
    ///     This method is used to set the title of the window and the title displayed in the UI
    ///     for TIFF images with multiple pages. If the image is not a TIFF with multiple pages,
    ///     the single image title is generated using the image model.
    /// </remarks>
    public static void TrySetTiffTitle(ImageModel? imageModel, MainViewModel vm)
    {
        if (!ValidateImageModel(imageModel, vm))
        {
            return;
        }

        if (TiffManager.GetTiffPageCount(imageModel.FileInfo.FullName) is { } pageCount and > 1)
        {
            var tiffNavigationInfo = new TiffManager.TiffNavigationInfo
            {
                CurrentPage = 0,
                PageCount = pageCount,
                Pages = TiffManager.LoadTiffPages(imageModel.FileInfo.FullName)
            };
            SetTiffTitle(tiffNavigationInfo, imageModel.PixelWidth, imageModel.PixelHeight,
                NavigationManager.GetCurrentIndex, imageModel.FileInfo, vm);
        }
        else
        {
            SetTitle(vm, imageModel);
        }
    }

    /// <summary>
    ///     Sets the title of the window and the title displayed in the UI when showing two images side by side.
    /// </summary>
    /// <param name="vm">The main view model instance.</param>
    /// <param name="imageModel1">The first image model containing the image information.</param>
    /// <param name="imageModel2">The second image model containing the image information.</param>
    /// <remarks>
    ///     This method is used to set the title of the window and the title displayed in the UI.
    ///     The title is a combination of the titles of the two images.
    /// </remarks>
    public static void SetSideBySideTitle(MainViewModel vm, ImageModel? imageModel1, ImageModel? imageModel2)
    {
        // Fix image models, which can be null caused by race conditions?
        if (!ValidateImageModel(imageModel1, vm))
        {
            if (vm.PicViewer.FileInfo is null)
            {
                return;
            }
            imageModel1 = new ImageModel
            {
                FileInfo = vm.PicViewer.FileInfo.Value,
                PixelWidth = vm.PicViewer.PixelWidth.Value,
                PixelHeight = vm.PicViewer.PixelHeight.Value
            };
        }
        if (!ValidateImageModel(imageModel2, vm))
        {
            if (!NavigationManager.CanNavigate(vm))
            {
                return;
            }
            var nextFileName = NavigationManager.GetNextFileName;
            if (string.IsNullOrWhiteSpace(nextFileName))
            {
                return;
            }
            using var magickImage = new MagickImage();
            magickImage.Ping(nextFileName);
            imageModel2 = new ImageModel
            {
                FileInfo = new FileInfo(nextFileName),
                PixelWidth = (int)magickImage.Width,
                PixelHeight = (int)magickImage.Height
            };
        }
        
        var firstWindowTitles = ImageTitleFormatter.GenerateTitleStrings(imageModel1.PixelWidth,
            imageModel1.PixelHeight, NavigationManager.GetCurrentIndex,
            imageModel1.FileInfo, vm.PicViewer.RotationAngle.CurrentValue, NavigationManager.GetCollection);
        var secondWindowTitles = ImageTitleFormatter.GenerateTitleStrings(imageModel2.PixelWidth,
            imageModel2.PixelHeight, NavigationManager.GetNextIndex,
            imageModel2.FileInfo, vm.PicViewer.RotationAngle.CurrentValue, NavigationManager.GetCollection);
        var windowTitle = $"{firstWindowTitles.BaseTitle} \u21dc || \u21dd {secondWindowTitles.BaseTitle} - PicView";
        var title = $"{firstWindowTitles.BaseTitle} \u21dc || \u21dd  {secondWindowTitles.BaseTitle}";
        var titleTooltip = $"{firstWindowTitles.FilePathTitle} \u21dc || \u21dd  {secondWindowTitles.FilePathTitle}";
        vm.PicViewer.WindowTitle.Value = windowTitle;
        vm.PicViewer.Title.Value = title;
        vm.PicViewer.TitleTooltip.Value = titleTooltip;
    }

    /// <summary>
    ///     Sets the window title and UI title to indicate no image is available.
    /// </summary>
    /// <param name="vm">The main view model instance.</param>
    /// <remarks>
    ///     This method sets the title, window title, and tooltip to a default message
    ///     indicating that no image is currently loaded or available.
    /// </remarks>
    public static void SetNoImageTitle(MainViewModel vm)
    {
        vm.PicViewer.Title.Value = TranslationManager.Translation.NoImage ?? string.Empty;
        vm.PicViewer.WindowTitle.Value = TranslationManager.Translation.NoImage + " - PicView";
        vm.PicViewer.TitleTooltip.Value = TranslationManager.Translation.NoImage ?? string.Empty;
    }

    private static void ReturnError(MainViewModel vm)
    {
        vm.PicViewer.WindowTitle.Value =
            vm.PicViewer.Title.Value =
                vm.PicViewer.TitleTooltip.Value = TranslationManager.GetTranslation("UnableToRender");
    }

    private static bool ValidateImageModel(ImageModel? imageModel, MainViewModel vm)
    {
        if (imageModel?.FileInfo is not null)
        {
            return true;
        }

        ReturnError(vm);
        return false;
    }
    
    private static void ApplyTitles(MainViewModel vm, WindowTitles titles)
    {
        vm.PicViewer.WindowTitle.Value = titles.TitleWithAppName;
        vm.PicViewer.Title.Value = titles.BaseTitle;
        vm.PicViewer.TitleTooltip.Value = titles.FilePathTitle;
    }
}