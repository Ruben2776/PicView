using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Svg.Skia;
using Avalonia.Threading;
using ImageMagick;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.ImageHandling;
using PicView.Avalonia.Views.UC;
using PicView.Avalonia.WindowBehavior;
using PicView.Core.DebugTools;
using PicView.Core.Gallery;
using PicView.Core.ImageDecoding;
using PicView.Core.Localization;
using PicView.Core.Models;
using PicView.Core.Titles;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.Navigation;

public static class UpdateImage
{
    public static void UpdateFileInfo(TabViewModel tabViewModel,  FileInfo? file)
    {
        if (tabViewModel.Model?.Image is null || tabViewModel.Model.PixelHeight is 0 ||
            tabViewModel.Model.PixelWidth is 0 || tabViewModel.SingleImageType is not SingleImageType.None)
        {
            return;
        }
        
        if (file is null || file.Length is 0)
        {
            var noImage = TranslationManager.Translation?.NoImage;
            if (string.IsNullOrEmpty(noImage))
            {
                return;
            }

            tabViewModel.TabTitle.Value = noImage;
            tabViewModel.TabTooltip.Value = noImage;
            return;
        }
        tabViewModel.FileInfo.Value = file;

        if (Application.Current.DataContext is not CoreViewModel core)
        {
            return;
        }
        
        if (Settings.UIProperties.IsTaskbarProgressEnabled && tabViewModel.ImageIterator.CurrentIndex > -1 && tabViewModel.ImageIterator.Files.Count > 0)
        {
            core.PlatformService.SetTaskbarProgress((ulong)tabViewModel.ImageIterator.CurrentIndex, (ulong)tabViewModel.ImageIterator.Files.Count);
        }

        core.Effects?.ProcessedImage = new MagickImage(file);
    }
    
    public static void UpdateTabSideBySideTitles(TabViewModel tabViewModel,
        int index,
        int secondaryIndex,
        FileInfo firstFile,
        FileInfo secondFile,
        List<FileInfo> files)
    {
        tabViewModel.FileInfo.Value = firstFile;
        tabViewModel.SecondaryFileInfo.Value = secondFile;
        var count = tabViewModel.ImageIterator.Files.Count;
        var zoom = tabViewModel.ZoomLevel.CurrentValue;
        var firstInfo = new ImageTitleInfo(firstFile,
            tabViewModel.Model.PixelWidth,
            tabViewModel.Model.PixelHeight,
            index,
            count);
        var secondInfo = new ImageTitleInfo(secondFile,
            tabViewModel.SecondaryModel.PixelWidth,
            tabViewModel.SecondaryModel.PixelHeight,
            secondaryIndex,
            count);
        var titles = ImageTitleFormatter.GenerateTitleForSideBySide(firstInfo, secondInfo,
            zoom,
            files);
        tabViewModel.WindowTitle.Value = titles.TitleWithAppName;
        tabViewModel.Title.Value = titles.BaseTitle;
        tabViewModel.TitleTooltip.Value = titles.FilePathTitle;
    }

    public static void ChangeImage(MainWindow mainWindow, TabViewModel tabViewModel, MainWindowViewModel vm)
    {
        if (vm.WindowTabs.ActiveTab.CurrentValue.CurrentView.CurrentValue is not ImageViewer imageViewer)
        {
            return;
        }
        
        if (Settings.Zoom.ResetZoomOnChange)
        {
            imageViewer.ResetZoomSlim();
            tabViewModel.RotationAngle.Value = 0;
        }
        
        if (tabViewModel.Model.ImageType is ImageType.Svg)
        {
            Dispatcher.UIThread.Invoke(() =>
            {
                tabViewModel.Image.Value = new SvgImage { Source = tabViewModel.Model.Image as SvgSource };
            }, DispatcherPriority.Send);
        }
        else
        {
            tabViewModel.Image.Value = tabViewModel.Model.Image;
        }
        tabViewModel.ImageType.Value = tabViewModel.Model.ImageType;
        
        SetWindowAndImageSize(mainWindow, tabViewModel, vm);

        imageViewer.UpdateMotionPhoto(tabViewModel);

        if (tabViewModel.Gallery.IsDockedGalleryVisible.CurrentValue)
        {
            Dispatcher.UIThread.Invoke(() =>
            {
                imageViewer.GalleryView.GalleryItemsControl.ScrollToCenterOfCurrentItem();
            }, DispatcherPriority.Render);
        }
        tabViewModel.ZoomLevel.Value = Convert.ToInt32(tabViewModel.InitialZoom.CurrentValue * 100);
        tabViewModel.UpdateTabTitle();
    }

    public static void SetWindowAndImageSize(MainWindow mainWindow, TabViewModel tabViewModel, MainWindowViewModel vm)
    {
        double secondaryWidth, secondaryHeight;
        if (Settings.ImageScaling.ShowImageSideBySide)
        {
            if (tabViewModel.SecondaryModel is null)
            {
#if DEBUG
                DebugHelper.LogDebug(nameof(UpdateImage),
                    nameof(ChangeImage),
                    "SecondaryModel.CurrentValue is null");
#endif
                secondaryWidth = 0;
                secondaryHeight = 0;
                tabViewModel.SecondaryImage.Value = null;
                tabViewModel.SecondaryFileInfo.Value = null;
                tabViewModel.SecondaryImageType.Value = null;
            }
            else
            {
                secondaryWidth = tabViewModel.SecondaryModel.PixelWidth;
                secondaryHeight = tabViewModel.SecondaryModel.PixelHeight;                
                tabViewModel.SecondaryImage.Value = tabViewModel.SecondaryModel.Image;
                tabViewModel.SecondaryImageType.Value = tabViewModel.SecondaryModel.ImageType;
                tabViewModel.SecondaryFileInfo.Value = tabViewModel.SecondaryModel.FileInfo;
            }
        }
        else
        {
            secondaryWidth = secondaryHeight = 0;
        }
        
        WindowResizing.SetSize(tabViewModel.Model.PixelWidth,
            tabViewModel.Model.PixelHeight, 
            secondaryWidth, secondaryHeight,
            WindowResizeReason.Application,
            mainWindow, vm);
    }

    public static void SetSingleImage(MainWindowViewModel vm, MainWindow mainWindow, Bitmap image, SingleImageType type, string name)
    {
        var tabViewModel = vm.WindowTabs.ActiveTab.CurrentValue;
        tabViewModel.Image.Value = image;
        tabViewModel.ImageType.Value = ImageType.Bitmap;

        tabViewModel.Gallery.ActiveGalleryMode.Value = GalleryMode.Closed;

        var width = (uint)image.PixelSize.Width;
        var height = (uint)image.PixelSize.Height;

        tabViewModel.Model.PixelWidth = width;
        tabViewModel.Model.PixelHeight = height;

        tabViewModel.SingleImageType = type;
        
        if (Settings.WindowProperties.AutoFit)
        {
            WindowResizing.SetSize(width, height, 0,0,
                WindowResizeReason.Application,
                mainWindow, vm);
        }
        Dispatcher.UIThread.Invoke(() =>
        {
            if (tabViewModel.CurrentView?.CurrentValue is not ImageViewer imageViewer)
            {
                tabViewModel.CurrentView.Value = new ImageViewer();
                WindowResizing.SetSize(width, height, 0,0,
                    WindowResizeReason.Application,
                    mainWindow, vm);
                return;
            }

            imageViewer.ResetZoomSlim();
            imageViewer.Rotate(0);
            imageViewer.UpdateMotionPhoto(tabViewModel);
        });
        
        var zoom = tabViewModel.ZoomLevel.CurrentValue;
        var windowTitles = ImageTitleFormatter.GenerateTitleForSingleImage(width, height, name, zoom);
        tabViewModel.WindowTitle.Value = windowTitles.TitleWithAppName;
        tabViewModel.Title.Value = windowTitles.BaseTitle;
        tabViewModel.TitleTooltip.Value = windowTitles.FilePathTitle;
        tabViewModel.TabTitle.Value = name;
        
        tabViewModel.DisposeImageIterator();
    }

    public static async ValueTask SetSingeBase64ImageAsync(string base64, MainWindowViewModel vm, MainWindow mainWindow, CancellationToken ct)
    {
        var base64Model =
            await GetImageModel.GetBase64ImageModelAsync(base64, ct)
                .ConfigureAwait(false);

        if (base64Model is null)
        {
            return;
        }
        
        SetSingleImage(vm, mainWindow, base64Model.Image as Bitmap, SingleImageType.Base64, TranslationManager.Translation.Base64Image);
    }
}