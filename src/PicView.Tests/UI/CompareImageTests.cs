using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Views.UC;
using static PicView.Tests.UI.TabViewsHarness;

namespace PicView.Tests.UI;

/// <summary>
/// The viewer stacks the partner image on the current one while comparing, blended in Difference (issue #367), and
/// leaves it hidden otherwise.
/// </summary>
[Collection("Sequential")]
public class CompareImageTests
{
    public CompareImageTests()
    {
        SetDefaults();
    }

    [AvaloniaFact]
    public void CompareImage_ComparisonOff_IsHidden() => WithWindow((window, _, tab) =>
    {
        tab.Display(BitmapModel("a.jpg"));
        Settle(window);
        var compare = CompareImageOf(tab);

        Assert.False(compare.IsVisible);
    });

    [AvaloniaFact]
    public void CompareImage_ComparisonOn_IsVisibleWithDifferenceAndFill() => WithWindow((window, vm, tab) =>
    {
        tab.Display(BitmapModel("a.jpg"));
        Settle(window);

        vm.GlobalSettings.IsComparing.Value = true;
        Settle(window);
        var compare = CompareImageOf(tab);

        Assert.True(compare.IsVisible);
        Assert.Equal(BitmapBlendingMode.Difference, compare.BlendMode);
        Assert.Equal(Stretch.Fill, compare.Stretch);
    });

    private static PicBox CompareImageOf(PicView.Core.ViewModels.TabViewModel tab)
    {
        var viewer = Assert.IsType<ImageViewer>(ViewOf(tab));
        return Assert.IsType<PicBox>(viewer.FindControl<PicBox>("CompareImage"));
    }
}
