using System.Globalization;
using Avalonia.Controls;
using Avalonia.Data;
using PicView.Avalonia.Converters;
using PicView.Avalonia.Views.UC;
using PicView.Core.Localization;
using static PicView.Tests.UI.TabViewsHarness;

namespace PicView.Tests.UI;

/// <summary>
/// The Side-by-Side order places the pair's images only: left to right shows the first image on the left, right to
/// left on the right.
/// </summary>
[Collection("Sequential")]
public class SideBySideOrderTests
{
    public SideBySideOrderTests()
    {
        SetDefaults();
        TranslationManager.Init();
    }

    [Theory]
    [InlineData(false, null, Dock.Left)]
    [InlineData(false, "Secondary", Dock.Right)]
    [InlineData(true, null, Dock.Right)]
    [InlineData(true, "Secondary", Dock.Left)]
    public void DockConverter_DocksEachImageOfThePair(bool isRightToLeft, string? parameter, Dock expected) =>
        Assert.Equal(expected,
            new SideBySideDockConverter().Convert(isRightToLeft, typeof(Dock), parameter, CultureInfo.InvariantCulture));

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    public void BoolToIndexConverter_RoundTrips(int index, bool value)
    {
        var converter = new BoolToIndexConverter();
        Assert.Equal(index, converter.Convert(value, typeof(int), null, CultureInfo.InvariantCulture));
        Assert.Equal(value, converter.ConvertBack(index, typeof(bool), null, CultureInfo.InvariantCulture));
    }

    [Fact]
    public void BoolToIndexConverter_NoSelection_ChangesNothing() =>
        Assert.Same(BindingOperations.DoNothing,
            new BoolToIndexConverter().ConvertBack(-1, typeof(bool), null, CultureInfo.InvariantCulture));

    [AvaloniaFact]
    public void ImageViewer_OrderChanged_SwapsTheSidesOfThePair()
        => WithWindow((window, vm, tab) =>
        {
            tab.Display(BitmapModel("a.jpg"));
            Settle(window);
            var viewer = Assert.IsType<ImageViewer>(ViewerOf(tab));
            var first = (Control)viewer.FindControl<Control>("MainImage")!.Parent!;
            var second = viewer.FindControl<Grid>("SecondaryImageHost")!;

            Assert.Equal(Dock.Left, DockPanel.GetDock(first));
            Assert.Equal(Dock.Right, DockPanel.GetDock(second));

            vm.GlobalSettings.IsSideBySideRightToLeft.Value = true;
            Settle(window);

            Assert.Equal(Dock.Right, DockPanel.GetDock(first));
            Assert.Equal(Dock.Left, DockPanel.GetDock(second));
        });
}
