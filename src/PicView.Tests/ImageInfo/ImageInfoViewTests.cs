using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using ImageMagick;
using PicView.Avalonia.Views.Main;
using PicView.Core.ImageDecoding;
using PicView.Core.Localization;
using PicView.Core.Models;
using PicView.Core.ViewModels;
using PicView.Tests.Metadata;

namespace PicView.Tests.ImageInfo;

/// <summary>
/// How the Image Info view follows the active tab of its Main Window.
/// </summary>
[Collection("Sequential")]
public class ImageInfoViewTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"picview-image-info-{Guid.NewGuid():N}");

    // An A1111 prompt with a typo, a negative prompt and settings
    private const string SdPayload = "a catt\nNegative prompt: blurry\nSteps: 20, Sampler: Euler, CFG scale: 7, Seed: 1, Size: 8x8";

    public ImageInfoViewTests()
    {
        SetDefaults();
        TranslationManager.LoadLanguage("en");
        Directory.CreateDirectory(_directory);
    }

    public void Dispose()
    {
        Directory.Delete(_directory, true);
        GC.SuppressFinalize(this);
    }

    [AvaloniaFact]
    public void SwitchingToATabWithAnImage_WhenOpenedFromABlankTab_ShowsThatImagesValues()
    {
        var vm = CreateViewModel();
        var imageTab = vm.WindowTabs.ActiveTab.CurrentValue;
        imageTab.Display(Image("a.jpg", 640, 480));
        var blankTab = vm.WindowTabs.CreateTab();
        Assert.Same(blankTab, vm.WindowTabs.ActiveTab.CurrentValue);

        var window = new Window { Content = new ImageInfoView { DataContext = vm } };
        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(0u, vm.Exif!.PixelWidth.Value);

            vm.WindowTabs.SelectTab(imageTab);

            Assert.True(WaitFor(() => vm.Exif.PixelWidth.Value == 640),
                "The Image Info view should show the image of the tab switched to");
        }
        finally
        {
            Close(window, vm);
        }
    }

    [AvaloniaFact]
    public void SwitchingToAnotherTabWithAnImage_ShowsThatImagesValues()
    {
        var vm = CreateViewModel();
        var firstTab = vm.WindowTabs.ActiveTab.CurrentValue;
        firstTab.Display(Image("a.jpg", 640, 480));
        var secondTab = vm.WindowTabs.CreateTab();
        secondTab.Display(Image("b.jpg", 800, 600));
        vm.WindowTabs.SelectTab(firstTab);

        var window = new Window { Content = new ImageInfoView { DataContext = vm } };
        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();
            Assert.True(WaitFor(() => vm.Exif!.PixelWidth.Value == 640),
                "The Image Info view should show the active tab's image when it opens");

            vm.WindowTabs.SelectTab(secondTab);

            Assert.True(WaitFor(() => vm.Exif!.PixelWidth.Value == 800),
                "The Image Info view should show the image of the tab switched to");
        }
        finally
        {
            Close(window, vm);
        }
    }

    [AvaloniaFact]
    public void NavigatingInABackgroundTab_KeepsTheActiveTabsValues()
    {
        var vm = CreateViewModel();
        var firstTab = vm.WindowTabs.ActiveTab.CurrentValue;
        firstTab.Display(Image("a.jpg", 640, 480));
        var secondTab = vm.WindowTabs.CreateTab();
        secondTab.Display(Image("b.jpg", 800, 600));

        var window = new Window { Content = new ImageInfoView { DataContext = vm } };
        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();
            Assert.True(WaitFor(() => vm.Exif!.PixelWidth.Value == 800),
                "The Image Info view should show the active tab's image when it opens");

            firstTab.Display(Image("c.jpg", 320, 240));

            Assert.False(WaitFor(() => vm.Exif!.PixelWidth.Value == 320, TimeSpan.FromMilliseconds(300)),
                "The Image Info view should not show an image shown in a tab that is not active");
            Assert.Equal(800u, vm.Exif!.PixelWidth.Value);
        }
        finally
        {
            Close(window, vm);
        }
    }

    [AvaloniaFact]
    public void ShowingAClipboardImage_ShowsItsDimensions()
    {
        var vm = CreateViewModel();
        var tab = vm.WindowTabs.ActiveTab.CurrentValue;
        tab.Display(Image("a.jpg", 640, 480));

        var window = new Window { Content = new ImageInfoView { DataContext = vm } };
        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();
            Assert.True(WaitFor(() => vm.Exif!.PixelWidth.Value == 640),
                "The Image Info view should show the active tab's image when it opens");

            tab.DisplaySingleImage(new ImageModel { PixelWidth = 300, PixelHeight = 200, ImageType = ImageType.Bitmap },
                SingleImageType.Clipboard, "Clipboard");

            Assert.True(WaitFor(() => vm.Exif!.PixelWidth.Value == 300),
                "The Image Info view should show the clipboard image's width");
            Assert.Equal(200u, vm.Exif!.PixelHeight.Value);
        }
        finally
        {
            Close(window, vm);
        }
    }

    [AvaloniaFact]
    public void SwitchingToABlankTab_ClearsTheValues()
    {
        var vm = CreateViewModel();
        vm.WindowTabs.ActiveTab.CurrentValue.Display(Image("a.jpg", 640, 480));

        var window = new Window { Content = new ImageInfoView { DataContext = vm } };
        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();
            Assert.True(WaitFor(() => vm.Exif!.PixelWidth.Value == 640),
                "The Image Info view should show the active tab's image when it opens");

            vm.WindowTabs.CreateTab();

            Assert.True(WaitFor(() => vm.Exif!.PixelWidth.Value == 0),
                "The Image Info view should show nothing for a blank tab");
        }
        finally
        {
            Close(window, vm);
        }
    }

    [AvaloniaFact]
    public void EnterInTheRenameBoxes_ForAClipboardImage_DoesNothing()
    {
        var vm = CreateViewModel();
        vm.WindowTabs.ActiveTab.CurrentValue.DisplaySingleImage(
            new ImageModel { PixelWidth = 300, PixelHeight = 200, ImageType = ImageType.Bitmap },
            SingleImageType.Clipboard, "Clipboard");

        var view = new ImageInfoView { DataContext = vm };
        var window = new Window { Content = view };
        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();
            Assert.True(WaitFor(() => vm.Exif!.PixelWidth.Value == 300));

            Assert.False(view.FileNameTextBox.IsEnabled);
            Assert.False(view.DirectoryNameTextBox.IsEnabled);
            Assert.False(view.FullPathTextBox.IsEnabled);

            // A key reaching the handlers anyway must not throw on the missing file
            view.FileNameTextBox.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Enter });
            view.DirectoryNameTextBox.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Enter });
            Dispatcher.UIThread.RunJobs();
        }
        finally
        {
            Close(window, vm);
        }
    }

    [AvaloniaFact]
    public void PickingAnotherOrientation_GoesBackToTheFilesOrientation()
    {
        var vm = CreateViewModel();
        var path = Path.Combine(_directory, "turned.jpg");
        using (var image = new MagickImage(MagickColors.White, 1, 1))
        {
            image.Format = MagickFormat.Jpeg;
            var profile = new ExifProfile();
            profile.SetValue(ExifTag.Orientation, (ushort)6);
            image.SetProfile(profile);
            image.Orientation = Orientation.RightTop;
            image.Write(path);
        }
        vm.WindowTabs.ActiveTab.CurrentValue.Display(new ImageModel { FileInfo = new FileInfo(path), PixelWidth = 1, PixelHeight = 1 });

        var view = new ImageInfoView { DataContext = vm };
        var window = new Window { Content = view };
        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();
            Assert.True(WaitFor(() => view.OrientationBox.SelectedIndex > 0));
            var fileIndex = view.OrientationBox.SelectedIndex;

            view.OrientationBox.SelectedIndex = fileIndex == 1 ? 2 : 1;

            Assert.True(WaitFor(() => view.OrientationBox.SelectedIndex == fileIndex),
                "A read-only dropdown should go back to the file's value");
            Assert.Equal(fileIndex, vm.Exif!.Orientation.SelectedIndex.Value);
        }
        finally
        {
            Close(window, vm);
        }
    }

    // An A1111 JPEG, whose UserComment holds the data: the Prompt box edits it, and the Comment box, which would show the
    // same tag, is hidden
    [AvaloniaFact]
    public void ImageInfoView_A1111Image_ShowsEditablePromptBox()
    {
        var vm = CreateViewModel();
        var path = SdTestFiles.WriteJpegWithUserComment(SdTestFiles.AsciiUserComment(SdPayload));
        vm.WindowTabs.ActiveTab.CurrentValue.Display(new ImageModel { FileInfo = new FileInfo(path), PixelWidth = 8, PixelHeight = 8 });

        var view = new ImageInfoView { DataContext = vm };
        var window = new Window { Content = view };
        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();
            Assert.True(WaitFor(() => view.SdPromptBox.Text == "a catt"),
                "The Prompt box should show the image's prompt");

            Assert.False(view.SdPromptBox.IsReadOnly);
            Assert.True(view.SdPromptBox.IsEffectivelyVisible);
            Assert.False(view.CommentBox.IsEffectivelyVisible);
        }
        finally
        {
            Close(window, vm);
            File.Delete(path);
        }
    }

    // Select all and Delete empties the box, which is bound to the same value as the row's visibility
    [AvaloniaFact]
    public void SdSettingsBox_ClearedWhileEditable_StaysVisible()
    {
        var vm = CreateViewModel();
        var path = SdTestFiles.WriteJpegWithUserComment(SdTestFiles.AsciiUserComment(SdPayload));
        vm.WindowTabs.ActiveTab.CurrentValue.Display(new ImageModel { FileInfo = new FileInfo(path), PixelWidth = 8, PixelHeight = 8 });

        var view = new ImageInfoView { DataContext = vm };
        var window = new Window { Content = view };
        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();
            Assert.True(WaitFor(() => view.SdSettingsBox.IsEffectivelyVisible));

            view.SdSettingsBox.Text = string.Empty;
            Dispatcher.UIThread.RunJobs();

            Assert.True(view.SdSettingsBox.IsEffectivelyVisible, "A cleared Settings box should stay visible while editable");
        }
        finally
        {
            Close(window, vm);
            File.Delete(path);
        }
    }

    [AvaloniaFact]
    public void SdExpandOverlay_EditedAndClosedOnEscape_WritesThePromptAndKeepsTheWindowOpen()
    {
        var vm = CreateViewModel();
        var path = SdTestFiles.WriteJpegWithUserComment(SdTestFiles.AsciiUserComment(SdPayload));
        vm.WindowTabs.ActiveTab.CurrentValue.Display(new ImageModel { FileInfo = new FileInfo(path), PixelWidth = 8, PixelHeight = 8 });

        var view = new ImageInfoView { DataContext = vm };
        var window = new Window { Content = view };
        var isWindowClosed = false;
        window.Closed += (_, _) => isWindowClosed = true;
        // As the window opener does: Esc that reaches the window closes it
        window.KeyDown += (_, e) =>
        {
            if (e.Key is Key.Escape)
            {
                window.Close();
            }
        };
        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();
            Assert.True(WaitFor(() => view.SdPromptBox.Text == "a catt"));

            view.SdPromptExpandButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Assert.True(view.SdExpandOverlay.IsVisible);
            Assert.False(view.SdExpandText.IsReadOnly);
            Assert.Equal("a catt", view.SdExpandText.Text);

            view.SdExpandText.Text = "a cat\non a mat";
            view.SdExpandText.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Escape });

            Assert.False(view.SdExpandOverlay.IsVisible);
            Assert.False(isWindowClosed, "Esc should close the overlay, not the window");
            Assert.True(WaitFor(() => vm.Exif!.SdPrompt.Value == "a cat\non a mat"),
                "Closing the overlay should write its text and show what the file holds");
            Assert.Equal("blurry", vm.Exif!.SdNegativePrompt.Value);
        }
        finally
        {
            Close(window, vm);
            File.Delete(path);
        }
    }

    // PicView's own write, or any other change on disk, fires a File change for the same image: the edit is kept
    [AvaloniaFact]
    public void SdExpandOverlay_ShownFileRewritten_StaysOpenWithTheEdit()
    {
        var vm = CreateViewModel();
        var path = SdTestFiles.WriteJpegWithUserComment(SdTestFiles.AsciiUserComment(SdPayload));
        var tab = vm.WindowTabs.ActiveTab.CurrentValue;
        tab.Display(new ImageModel { FileInfo = new FileInfo(path), PixelWidth = 8, PixelHeight = 8 });

        var view = new ImageInfoView { DataContext = vm };
        var window = new Window { Content = view };
        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();
            Assert.True(WaitFor(() => view.SdPromptBox.Text == "a catt"));
            view.SdPromptExpandButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            view.SdExpandText.Text = "a cat, typing";

            Assert.True(tab.UpdateShownFile(path, new FileInfo(path)));

            Assert.False(WaitFor(() => !view.SdExpandOverlay.IsVisible, TimeSpan.FromMilliseconds(500)),
                "A change of the shown image's file should keep the overlay open");
            Assert.Equal("a cat, typing", view.SdExpandText.Text);
        }
        finally
        {
            Close(window, vm);
            File.Delete(path);
        }
    }

    [AvaloniaFact]
    public void SdExpandOverlay_AnotherImageShown_ClosesWithoutWriting()
    {
        var vm = CreateViewModel();
        var path = SdTestFiles.WriteJpegWithUserComment(SdTestFiles.AsciiUserComment(SdPayload));
        var tab = vm.WindowTabs.ActiveTab.CurrentValue;
        tab.Display(new ImageModel { FileInfo = new FileInfo(path), PixelWidth = 8, PixelHeight = 8 });
        var bytes = File.ReadAllBytes(path);

        var view = new ImageInfoView { DataContext = vm };
        var window = new Window { Content = view };
        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();
            Assert.True(WaitFor(() => view.SdPromptBox.Text == "a catt"));
            view.SdPromptExpandButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            view.SdExpandText.Text = "a cat";

            tab.Display(Image("other.jpg", 640, 480));

            Assert.True(WaitFor(() => !view.SdExpandOverlay.IsVisible),
                "Showing another image should close the overlay");
            Assert.True(WaitFor(() => vm.Exif!.PixelWidth.Value == 640));
            Assert.Equal(bytes, File.ReadAllBytes(path));
        }
        finally
        {
            Close(window, vm);
            File.Delete(path);
        }
    }

    // Set up as the window opener does it, with the Exif and Info Window view models the view reads
    private static MainWindowViewModel CreateViewModel()
    {
        var core = new CoreViewModel(null, _ => new ImageModel());
        var vm = new MainWindowViewModel(core, core.Translation, core.GlobalSettings!, core.GallerySettings)
        {
            Exif = new ExifViewModel()
        };
        vm.InfoWindow = new ImageInfoWindowViewModel(vm);
        return vm;
    }

    private static void Close(Window window, MainWindowViewModel vm)
    {
        window.Close();
        vm.Exif?.Dispose();
        vm.InfoWindow?.Dispose();
    }

    // A real file, as the view reads its size
    private ImageModel Image(string name, uint width, uint height)
    {
        var path = Path.Combine(_directory, name);
        using (var image = new MagickImage(MagickColors.White, 1, 1))
        {
            image.Format = MagickFormat.Jpeg;
            image.Write(path);
        }
        return new ImageModel { FileInfo = new FileInfo(path), PixelWidth = width, PixelHeight = height };
    }

    // The values are read off the UI thread and set back on it, so the dispatcher runs while waiting
    private static bool WaitFor(Func<bool> condition, TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(5));
        while (DateTime.UtcNow < deadline)
        {
            Dispatcher.UIThread.RunJobs();
            if (condition())
            {
                return true;
            }
            Thread.Sleep(10);
        }
        return condition();
    }
}
