using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Headless;
using Avalonia.Threading;
using Avalonia.VisualTree;
using PicView.Avalonia;
using PicView.Core.ViewModels;

namespace PicView.Tests.Controls;

[Collection("Sequential")]
public class ComboBoxTests
{
    static ComboBoxTests()
    {
        if (Application.Current == null)
        {
            AppBuilder.Configure<App>()
                .UseHeadless(new AvaloniaHeadlessPlatformOptions())
                .SetupWithoutStarting();
        }
    }

    [Fact]
    public void ComboBox_WithDisplayMemberBinding_RendersDisplayNameInSelectionBox()
    {
        var items = new List<LanguageItem>
        {
            new("da", "Danish"),
            new("en", "English")
        };

        var comboBox = new ComboBox
        {
            ItemsSource = items,
            DisplayMemberBinding = new Binding(nameof(LanguageItem.DisplayName)),
            SelectedIndex = 0
        };

        var window = new Window { Content = comboBox };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        // Find the TextBlock inside the ComboBox's template representing the selection box
        var textBlocks = comboBox.GetVisualDescendants()
            .OfType<TextBlock>()
            .Where(t => t.Name != "PlaceholderTextBlock")
            .ToList();

        Assert.NotEmpty(textBlocks);
        Assert.Equal("Danish", textBlocks[0].Text);
    }
}
