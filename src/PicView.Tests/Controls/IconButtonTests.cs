using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Media;
using Avalonia.Threading;
using PicView.Avalonia;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.UI;

namespace PicView.Tests.Controls;

[Collection("Sequential")]
public class IconButtonTests
{
    public IconButtonTests()
    {
        try
        {
            AppBuilder.Configure<App>()
                .UseHeadless(new AvaloniaHeadlessPlatformOptions())
                .SetupWithoutStarting();
        }
        catch (InvalidOperationException)
        {
        }

        SetDefaults();
    }

    private static DrawingImage CreateTestIcon(IBrush initialBrush)
    {
        var geomDrawing = new GeometryDrawing
        {
            Geometry = StreamGeometry.Parse("M 0 0 L 10 10"),
            Pen = new Pen
            {
                Brush = initialBrush,
                Thickness = 2
            }
        };

        var group = new DrawingGroup();
        group.Children.Add(geomDrawing);

        return new DrawingImage { Drawing = group };
    }

    [Fact]
    public void IconButton_WithSharedIcon_CreatesPrivateLocalIconCopies()
    {
        var initialBrush = Brushes.Red;
        var sharedIcon = CreateTestIcon(initialBrush);

        var button1 = new IconButton { Icon = sharedIcon };
        var button2 = new IconButton { Icon = sharedIcon };

        Assert.NotNull(button1.LocalIconCopy);
        Assert.NotNull(button2.LocalIconCopy);

        // Clones must not be the original instance
        Assert.NotSame(sharedIcon, button1.LocalIconCopy);
        Assert.NotSame(sharedIcon, button2.LocalIconCopy);

        // Clones must be independent of each other
        Assert.NotSame(button1.LocalIconCopy, button2.LocalIconCopy);

        // Internal DrawingGroups and Pens must be distinct
        var group1 = Assert.IsType<DrawingGroup>(button1.LocalIconCopy.Drawing);
        var group2 = Assert.IsType<DrawingGroup>(button2.LocalIconCopy.Drawing);
        Assert.NotSame(group1, group2);

        var geom1 = Assert.IsType<GeometryDrawing>(group1.Children[0]);
        var geom2 = Assert.IsType<GeometryDrawing>(group2.Children[0]);
        Assert.NotNull(geom1.Pen);
        Assert.NotNull(geom2.Pen);
        Assert.NotSame(geom1.Pen, geom2.Pen);
    }

    [Fact]
    public void IconButton_PointerEntered_DoesNotMutateSharedIconOrOtherButton()
    {
        var initialBrush = Brushes.Red;
        var sharedIcon = CreateTestIcon(initialBrush);

        var button1 = new IconButton { Icon = sharedIcon };
        var button2 = new IconButton { Icon = sharedIcon };

        var panel = new StackPanel();
        panel.Children.Add(button1);
        panel.Children.Add(button2);

        var window = new Window { Content = panel };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        // Get pen references
        var sharedPen = ((GeometryDrawing)((DrawingGroup)sharedIcon.Drawing!).Children[0]).Pen!;
        var button2Pen = ((GeometryDrawing)((DrawingGroup)button2.LocalIconCopy!.Drawing!).Children[0]).Pen!;
        var button1Pen = ((GeometryDrawing)((DrawingGroup)button1.LocalIconCopy!.Drawing!).Children[0]).Pen!;

        Assert.Equal(initialBrush, sharedPen.Brush);

        // Hover over button1
        button1.TriggerPointerEntered();
        Dispatcher.UIThread.RunJobs();

        // sharedIcon must remain UNCHANGED
        Assert.Equal(initialBrush, sharedPen.Brush);

        var secondaryColor = UIHelper.GetColor("SecondaryTextColor");

        // button2 must remain UNCHANGED
        var b2Brush = Assert.IsAssignableFrom<ISolidColorBrush>(button2Pen.Brush);
        Assert.NotEqual(secondaryColor, b2Brush.Color);

        // button1's private copy MUST have been updated to secondary text color
        var b1Brush = Assert.IsAssignableFrom<ISolidColorBrush>(button1Pen.Brush);
        Assert.Equal(secondaryColor, b1Brush.Color);

        // Pointer exit button1
        button1.TriggerPointerExited();
        Dispatcher.UIThread.RunJobs();

        // sharedIcon must still be untouched
        Assert.Equal(initialBrush, sharedPen.Brush);
    }

    [Fact]
    public void TextIconButton_WithSharedIcon_CreatesPrivateLocalIconCopies()
    {
        var initialBrush = Brushes.Red;
        var sharedIcon = CreateTestIcon(initialBrush);

        var button1 = new TextIconButton { Icon = sharedIcon };
        var button2 = new TextIconButton { Icon = sharedIcon };

        Assert.NotNull(button1.LocalIconCopy);
        Assert.NotNull(button2.LocalIconCopy);

        Assert.NotSame(sharedIcon, button1.LocalIconCopy);
        Assert.NotSame(sharedIcon, button2.LocalIconCopy);
        Assert.NotSame(button1.LocalIconCopy, button2.LocalIconCopy);
    }

    [Fact]
    public void IconButton_WithPathData_DisplaysCorrectly()
    {
        var geometry = StreamGeometry.Parse("M 0 0 L 10 10");
        var button = new IconButton
        {
            Data = geometry,
            IconWidth = 16,
            IconHeight = 16
        };

        var window = new Window { Content = button };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(geometry, button.Data);
        Assert.Equal(16, button.IconWidth);
        Assert.Equal(16, button.IconHeight);
    }
}
