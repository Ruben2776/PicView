using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Headless;
using Avalonia.Layout;
using Avalonia.Threading;
using PicView.Avalonia;
using PicView.Avalonia.CustomControls;

namespace PicView.Tests.Gallery;

[Collection("Sequential")]
public class NavigateAbleItemsViewerTests
{
    public NavigateAbleItemsViewerTests()
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
    public void CurrentItemIndex_DoesNotModify_SelectedItemIndex()
    {
        var viewer = new NavigateAbleItemsViewer();
        Assert.Equal(-1, viewer.SelectedItemIndex);
        Assert.Equal(-1, viewer.CurrentItemIndex);

        viewer.CurrentItemIndex = 2;

        Assert.Equal(2, viewer.CurrentItemIndex);
        Assert.Equal(-1, viewer.SelectedItemIndex);
    }

    [Fact]
    public void SelectedItemIndex_DoesNotModify_CurrentItemIndex()
    {
        var viewer = new NavigateAbleItemsViewer();
        viewer.CurrentItemIndex = 1;
        Assert.Equal(1, viewer.CurrentItemIndex);

        viewer.SelectedItemIndex = 4;

        Assert.Equal(1, viewer.CurrentItemIndex);
        Assert.Equal(4, viewer.SelectedItemIndex);
    }

    [Fact]
    public void Viewer_EnforcesSingleCurrentAndSingleSelected()
    {
        RunWithDispatcher(Dispatcher.UIThread.InvokeAsync(() =>
        {
            var gallery = new VirtualizingGallery
            {
                ItemHeight = 50,
                Orientation = Orientation.Horizontal
            };

            var viewer = new NavigateAbleItemsViewer
            {
                ItemsPanel = new FuncTemplate<Panel?>(() => gallery),
                ItemTemplate = new FuncDataTemplate<string>((_, _) => new NavigateAbleItem()),
                ItemsSource = new[] { "A", "B", "C", "D", "E" }
            };

            var window = new Window { Width = 1000, Height = 500, Content = viewer };
            window.Show();

            viewer.CurrentItemIndex = 1;
            viewer.SelectedItemIndex = 3;

            for (int i = 0; i < 5; i++)
            {
                var presenter = viewer.ContainerFromIndex(i) as ContentPresenter;
                Assert.NotNull(presenter);
                var navItem = presenter.Child as NavigateAbleItem;
                Assert.NotNull(navItem);

                var isCurrent = navItem.Classes.Contains(":currentItem");
                var isSelected = navItem.Classes.Contains(":selectedItem");

                Assert.Equal(i == 1, isCurrent);
                Assert.Equal(i == 3, isSelected);
            }

            // Change current to 4
            viewer.CurrentItemIndex = 4;

            for (int i = 0; i < 5; i++)
            {
                var presenter = (ContentPresenter)viewer.ContainerFromIndex(i)!;
                var navItem = (NavigateAbleItem)presenter.Child!;

                Assert.Equal(i == 4, navItem.Classes.Contains(":currentItem"));
                Assert.Equal(i == 3, navItem.Classes.Contains(":selectedItem"));
            }

            // Clear selection
            viewer.SelectedItemIndex = -1;

            for (int i = 0; i < 5; i++)
            {
                var presenter = (ContentPresenter)viewer.ContainerFromIndex(i)!;
                var navItem = (NavigateAbleItem)presenter.Child!;

                Assert.Equal(i == 4, navItem.Classes.Contains(":currentItem"));
                Assert.DoesNotContain(":selectedItem", navItem.Classes);
            }
        }));
    }

    private static void RunWithDispatcher(DispatcherOperation operation)
    {
        while (operation.Status is not DispatcherOperationStatus.Completed and not DispatcherOperationStatus.Aborted)
        {
            Dispatcher.UIThread.RunJobs();
            Thread.Sleep(10);
        }
        operation.GetAwaiter().GetResult();
    }
}
