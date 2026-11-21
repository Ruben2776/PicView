using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Views.Gallery;
using PicView.Core.Gallery;
using PicView.Core.Localization;
using PicView.Core.Models;
using PicView.Core.Navigation;
using PicView.Core.Thumbnailing;
using PicView.Core.ViewModels;
using PicView.Tests.Config;
using PicView.Tests.MotionPhoto;
using PicView.Tests.Navigation;
using PicView.Tests.Thumbnailing;

namespace PicView.Tests.Gallery;

/// <summary>
/// Pins when a gallery item detects its motion photo badge without loading its thumbnail: when it is shown with a
/// thumbnail it has already, and when the badges are turned on while it is shown. Never for an item no longer shown.
/// </summary>
[Collection("Sequential")]
public sealed class GalleryMotionPhotoDetectionTests : IDisposable
{
    private readonly string _directory = MotionPhotoFixtures.CreateTempDirectory();
    private readonly SharedImageCache _cache = new(static f => new ImageModel { FileInfo = f });

    public GalleryMotionPhotoDetectionTests()
    {
        TranslationManager.Init();
        SetDefaults();
    }

    public void Dispose() => MotionPhotoFixtures.DeleteDirectory(_directory);

    [AvaloniaFact]
    public void LoadImage_ItemAlreadyShowingAThumbnail_DetectsTheBadge()
    {
        Settings.Gallery.ShowMotionPhotoBadges = true;
        var vm = LoadedMotionPhoto();
        vm.Image.Value = Pixel();
        var item = new GalleryItem { DataContext = vm };

        _ = item.LoadImage(vm, isTile: false);

        WaitUntil(() => vm.IsMotionPhotoChecked);
        Assert.True(vm.IsMotionPhoto.Value);
    }

    [AvaloniaFact]
    public void DetectMotionPhoto_ItemUnloaded_DetectsNothing()
    {
        Settings.Gallery.ShowMotionPhotoBadges = true;
        var vm = LoadedMotionPhoto();
        var item = new GalleryItem { DataContext = vm };
        item.UnloadImage();

        item.DetectMotionPhoto();
        Thread.Sleep(100);

        Assert.False(vm.IsMotionPhotoChecked);
        Assert.False(vm.IsMotionPhoto.Value);
    }

    [AvaloniaFact]
    public void BadgesTurnedOn_ItemsShown_DetectsTheirBadges()
    {
        var gallerySettings = new GallerySharedSettingsViewModel();
        // As the app starts it, so switching the badges writes the setting detection reads
        gallerySettings.Start();
        Assert.False(gallerySettings.ShowMotionPhotoBadges.Value);
        var vm = LoadedMotionPhoto();
        var viewer = new NavigateAbleItemsViewer
        {
            ItemsPanel = new FuncTemplate<Panel?>(() => new VirtualizingGallery
                { ItemHeight = 50, Orientation = Orientation.Horizontal }),
            ItemTemplate = new FuncDataTemplate<GalleryItemViewModel>((_, _) => new GalleryItem()),
            ItemsSource = new[] { vm }
        };
        var window = new Window
        {
            Width = 800,
            Height = 200,
            DataContext = new MainWindowViewModel(null!, new TranslationViewModel(), StartedSettings.New(),
                gallerySettings),
            Content = viewer
        };
        window.Show();
        try
        {
            // Shown, and its thumbnail made, while the badges were hidden
            WaitUntil(() => vm.Image.Value is not null);
            Assert.False(vm.IsMotionPhotoChecked);

            gallerySettings.ShowMotionPhotoBadges.Value = true;

            WaitUntil(() => vm.IsMotionPhotoChecked);
            Assert.True(vm.IsMotionPhoto.Value);
        }
        finally
        {
            window.Close();
        }
    }

    /// <summary>
    /// The item of a loaded gallery of one Google-style motion photo, whose thumbnail the fake decoder makes.
    /// </summary>
    private GalleryItemViewModel LoadedMotionPhoto()
    {
        var video = MotionPhotoFixtures.BuildMp4Head(64);
        var file = MotionPhotoFixtures.CreateEmbeddedMotionPhoto(_directory, "a.jpg", video,
            MotionPhotoFixtures.NewStandardXmp(video.Length));
        var tab = new TabViewModel(null!,
            TestTabServices.For(_cache, new Thumbnails(new FakeThumbnailDecoder(), _cache)))
        {
            Model = new ImageModel { FileInfo = new FileInfo(Path.Combine(_directory, "shown.jpg")) }
        };
        tab.Initialize();
        tab.Gallery.Switcher.Start(true);
        tab.ImageIterator.Initialize([file]);
        WaitUntil(() => tab.Gallery.LoadingState is GalleryLoadingState.Loaded && tab.Gallery.GalleryItems.Count is 1);
        return tab.Gallery.GalleryItems[0];
    }

    private static WriteableBitmap Pixel() =>
        new(new PixelSize(1, 1), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Premul);

    private static void WaitUntil(Func<bool> condition)
    {
        var deadline = Environment.TickCount64 + 5000;
        while (!condition())
        {
            Assert.True(Environment.TickCount64 < deadline, "Timed out");
            Dispatcher.UIThread.RunJobs();
            Thread.Sleep(10);
        }
    }
}
