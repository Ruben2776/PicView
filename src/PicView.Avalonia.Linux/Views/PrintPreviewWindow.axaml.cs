using PicView.Avalonia.CustomControls;
using PicView.Avalonia.UI;
using PicView.Core.Config;
using PicView.Core.Extensions;
using PicView.Core.Localization;

namespace PicView.Avalonia.Linux.Views;

public partial class PrintPreviewWindow : PrintWindow
{
    public PrintPreviewWindow(PrintWindowConfig config)
    {
        Config = config;
        InitializeComponent();
        GenericWindowHelper.GenericWindowInitialize(this, StringExtensions.CombineWithAppName(TranslationManager.Translation.Print));
        SetWindowSize();
    }
}