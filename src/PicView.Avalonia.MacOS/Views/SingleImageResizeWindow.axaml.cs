using PicView.Avalonia.CustomControls;
using PicView.Avalonia.UI;
using PicView.Core.Extensions;
using PicView.Core.Localization;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.MacOS.Views;

public partial class SingleImageResizeWindow : GenericWindow
{
    public SingleImageResizeWindow(MainWindowViewModel vm)
    {
        DataContext = vm;
        InitializeComponent();
        GenericWindowHelper.GenericWindowInitialize(this, StringExtensions.CombineWithAppName(TranslationManager.Translation.ResizeImage));
    }
}