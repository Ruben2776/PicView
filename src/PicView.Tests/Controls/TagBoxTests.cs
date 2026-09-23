using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Threading;
using PicView.Avalonia;
using PicView.Avalonia.CustomControls;

namespace PicView.Tests.Controls;

[Collection("Sequential")]
public class TagBoxTests
{
    public TagBoxTests()
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

    [Fact]
    public void TagBox_WithTags_ShowsTagsAndRemovesTag()
    {
        var tagBox = new TagBox
        {
            Tags = new ObservableCollection<string> { "Ctrl", "Shift", "P" }
        };
        var window = new Window { Content = tagBox };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        // Tags should have 3 items
        Assert.Equal(3, tagBox.Tags.Count);

        // Remove a tag
        tagBox.RemoveTag("Shift");
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(2, tagBox.Tags.Count);
        Assert.DoesNotContain("Shift", tagBox.Tags);
    }

    [Fact]
    public void TagBox_EmptyPseudoClass_UpdatesCorrectly()
    {
        var tagBox = new TagBox();
        var window = new Window { Content = tagBox };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        Assert.Contains(TagBox.Empty, tagBox.Classes);

        tagBox.Tags.Add("Alt");
        Dispatcher.UIThread.RunJobs();

        Assert.DoesNotContain(TagBox.Empty, tagBox.Classes);

        tagBox.RemoveTag("Alt");
        Dispatcher.UIThread.RunJobs();

        Assert.Contains(TagBox.Empty, tagBox.Classes);
    }
}
