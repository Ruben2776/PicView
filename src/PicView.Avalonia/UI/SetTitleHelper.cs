using PicView.Avalonia.ImageHandling;
using PicView.Avalonia.ViewModels;
using PicView.Core.FileHandling;
using PicView.Core.Localization;
using PicView.Core.Navigation;

namespace PicView.Avalonia.UI;

public static class SetTitleHelper
{
    public static void SetTitle(MainViewModel vm)
    {
        if (vm.ImageIterator is null || vm.FileInfo is null)
        {
            string title;
            var s = vm.Title;
            if (!string.IsNullOrWhiteSpace(s.GetURL()))
            {
                title = vm.Title.GetURL();
            }
            else if (s.Contains(TranslationHelper.Translation.Base64Image))
            {
                title = TranslationHelper.Translation.Base64Image ?? "Base64 Image";
            }
            else
            {
                title = TranslationHelper.Translation.ClipboardImage ?? "Clipboard Image";
            }
            
            var singeImageWindowTitles = ImageTitleFormatter.GenerateTitleForSingleImage(vm.PixelWidth, vm.PixelWidth, title, vm.ZoomValue);
            vm.WindowTitle = singeImageWindowTitles.BaseTitle;
            vm.Title = singeImageWindowTitles.TitleWithAppName;
            vm.TitleTooltip = singeImageWindowTitles.TitleWithAppName;
            return;
        }

        var windowTitles = ImageTitleFormatter.GenerateTitleStrings(vm.PixelWidth, vm.PixelHeight, vm.ImageIterator.CurrentIndex,
            vm.FileInfo, vm.ZoomValue, vm.ImageIterator.ImagePaths);
        vm.WindowTitle = windowTitles.TitleWithAppName;
        vm.Title = windowTitles.BaseTitle;
        vm.TitleTooltip = windowTitles.FilePathTitle;
    }

    public static void RefreshTitle(MainViewModel vm)
    {
        if (vm.FileInfo == null)
        {
            return;
        }
        var path = vm.FileInfo.FullName;
        vm.FileInfo = new FileInfo(path);
        SetTitle(vm);
    }
    
    public static void SetLoadingTitle(MainViewModel vm)
    {
        vm.WindowTitle = $"{TranslationHelper.Translation.Loading} - PicView";
        vm.Title = TranslationHelper.Translation.Loading;
        vm.TitleTooltip = vm.Title;
    }
    
    public static void SetTitle(MainViewModel vm, ImageModel? imageModel)
    {
        if (imageModel is null)
        {
            ReturnError();
            return;
        }

        if (imageModel.FileInfo is null)
        {
            ReturnError();
            return;
        }

        var windowTitles = ImageTitleFormatter.GenerateTitleStrings(imageModel.PixelWidth, imageModel.PixelHeight,  vm.ImageIterator.CurrentIndex,
            imageModel.FileInfo,  vm.ZoomValue,  vm.ImageIterator.ImagePaths);
        vm.WindowTitle = windowTitles.TitleWithAppName;
        vm.Title = windowTitles.BaseTitle;
        vm.TitleTooltip = windowTitles.FilePathTitle;

        return;

        void ReturnError()
        {
            vm.WindowTitle =
                vm.Title =
                    vm.TitleTooltip = TranslationHelper.GetTranslation("UnableToRender");
        }
    }
    
    public static void SetSideBySideTitle(MainViewModel vm, ImageModel? imageModel1, ImageModel? imageModel2)
    {
        if (imageModel1 is null || imageModel2 is null)
        {
            ReturnError();
            return;
        }

        if (imageModel1.FileInfo is null || imageModel2.FileInfo is null)
        {
            ReturnError();
            return;
        }

        var firstWindowTitles = ImageTitleFormatter.GenerateTitleStrings(imageModel1.PixelWidth, imageModel1.PixelHeight,  vm.ImageIterator.CurrentIndex,
            imageModel1.FileInfo,  vm.ZoomValue,  vm.ImageIterator.ImagePaths);
        var secondWindowTitles = ImageTitleFormatter.GenerateTitleStrings(imageModel2.PixelWidth, imageModel2.PixelHeight,  vm.ImageIterator.NextIndex,
            imageModel2.FileInfo,  vm.ZoomValue,  vm.ImageIterator.ImagePaths);
        var windowTitle = $"{firstWindowTitles.BaseTitle} \u21dc || \u21dd {secondWindowTitles.BaseTitle} - PicView";
        var title = $"{firstWindowTitles.BaseTitle} \u21dc || \u21dd  {secondWindowTitles.BaseTitle}";
        var titleTooltip = $"{firstWindowTitles.FilePathTitle} \u21dc || \u21dd  {secondWindowTitles.FilePathTitle}";
        vm.WindowTitle = windowTitle;
        vm.Title = title;
        vm.TitleTooltip = titleTooltip;

        return;

        void ReturnError()
        {
            vm.WindowTitle =
                vm.Title =
                    vm.TitleTooltip = TranslationHelper.GetTranslation("UnableToRender");
        }
    }

    public static void ResetTitle(MainViewModel vm)
    {
        vm.WindowTitle = TranslationHelper.GetTranslation("NoImage") + " - PicView";
        vm.TitleTooltip = vm.Title = TranslationHelper.GetTranslation("NoImage");
    }
}
