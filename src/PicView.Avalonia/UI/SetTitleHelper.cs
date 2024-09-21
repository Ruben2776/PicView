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
                title = TranslationHelper.Translation.Base64Image ?? "Base64Image";
            }
            else
            {
                title = TranslationHelper.Translation.ClipboardImage ?? "ClipboardImage";
            }
            
            var singeImageWindowTitles = ImageTitleFormatter.GenerateTitleForSingleImage(vm.ImageWidth, vm.ImageHeight, title, vm.ZoomValue);
            vm.WindowTitle = singeImageWindowTitles.BaseTitle;
            vm.Title = singeImageWindowTitles.TitleWithAppName;
            vm.TitleTooltip = singeImageWindowTitles.TitleWithAppName;
            return;
        }

        var windowTitles = ImageTitleFormatter.GenerateTitleStrings(vm.ImageWidth, vm.ImageHeight, vm.ImageIterator.CurrentIndex,
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

    public static void ResetTitle(MainViewModel vm)
    {
        vm.WindowTitle = TranslationHelper.GetTranslation("NoImage") + " - PicView";
        vm.TitleTooltip = vm.Title = TranslationHelper.GetTranslation("NoImage");
    }
}
