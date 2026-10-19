using Avalonia.Controls;
using PicView.Avalonia.WindowBehavior;
using PicView.Core.Sizing;

namespace PicView.Tests.WindowBehavior;

public class WindowSizingPlanTests
{
    private static readonly ScreenSize Screen = new()
    {
        Width = 1920,
        Height = 1080,
        WorkingAreaWidth = 1920,
        WorkingAreaHeight = 1040,
        Scaling = 1
    };

    private static readonly Fit Fit = new(WindowWidth: 1000, WindowHeight: 558, Width: 1000, Height: 500,
        ScrollViewerWidth: double.NaN, ScrollViewerHeight: double.NaN, Scale: 1);

    private static WindowSizingPlan Plan(WindowMode mode, bool autoFit = false, bool isShown = true) =>
        WindowSizingPlan.For(mode, autoFit, isShown, Fit, Screen);

    [Fact]
    public void For_NormalWithoutAutoFit_KeepsTheUsersSizeWithNoLimit()
    {
        var plan = Plan(WindowMode.Normal);

        Assert.Equal(SizeToContent.Manual, plan.SizeToContent);
        Assert.True(double.IsNaN(plan.MaxWidth));
        Assert.True(double.IsNaN(plan.MaxHeight));
        Assert.Null(plan.Width);
        Assert.Null(plan.Height);
        Assert.False(plan.SettleToContentAfterRender);
    }

    [Theory]
    [InlineData(WindowMode.Fullscreen, 1920, 1080)]
    [InlineData(WindowMode.Maximized, 1920, 1040)]
    public void For_FullscreenOrMaximized_IsLimitedToScreenOrWorkArea(WindowMode mode, double maxWidth,
        double maxHeight)
    {
        var plan = Plan(mode);

        Assert.Equal(SizeToContent.Manual, plan.SizeToContent);
        Assert.Equal(maxWidth, plan.MaxWidth);
        Assert.Equal(maxHeight, plan.MaxHeight);
        Assert.False(plan.SettleToContentAfterRender);
    }

    [Theory]
    [InlineData(WindowMode.Fullscreen)]
    [InlineData(WindowMode.Maximized)]
    public void For_FullscreenOrMaximized_ClearsAnExplicitSize(WindowMode mode)
    {
        var plan = Plan(mode, autoFit: true);

        Assert.True(plan.Width is double.NaN);
        Assert.True(plan.Height is double.NaN);
    }

    [Fact]
    public void For_AutoFitShownWindow_GrowsToContentUpToTheFit()
    {
        var plan = Plan(WindowMode.Normal, autoFit: true);

        Assert.Equal(SizeToContent.WidthAndHeight, plan.SizeToContent);
        Assert.Equal(1000, plan.MaxWidth);
        Assert.Equal(558, plan.MaxHeight);
        Assert.Null(plan.Width);
        Assert.Null(plan.Height);
        Assert.True(plan.SettleToContentAfterRender);
    }

    [Fact]
    public void For_AutoFitWindowNotShownYet_SetsTheFitAsExplicitSize()
    {
        var plan = Plan(WindowMode.Normal, autoFit: true, isShown: false);

        Assert.Equal(SizeToContent.Manual, plan.SizeToContent);
        Assert.Equal(1000, plan.MaxWidth);
        Assert.Equal(558, plan.MaxHeight);
        Assert.Equal(1000, plan.Width);
        Assert.Equal(558, plan.Height);
        Assert.True(plan.SettleToContentAfterRender);
    }
}
