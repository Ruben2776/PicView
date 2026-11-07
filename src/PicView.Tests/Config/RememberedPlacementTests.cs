using PicView.Core.Config;

namespace PicView.Tests.Config;

public class RememberedPlacementTests
{
    private static readonly WindowLimits Resizable = new(true, 100, 2000, 100, 2000, double.NaN, double.NaN);

    private static readonly ScreenArea[] Primary = [new(0, 0, 1920, 1080)];

    private sealed class Properties : IWindowProperties
    {
        public int? Top { get; set; }
        public int? Left { get; set; }
        public double? Width { get; set; }
        public double? Height { get; set; }
        public bool Maximized { get; set; }
    }

    [Fact]
    public void Restore_Maximized_MaximizesAndKeepsTheSizeForUnmaximizing()
    {
        var properties = new Properties { Maximized = true, Width = 500, Height = 400, Left = 10, Top = 20 };

        var placement = RememberedPlacement.Restore(properties, Resizable, Primary);

        Assert.True(placement.Maximize);
        Assert.Equal((500d, 400d), placement.Size);
        Assert.Equal((10, 20), placement.Position);
    }

    [Fact]
    public void Restore_SizeInsideLimits_AppliesIt()
    {
        var properties = new Properties { Width = 500, Height = 400 };

        var placement = RememberedPlacement.Restore(properties, Resizable, Primary);

        Assert.Equal((500d, 400d), placement.Size);
    }

    [Theory]
    [InlineData(100d, 400d)] // at the minimum width
    [InlineData(2000d, 400d)] // at the maximum width
    [InlineData(500d, 100d)] // at the minimum height
    [InlineData(500d, 2000d)] // at the maximum height
    public void Restore_SizeAtTheLimits_AppliesIt(double width, double height)
    {
        var properties = new Properties { Width = width, Height = height };

        var placement = RememberedPlacement.Restore(properties, Resizable, Primary);

        Assert.Equal((width, height), placement.Size);
    }

    [Theory]
    [InlineData(99d, 400d)] // just below the minimum width
    [InlineData(2001d, 400d)] // just above the maximum width
    [InlineData(500d, 99d)] // just below the minimum height
    [InlineData(500d, 2001d)] // just above the maximum height
    [InlineData(double.NaN, 400d)]
    [InlineData(double.PositiveInfinity, 400d)]
    public void Restore_SizeOutsideLimits_KeepsTheWindowSize(double width, double height)
    {
        var properties = new Properties { Width = width, Height = height };

        var placement = RememberedPlacement.Restore(properties, Resizable, Primary);

        Assert.Null(placement.Size);
    }

    [Fact]
    public void Restore_OneDimensionMissing_KeepsTheWindowSize()
    {
        var properties = new Properties { Width = 500, Height = null };

        var placement = RememberedPlacement.Restore(properties, Resizable, Primary);

        Assert.Null(placement.Size);
    }

    [Fact]
    public void Restore_WindowCannotResize_IgnoresTheSize()
    {
        var properties = new Properties { Width = 500, Height = 400 };
        var fixedSize = new WindowLimits(false, 100, 2000, 100, 2000, double.NaN, double.NaN);

        var placement = RememberedPlacement.Restore(properties, fixedSize, Primary);

        Assert.Null(placement.Size);
    }

    [Fact]
    public void Restore_PositionRemembered_AppliesIt()
    {
        var properties = new Properties { Left = 10, Top = 20 };

        var placement = RememberedPlacement.Restore(properties, Resizable, Primary);

        Assert.Equal((10, 20), placement.Position);
    }

    [Fact]
    public void Restore_PositionOnASecondScreenLeftOfThePrimary_AppliesIt()
    {
        var properties = new Properties { Left = -1000, Top = 100 };
        ScreenArea[] screens = [new(0, 0, 1920, 1080), new(-1920, 0, 1920, 1080)];

        var placement = RememberedPlacement.Restore(properties, Resizable, screens);

        Assert.Equal((-1000, 100), placement.Position);
    }

    [Fact]
    public void Restore_PositionOnNoScreen_Centers()
    {
        var properties = new Properties { Left = -5000, Top = 100 };

        var placement = RememberedPlacement.Restore(properties, Resizable, Primary);

        Assert.Null(placement.Position);
    }

    [Fact]
    public void Restore_PositionPartlyOnAScreen_AppliesIt()
    {
        var properties = new Properties { Left = -100, Top = 100, Width = 500, Height = 400 };

        var placement = RememberedPlacement.Restore(properties, Resizable, Primary);

        Assert.Equal((-100, 100), placement.Position);
    }

    [Fact]
    public void Restore_NoScreensKnown_AppliesThePosition()
    {
        var properties = new Properties { Left = -5, Top = -300 };

        var placement = RememberedPlacement.Restore(properties, Resizable, []);

        Assert.Equal((-5, -300), placement.Position);
    }

    [Fact]
    public void Restore_OnlyOneOfTopAndLeft_Centers()
    {
        var properties = new Properties { Left = 10, Top = null };

        var placement = RememberedPlacement.Restore(properties, Resizable, Primary);

        Assert.Null(placement.Position);
    }

    [Fact]
    public void Restore_NothingRemembered_Centers()
    {
        var placement = RememberedPlacement.Restore(new Properties(), Resizable, Primary);

        Assert.False(placement.Maximize);
        Assert.Null(placement.Size);
        Assert.Null(placement.Position);
    }

    [Fact]
    public void Remember_Normal_WritesSizeAndPosition()
    {
        var properties = new Properties();

        RememberedPlacement.Remember(properties, new WindowBounds(640, 480, 30, 40, false, false), canResize: true);

        Assert.Equal(640d, properties.Width);
        Assert.Equal(480d, properties.Height);
        Assert.Equal(30, properties.Left);
        Assert.Equal(40, properties.Top);
        Assert.False(properties.Maximized);
    }

    [Fact]
    public void Remember_Maximized_WritesTheFlagAndPositionButNotTheSize()
    {
        var properties = new Properties { Width = 500, Height = 400 };

        RememberedPlacement.Remember(properties, new WindowBounds(1920, 1080, 0, 0, true, false), canResize: true);

        Assert.Equal(500d, properties.Width);
        Assert.Equal(400d, properties.Height);
        Assert.True(properties.Maximized);
        Assert.Equal(0, properties.Left);
        Assert.Equal(0, properties.Top);
    }

    [Theory]
    [InlineData(-32000, -32000)]
    [InlineData(1920, 0)]
    [InlineData(0, 1080)]
    [InlineData(-801, 0)]
    public void IsOnAScreen_PositionOutsideEveryScreen_IsFalse(int left, int top)
    {
        Assert.False(RememberedPlacement.IsOnAScreen(left, top, 800, 600, Primary));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1900, 1000)]
    [InlineData(-799, 0)]
    public void IsOnAScreen_WindowOverlapsAScreen_IsTrue(int left, int top)
    {
        Assert.True(RememberedPlacement.IsOnAScreen(left, top, 800, 600, Primary));
    }

    [Fact]
    public void IsOnAScreen_NoScreensKnown_IsTrue()
    {
        Assert.True(RememberedPlacement.IsOnAScreen(-32000, -32000, 800, 600, []));
    }

    [Fact]
    public void Remember_Minimized_LeavesThePosition()
    {
        var properties = new Properties { Left = 10, Top = 20 };

        RememberedPlacement.Remember(properties, new WindowBounds(640, 480, -32000, -32000, false, true),
            canResize: true);

        Assert.Equal(10, properties.Left);
        Assert.Equal(20, properties.Top);
    }

    [Fact]
    public void Remember_CannotResize_WritesPositionOnly()
    {
        var properties = new Properties();

        RememberedPlacement.Remember(properties, new WindowBounds(300, 200, 30, 40, false, false), canResize: false);

        Assert.Null(properties.Width);
        Assert.Null(properties.Height);
        Assert.Equal(30, properties.Left);
        Assert.Equal(40, properties.Top);
    }

    [Fact]
    public void Remember_NonFiniteSize_LeavesTheOldSize()
    {
        var properties = new Properties { Width = 500, Height = 400 };

        RememberedPlacement.Remember(properties, new WindowBounds(double.NaN, 0, 1, 2, false, false),
            canResize: true);

        Assert.Equal(500d, properties.Width);
        Assert.Equal(400d, properties.Height);
    }

    [Fact]
    public void Remember_NonFiniteSizeAlreadyInTheProperties_ClearsIt()
    {
        var properties = new Properties { Width = double.NaN, Height = 400 };

        RememberedPlacement.Remember(properties, new WindowBounds(double.NaN, double.NaN, 1, 2, false, false),
            canResize: true);

        Assert.Null(properties.Width);
        Assert.Equal(400d, properties.Height);
    }
}
