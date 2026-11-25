using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;
using PicView.Avalonia.CustomControls;

namespace PicView.Tests.Controls;

[Collection("Sequential")]
public class TextIconButtonTests
{
    public TextIconButtonTests() => SetDefaults();

    private static TextIconButton Show(string? text)
    {
        var button = new TextIconButton { Text = text };
        var window = new Window { Content = button, Width = 400, Height = 200 };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return button;
    }

    [AvaloniaFact]
    public void MeasureTextWidth_TextHiddenByMaxWidth_IsTheWidthItTakesUnclipped()
    {
        var button = Show("Difference");
        var text = button.GetVisualDescendants().OfType<TextBlock>().Single();
        var expected = text.Bounds.Width;
        Assert.True(expected > 0);

        button.TextMaxWidth = 0;
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(0, text.Bounds.Width, precision: 1);
        Assert.Equal(expected, button.MeasureTextWidth(), precision: 1);
    }

    [AvaloniaFact]
    public void MeasureTextWidth_NoText_IsZero()
    {
        var button = Show(null);

        Assert.Equal(0, button.MeasureTextWidth());
    }
}
