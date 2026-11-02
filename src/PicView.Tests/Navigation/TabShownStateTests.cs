using PicView.Core.ImageDecoding;
using PicView.Core.Localization;
using PicView.Core.Models;
using PicView.Core.Navigation.Tiff;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Tests.Navigation;

/// <summary>
/// The tab is the one writer of what it shows: it publishes one Shown change, and the narrow methods beside Display
/// change the shown state without recording or firing it.
/// </summary>
[Collection("Sequential")]
public class TabShownStateTests
{
    private readonly TabViewModel _tab;

    public TabShownStateTests()
    {
        ObservableSystem.DefaultFrameProvider = new MockFrameProvider();
        SetDefaults();
        _tab = new TabViewModel(null!);
    }

    [Fact]
    public void Display_FiresShownImageOnce_AfterBindablesAndRecording()
    {
        var model = Model("a.jpg");
        var changes = new List<ShownChange>();
        object? imageWhenFired = null;
        var entriesWhenFired = -1;
        using var _ = _tab.Shown.Subscribe(change =>
        {
            changes.Add(change);
            imageWhenFired = _tab.Image.Value;
            entriesWhenFired = _tab.ViewHistory.Entries.Count();
        });

        _tab.Display(model);

        Assert.Equal([ShownChange.Image], changes);
        Assert.Same(model.Image, imageWhenFired);
        Assert.Equal(1, entriesWhenFired);
    }

    [Fact]
    public void DisplaySingleImage_FiresShownImageOnce()
    {
        var changes = new List<ShownChange>();
        using var _ = _tab.Shown.Subscribe(changes.Add);

        _tab.DisplaySingleImage(new ImageModel { Image = new object() }, SingleImageType.Clipboard);

        Assert.Equal([ShownChange.Image], changes);
    }

    [Fact]
    public void ShowPage_OnMultiPageTiff_FiresShownImageOnce()
    {
        var pages = new[] { new object(), new object() };
        var model = Model("a.tif");
        model.Image = pages[0];
        model.TiffNavigation = new TiffNavigationInfo { PageCount = 2, CurrentPage = 0, Pages = pages };
        _tab.Display(model);
        var changes = new List<ShownChange>();
        using var _ = _tab.Shown.Subscribe(changes.Add);

        _tab.ShowPage(1);

        Assert.Equal([ShownChange.Image], changes);
        Assert.Same(pages[1], _tab.Image.Value);
    }

    [Fact]
    public void ShowPage_WithoutTiff_FiresNothing()
    {
        _tab.Display(Model("a.jpg"));
        var changes = new List<ShownChange>();
        using var _ = _tab.Shown.Subscribe(changes.Add);

        _tab.ShowPage(1);

        Assert.Empty(changes);
    }

    [Fact]
    public void ShowStandIn_SetsImageOnly()
    {
        var model = Model("a.jpg");
        _tab.Display(model);
        var standIn = new object();
        var fired = false;
        using var _ = _tab.Shown.Subscribe(_ => fired = true);

        _tab.ShowStandIn(standIn);

        Assert.Same(standIn, _tab.Image.Value);
        Assert.Same(model, _tab.Model);
        Assert.Same(model.FileInfo, _tab.FileInfo.Value);
        Assert.Single(_tab.ViewHistory.Entries);
        Assert.False(fired);
    }

    [Fact]
    public void ShowLoading_SetsThumbnailLoadingTitleAndClearsSecondaryImage()
    {
        TranslationManager.Init();
        var secondary = Model("b.jpg");
        _tab.Display(Model("a.jpg"), secondary);
        var thumb = new object();
        var fired = false;
        using var _ = _tab.Shown.Subscribe(_ => fired = true);

        _tab.ShowLoading(thumb);

        Assert.Same(thumb, _tab.Image.Value);
        Assert.Null(_tab.SecondaryImage.Value);
        Assert.Equal(TranslationManager.Translation.Loading, _tab.Title!.Value);
        Assert.Same(secondary, _tab.SecondaryModel);
        Assert.Single(_tab.ViewHistory.Entries);
        Assert.False(fired);
    }

    [Fact]
    public void ShowLoading_OnTabThatShowedNothing_DoesNotThrow()
    {
        TranslationManager.Init();

        _tab.ShowLoading(null);

        Assert.Null(_tab.Image.Value);
        Assert.Equal(TranslationManager.Translation.Loading, _tab.Title!.Value);
    }

    [Fact]
    public void UpdateShownFile_PrimaryPath_FollowsFileAndSize_FiresFile()
    {
        var model = Model("a.jpg");
        _tab.Display(model);
        var changes = new List<ShownChange>();
        using var _ = _tab.Shown.Subscribe(changes.Add);
        var renamed = new FileInfo("b.jpg");

        var matched = _tab.UpdateShownFile(model.FileInfo!.FullName, renamed, 640, 480);

        Assert.True(matched);
        Assert.Same(renamed, _tab.Model.FileInfo);
        Assert.Same(renamed, _tab.FileInfo.Value);
        Assert.Equal(640u, _tab.Model.PixelWidth);
        Assert.Equal(480u, _tab.Model.PixelHeight);
        Assert.Same(model.Image, _tab.Image.Value);
        Assert.Equal([ShownChange.File], changes);
        Assert.Single(_tab.ViewHistory.Entries);
    }

    [Fact]
    public void UpdateShownFile_SecondaryPath_FollowsSecondary()
    {
        var primary = Model("a.jpg");
        var secondary = Model("b.jpg");
        _tab.Display(primary, secondary);
        var changes = new List<ShownChange>();
        using var _ = _tab.Shown.Subscribe(changes.Add);
        var renamed = new FileInfo("c.jpg");

        var matched = _tab.UpdateShownFile(secondary.FileInfo!.FullName, renamed);

        Assert.True(matched);
        Assert.Same(renamed, _tab.SecondaryModel!.FileInfo);
        Assert.Same(renamed, _tab.SecondaryFileInfo.Value);
        Assert.Same(primary.FileInfo, _tab.Model.FileInfo);
        Assert.Equal([ShownChange.File], changes);
    }

    [Fact]
    public void UpdateShownFile_WithoutSize_KeepsSize()
    {
        var model = Model("a.jpg");
        model.PixelWidth = 10;
        model.PixelHeight = 20;
        _tab.Display(model);

        _tab.UpdateShownFile(model.FileInfo!.FullName, new FileInfo("b.jpg"));

        Assert.Equal(10u, _tab.Model.PixelWidth);
        Assert.Equal(20u, _tab.Model.PixelHeight);
    }

    [Fact]
    public void UpdateShownFile_PathDifferingOnlyInCase_Matches()
    {
        var model = Model("a.JPG");
        _tab.Display(model);

        var matched = _tab.UpdateShownFile(model.FileInfo!.FullName.ToLowerInvariant(), new FileInfo("a.jpg"));

        Assert.True(matched);
    }

    [Fact]
    public void UpdateShownFile_UnshownPath_NoSecondary_ReturnsFalseAndChangesNothing()
    {
        var model = Model("a.jpg");
        _tab.Display(model);
        var changes = new List<ShownChange>();
        using var _ = _tab.Shown.Subscribe(changes.Add);

        var matched = _tab.UpdateShownFile(new FileInfo("zzz.jpg").FullName, new FileInfo("y.jpg"));

        Assert.False(matched);
        Assert.Same(model.FileInfo, _tab.Model.FileInfo);
        Assert.Empty(changes);
    }

    [Fact]
    public void IsShownFile_PrimarySecondaryAndOther()
    {
        var primary = Model("a.jpg");
        var secondary = Model("b.jpg");
        _tab.Display(primary, secondary);

        Assert.True(_tab.IsShownFile(primary.FileInfo!.FullName));
        Assert.True(_tab.IsShownFile(secondary.FileInfo!.FullName));
        Assert.False(_tab.IsShownFile(new FileInfo("c.jpg").FullName));
    }

    [Fact]
    public void UpdateTabTitle_SingleImage_UsesTheGivenTitle()
    {
        TranslationManager.Init();
        _tab.IsSelected = true;
        var single = new ImageModel { Image = new object(), PixelWidth = 1, PixelHeight = 1 };
        _tab.DisplaySingleImage(single, SingleImageType.Base64, "Pasted");

        _tab.UpdateTabTitle();

        Assert.Equal("Pasted", _tab.TabTitle.Value);
        Assert.StartsWith("Pasted", _tab.Title!.Value, StringComparison.Ordinal);
    }

    [Fact]
    public void UpdateTabTitle_FileWithoutIterator_LeavesTitleAlone()
    {
        TranslationManager.Init();
        _tab.IsSelected = true;
        _tab.SetLoading();
        var model = Model("a.jpg");
        model.PixelWidth = 1;
        model.PixelHeight = 1;
        _tab.Display(model);

        _tab.UpdateTabTitle();

        Assert.Equal(TranslationManager.Translation.Loading, _tab.Title!.Value);
    }

    private static ImageModel Model(string name, ImageType type = ImageType.Bitmap) => new()
    {
        FileInfo = new FileInfo(name),
        Image = new object(),
        ImageType = type
    };

    private sealed class MockFrameProvider : FrameProvider
    {
        public override long GetFrameCount() => 0;
        public override void Register(IFrameRunnerWorkItem callback) => callback.MoveNext(0);
    }
}
