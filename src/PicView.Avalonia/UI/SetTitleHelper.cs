using Avalonia.Threading;
using PicView.Avalonia.ImageHandling;
using PicView.Avalonia.ViewModels;
using PicView.Core.FileHandling;
using PicView.Core.ImageDecoding;
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
            
            var titleString = TitleHelper.TitleString((int)vm.ImageWidth, (int)vm.ImageHeight, title, vm.ZoomValue);
            vm.WindowTitle = titleString[0];
            vm.Title = titleString[1];
            vm.TitleTooltip = titleString[1];
            return;
        }

        var getTitle = TitleHelper.GetTitle((int)vm.ImageWidth, (int)vm.ImageHeight, vm.ImageIterator.Index,
            vm.FileInfo, vm.ZoomValue, vm.ImageIterator.Pics);
        vm.WindowTitle = getTitle[0];
        vm.Title = getTitle[1];
        vm.TitleTooltip = getTitle[2];
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
        vm. WindowTitle = TranslationHelper.GetTranslation("Loading") + " - PicView";
        vm.TitleTooltip = vm.Title = TranslationHelper.GetTranslation("Loading");
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

        var titleString = TitleHelper.GetTitle(imageModel.PixelWidth, imageModel.PixelHeight,  vm.ImageIterator.Index,
            imageModel.FileInfo,  vm.ZoomValue,  vm.ImageIterator.Pics);
        vm.WindowTitle = titleString[0];
        vm.Title = titleString[1];
        vm.TitleTooltip = titleString[2];

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
