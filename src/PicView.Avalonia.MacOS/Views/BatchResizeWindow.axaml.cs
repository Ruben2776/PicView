using PicView.Avalonia.CustomControls;
using PicView.Avalonia.UI;
using PicView.Core.Config;
using PicView.Core.Extensions;
using PicView.Core.Localization;

namespace PicView.Avalonia.MacOS.Views;

public partial class BatchResizeWindow : GenericWindow
{
    public BatchResizeWindow(BatchResizeWindowConfig config)
    {
        InitializeComponent();
        GenericWindowHelper.GenericWindowInitialize(this, StringExtensions.CombineWithAppName(TranslationManager.Translation.BatchResize), false, config.WindowProperties);
    }
}