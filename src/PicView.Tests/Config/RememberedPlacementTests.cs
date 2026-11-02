using PicView.Core.Config;

namespace PicView.Tests.Config;

public class RememberedPlacementTests
{
    private static readonly WindowLimits Resizable = new(true, 100, 2000, 100, 2000);

    private sealed class Properties : IWindowProperties
    {
        public int? Top { get; set; }
        public int? Left { get; set; }
        public double? Width { get; set; }
        public double? Height { get; set; }
        public bool Maximized { get; set; }
    }

    [Fact]
    public void Restore_Maximized_MaximizesAndAppliesNothingElse()
    {
        var properties = new Properties { Maximized = true, Width = 500, Height = 400, Left = 10, Top = 20 };

        var placement = RememberedPlacement.Restore(properties, Resizable);

        Assert.True(placement.Maximize);
        Assert.Null(placement.Size);
        Assert.Null(placement.Position);
    }

    [Fact]
    public void Restore_SizeInsideLimits_AppliesIt()
    {
        var properties = new Properties { Width = 500, Height = 400 };

        var placement = RememberedPlacement.Restore(properties, Resizable);

        Assert.Equal((500d, 400d), placement.Size);
    }

    [Theory]
    [InlineData(100d, 400d)] // at the minimum width
    [InlineData(2000d, 400d)] // at the maximum width
    [InlineData(500d, 100d)] // at the minimum height
    [InlineData(500d, 2000d)] // at the maximum height
    [InlineData(double.NaN, 400d)]
    [InlineData(double.PositiveInfinity, 400d)]
    public void Restore_SizeOnOrOutsideLimits_KeepsTheWindowSize(double width, double height)
    {
        var properties = new Properties { Width = width, Height = height };

        var placement = RememberedPlacement.Restore(properties, Resizable);

        Assert.Null(placement.Size);
    }

    [Fact]
    public void Restore_OneDimensionMissing_KeepsTheWindowSize()
    {
        var properties = new Properties { Width = 500, Height = null };

        var placement = RememberedPlacement.Restore(properties, Resizable);

        Assert.Null(placement.Size);
    }

    [Fact]
    public void Restore_WindowCannotResize_IgnoresTheSize()
    {
        var properties = new Properties { Width = 500, Height = 400 };
        var fixedSize = new WindowLimits(false, 100, 2000, 100, 2000);

        var placement = RememberedPlacement.Restore(properties, fixedSize);

        Assert.Null(placement.Size);
    }

    [Fact]
    public void Restore_PositionRemembered_AppliesIt()
    {
        var properties = new Properties { Left = 10, Top = 20 };

        var placement = RememberedPlacement.Restore(properties, Resizable);

        Assert.Equal((10, 20), placement.Position);
    }

    [Fact]
    public void Restore_NegativePosition_ClampsToZero()
    {
        var properties = new Properties { Left = -5, Top = -300 };

        var placement = RememberedPlacement.Restore(properties, Resizable);

        Assert.Equal((0, 0), placement.Position);
    }

    [Fact]
    public void Restore_OnlyOneOfTopAndLeft_Centers()
    {
        var properties = new Properties { Left = 10, Top = null };

        var placement = RememberedPlacement.Restore(properties, Resizable);

        Assert.Null(placement.Position);
    }

    [Fact]
    public void Restore_NothingRemembered_Centers()
    {
        var placement = RememberedPlacement.Restore(new Properties(), Resizable);

        Assert.False(placement.Maximize);
        Assert.Null(placement.Size);
        Assert.Null(placement.Position);
    }

    [Fact]
    public void Remember_Normal_WritesSizeAndPosition()
    {
        var properties = new Properties();

        RememberedPlacement.Remember(properties, new WindowBounds(640, 480, 30, 40, false), canResize: true);

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

        RememberedPlacement.Remember(properties, new WindowBounds(1920, 1080, 0, 0, true), canResize: true);

        Assert.Equal(500d, properties.Width);
        Assert.Equal(400d, properties.Height);
        Assert.True(properties.Maximized);
        Assert.Equal(0, properties.Left);
        Assert.Equal(0, properties.Top);
    }

    [Fact]
    public void Remember_CannotResize_WritesPositionOnly()
    {
        var properties = new Properties();

        RememberedPlacement.Remember(properties, new WindowBounds(300, 200, 30, 40, false), canResize: false);

        Assert.Null(properties.Width);
        Assert.Null(properties.Height);
        Assert.Equal(30, properties.Left);
        Assert.Equal(40, properties.Top);
    }

    [Fact]
    public void Remember_NonFiniteSize_LeavesTheOldSize()
    {
        var properties = new Properties { Width = 500, Height = 400 };

        RememberedPlacement.Remember(properties, new WindowBounds(double.NaN, 0, 1, 2, false), canResize: true);

        Assert.Equal(500d, properties.Width);
        Assert.Equal(400d, properties.Height);
    }

    [Fact]
    public void Remember_NonFiniteSizeAlreadyInTheProperties_ClearsIt()
    {
        var properties = new Properties { Width = double.NaN, Height = 400 };

        RememberedPlacement.Remember(properties, new WindowBounds(double.NaN, double.NaN, 1, 2, false),
            canResize: true);

        Assert.Null(properties.Width);
        Assert.Equal(400d, properties.Height);
    }
}
