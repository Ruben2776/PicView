using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Views.UC;
using PicView.Core.ImageDecoding;
using PicView.Core.Localization;
using PicView.Core.Models;
using PicView.Core.Svg;
using PicView.Core.ViewModels;
using static PicView.Tests.UI.TabViewsHarness;

namespace PicView.Tests.UI;

[Collection("Sequential")]
public class FileHistoryItemTests : IDisposable
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".svg");

    public FileHistoryItemTests()
    {
        SetDefaults();
        TranslationManager.Init();
        File.WriteAllText(_path, "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"10\" height=\"10\"/>");
    }

    public void Dispose() => File.Delete(_path);

    [AvaloniaFact]
    public void Tooltip_CachedSvg_ShowsDrawingImage()
        => WithWindow((_, vm, tab) =>
        {
            // The open SVG and its preloaded neighbors are in the shared cache as the parsed drawing
            var file = new FileInfo(_path);
            vm.WindowTabs.SharedCache!.Put(tab.Id, file, new ImageModel
            {
                FileInfo = file,
                Image = new SvgDrawing(10, 10, new SvgGroup(null, 1, null, [])),
                PixelWidth = 10,
                PixelHeight = 10,
                ImageType = ImageType.Svg
            });
            using var entry = new FileHistoryEntryViewModel();
            entry.Initialize(_path, file.Name, isPinned: false, isCurrentItem: true, index: 0, vm);
            var item = new FileHistoryItem { DataContext = entry };

            // The tooltip's content takes its DataContext when it opens
            var tip = Assert.IsType<StackPanel>(ToolTip.GetTip(item.FindControl<Button>("MainButton")!));
            tip.DataContext = entry;
            Dispatcher.UIThread.RunJobs();

            var thumb = Assert.IsType<ThumbImage>(tip.Children[0]);
            Assert.IsType<DrawingImage>(thumb.Source);
        });
}
