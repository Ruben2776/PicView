using PicView.Core.Animation;
using PicView.Core.ImageDecoding;
using PicView.Core.Models;
using PicView.Core.Navigation.Tiff;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Tests.Animation;

[Collection("Sequential")]
public class TabAnimationTests
{
    private readonly TabViewModel _tab;

    public TabAnimationTests()
    {
        ObservableSystem.DefaultFrameProvider = new MockFrameProvider();
        SetDefaults();
        _tab = new TabViewModel(null!);
    }

    private static ImageModel Model(string name, ImageType type) =>
        new() { FileInfo = new FileInfo(name), Image = new object(), ImageType = type };

    private void DisplaySideBySide(ImageModel primary, ImageModel secondary)
    {
        _tab.Display(primary, secondary);
    }

    [Fact]
    public void Display_AnimatedImage_GivesPrimaryAPlayback()
    {
        var model = Model("a.gif", ImageType.AnimatedGif);

        _tab.Display(model);

        var playback = Assert.IsType<AnimationPlayback>(_tab.Animation.Primary.Value);
        Assert.Same(model.FileInfo, playback.File);
        Assert.Equal(ImageType.AnimatedGif, playback.ImageType);
        Assert.Null(_tab.Animation.Secondary.Value);
    }

    [Fact]
    public void Display_StillImage_GivesNoPlayback()
    {
        _tab.Display(Model("a.jpg", ImageType.Bitmap));

        Assert.Null(_tab.Animation.Primary.Value);
    }

    [Fact]
    public void Display_AnimatedWithoutFile_GivesNoPlayback()
    {
        _tab.Display(new ImageModel { Image = new object(), ImageType = ImageType.AnimatedGif });

        Assert.Null(_tab.Animation.Primary.Value);
    }

    [Fact]
    public void Display_SideBySide_GivesEachAnimatedSideItsOwn()
    {
        DisplaySideBySide(Model("a.gif", ImageType.AnimatedGif), Model("b.webp", ImageType.AnimatedWebp));

        var primary = _tab.Animation.Primary.Value;
        var secondary = _tab.Animation.Secondary.Value;
        Assert.NotNull(primary);
        Assert.NotNull(secondary);
        Assert.NotSame(primary, secondary);
        Assert.Equal(ImageType.AnimatedWebp, secondary.ImageType);

        DisplaySideBySide(Model("a.gif", ImageType.AnimatedGif), Model("b.jpg", ImageType.Bitmap));

        Assert.NotNull(_tab.Animation.Primary.Value);
        Assert.Null(_tab.Animation.Secondary.Value);
    }

    [Fact]
    public void Display_SameFileAgain_GivesANewPlayback()
    {
        var model = Model("a.gif", ImageType.AnimatedGif);
        _tab.Display(model);
        var first = _tab.Animation.Primary.Value;

        _tab.Display(model);

        Assert.NotSame(first, _tab.Animation.Primary.Value);
    }

    [Fact]
    public void Display_PlaybackIsSetBeforeImageType()
    {
        AnimationPlayback? seen = null;
        using var _ = _tab.ImageType.Skip(1).Subscribe(_ => seen = _tab.Animation.Primary.Value);

        _tab.Display(Model("a.gif", ImageType.AnimatedGif));

        Assert.NotNull(seen);
        Assert.Same(_tab.Animation.Primary.Value, seen);
    }

    // A control that loses its player draws its image again, so the image must already be the new one: the old
    // bitmap may be disposed by then, as when the next file is in another directory
    [Fact]
    public void Display_StillAfterAnimated_SetsImageBeforeEndingThePlayback()
    {
        _tab.Display(Model("a.gif", ImageType.AnimatedGif));
        var next = Model("b.jpg", ImageType.Bitmap);
        object? imageWhenPlaybackEnded = null;
        using var subscription = _tab.Animation.Primary.AsObservable().Skip(1)
            .Subscribe(_ => imageWhenPlaybackEnded = _tab.Image.Value);

        _tab.Display(next);

        Assert.Same(next.Image, imageWhenPlaybackEnded);
    }

    [Fact]
    public void Display_SideBySide_SetsBothImagesBeforeChangingThePlaybacks()
    {
        _tab.Display(Model("a.gif", ImageType.AnimatedGif), Model("b.webp", ImageType.AnimatedWebp));
        var primary = Model("c.gif", ImageType.AnimatedGif);
        var secondary = Model("d.jpg", ImageType.Bitmap);
        object? primaryImageWhenChanged = null;
        object? secondaryImageWhenChanged = null;
        using var first = _tab.Animation.Primary.AsObservable().Skip(1)
            .Subscribe(_ => primaryImageWhenChanged = _tab.Image.Value);
        using var second = _tab.Animation.Secondary.AsObservable().Skip(1)
            .Subscribe(_ => secondaryImageWhenChanged = _tab.SecondaryImage.Value);

        _tab.Display(primary, secondary);

        Assert.Same(primary.Image, primaryImageWhenChanged);
        Assert.Same(secondary.Image, secondaryImageWhenChanged);
    }

    [Fact]
    public void ShowStandIn_SetsImageBeforeEndingThePlayback()
    {
        _tab.Display(Model("a.gif", ImageType.AnimatedGif));
        var standIn = new object();
        object? imageWhenPlaybackEnded = null;
        using var subscription = _tab.Animation.Primary.AsObservable().Skip(1)
            .Subscribe(_ => imageWhenPlaybackEnded = _tab.Image.Value);

        _tab.ShowStandIn(standIn);

        Assert.Same(standIn, imageWhenPlaybackEnded);
    }

    [Fact]
    public void UpdateShownFile_AnimatedPrimary_ReplacesItsPlayback()
    {
        var primary = Model("a.gif", ImageType.AnimatedGif);
        DisplaySideBySide(primary, Model("b.webp", ImageType.AnimatedWebp));
        var oldPrimary = _tab.Animation.Primary.Value;
        var secondary = _tab.Animation.Secondary.Value;
        var renamed = new FileInfo("c.gif");

        Assert.True(_tab.UpdateShownFile(primary.FileInfo!.FullName, renamed));

        Assert.NotSame(oldPrimary, _tab.Animation.Primary.Value);
        Assert.Same(renamed, _tab.Animation.Primary.Value!.File);
        Assert.Same(secondary, _tab.Animation.Secondary.Value);
    }

    [Fact]
    public void UpdateShownFile_AnimatedSecondary_ReplacesOnlySecondary()
    {
        var secondaryModel = Model("b.webp", ImageType.AnimatedWebp);
        DisplaySideBySide(Model("a.gif", ImageType.AnimatedGif), secondaryModel);
        var primary = _tab.Animation.Primary.Value;
        var oldSecondary = _tab.Animation.Secondary.Value;
        var renamed = new FileInfo("c.webp");

        Assert.True(_tab.UpdateShownFile(secondaryModel.FileInfo!.FullName, renamed));

        Assert.Same(primary, _tab.Animation.Primary.Value);
        Assert.NotSame(oldSecondary, _tab.Animation.Secondary.Value);
        Assert.Same(renamed, _tab.Animation.Secondary.Value!.File);
    }

    [Fact]
    public void ShowStandIn_EndsBothPlaybacks()
    {
        DisplaySideBySide(Model("a.gif", ImageType.AnimatedGif), Model("b.webp", ImageType.AnimatedWebp));

        _tab.ShowStandIn(new object());

        Assert.Null(_tab.Animation.Primary.Value);
        Assert.Null(_tab.Animation.Secondary.Value);
    }

    [Fact]
    public void ShowLoading_EndsBothPlaybacks()
    {
        DisplaySideBySide(Model("a.gif", ImageType.AnimatedGif), Model("b.webp", ImageType.AnimatedWebp));

        _tab.ShowLoading(new object());

        Assert.Null(_tab.Animation.Primary.Value);
        Assert.Null(_tab.Animation.Secondary.Value);
    }

    [Fact]
    public void ShowPage_OnTiffBesideAnAnimation_LeavesTheSecondaryPlaybackAlone()
    {
        var pages = new[] { new object(), new object() };
        var tiff = Model("a.tif", ImageType.Bitmap);
        tiff.Image = pages[0];
        tiff.TiffNavigation = new TiffNavigationInfo { PageCount = 2, CurrentPage = 0, Pages = pages };
        DisplaySideBySide(tiff, Model("b.gif", ImageType.AnimatedGif));
        var playback = _tab.Animation.Secondary.Value;

        _tab.ShowPage(1);

        Assert.Same(pages[1], _tab.Image.Value);
        Assert.NotNull(playback);
        Assert.Same(playback, _tab.Animation.Secondary.Value);
        Assert.Null(_tab.Animation.Primary.Value);
    }

    [Fact]
    public void DisposeImageIterator_EndsBothPlaybacks()
    {
        DisplaySideBySide(Model("a.gif", ImageType.AnimatedGif), Model("b.webp", ImageType.AnimatedWebp));

        _tab.DisposeImageIterator();

        Assert.Null(_tab.Animation.Primary.Value);
        Assert.Null(_tab.Animation.Secondary.Value);
    }

    [Fact]
    public void TogglePause_PausesBoth_AndResumesBoth()
    {
        DisplaySideBySide(Model("a.gif", ImageType.AnimatedGif), Model("b.webp", ImageType.AnimatedWebp));

        _tab.Animation.TogglePause();

        Assert.True(_tab.Animation.Primary.Value!.IsPaused);
        Assert.True(_tab.Animation.Secondary.Value!.IsPaused);

        _tab.Animation.TogglePause();

        Assert.False(_tab.Animation.Primary.Value.IsPaused);
        Assert.False(_tab.Animation.Secondary.Value.IsPaused);
    }

    [Fact]
    public void TogglePause_WhenTheyDiffer_FollowsThePrimary()
    {
        DisplaySideBySide(Model("a.gif", ImageType.AnimatedGif), Model("b.webp", ImageType.AnimatedWebp));
        _tab.Animation.Secondary.Value!.SetPaused(true);

        _tab.Animation.TogglePause();

        Assert.True(_tab.Animation.Primary.Value!.IsPaused);
        Assert.True(_tab.Animation.Secondary.Value.IsPaused);
    }

    [Fact]
    public void Step_StepsBoth()
    {
        DisplaySideBySide(Model("a.gif", ImageType.AnimatedGif), Model("b.webp", ImageType.AnimatedWebp));

        _tab.Animation.Step(1);

        Assert.True(_tab.Animation.Primary.Value!.IsPaused);
        Assert.True(_tab.Animation.Secondary.Value!.IsPaused);
    }

    [Fact]
    public void SetSpeed_AppliesToBoth()
    {
        DisplaySideBySide(Model("a.gif", ImageType.AnimatedGif), Model("b.webp", ImageType.AnimatedWebp));

        _tab.Animation.SetSpeed(2);

        Assert.Equal(2, _tab.Animation.Primary.Value!.Speed);
        Assert.Equal(2, _tab.Animation.Secondary.Value!.Speed);
    }

    [Fact]
    public void SpeedUp_MovesOneStepAtATime_AndStaysAtTheFastest()
    {
        _tab.Display(Model("a.gif", ImageType.AnimatedGif));

        Assert.Equal(2, _tab.Animation.SpeedUp());
        Assert.Equal(4, _tab.Animation.SpeedUp());
        Assert.Equal(4, _tab.Animation.SpeedUp());
        Assert.Equal(4, _tab.Animation.Primary.Value!.Speed);
    }

    [Fact]
    public void SlowDown_MovesOneStepAtATime_AndStaysAtTheSlowest()
    {
        _tab.Display(Model("a.gif", ImageType.AnimatedGif));

        Assert.Equal(0.5, _tab.Animation.SlowDown());
        Assert.Equal(0.25, _tab.Animation.SlowDown());
        Assert.Equal(0.25, _tab.Animation.SlowDown());
        Assert.Equal(0.25, _tab.Animation.Primary.Value!.Speed);
    }

    [Fact]
    public void SpeedUpAndSlowDown_FromASpeedBetweenSteps_GoToTheNearestStepThatWay()
    {
        _tab.Display(Model("a.gif", ImageType.AnimatedGif));
        _tab.Animation.SetSpeed(1.5);

        Assert.Equal(2, _tab.Animation.SpeedUp());

        _tab.Animation.SetSpeed(1.5);

        Assert.Equal(1, _tab.Animation.SlowDown());
    }

    [Fact]
    public void ResetSpeed_ReturnsToNormal()
    {
        _tab.Display(Model("a.gif", ImageType.AnimatedGif));
        _tab.Animation.SlowDown();

        _tab.Animation.ResetSpeed();

        Assert.Equal(1, _tab.Animation.Speed);
        Assert.Equal(1, _tab.Animation.Primary.Value!.Speed);
    }

    [Fact]
    public void Display_NextAnimatedImage_KeepsTheSpeedButNotThePause()
    {
        _tab.Display(Model("a.gif", ImageType.AnimatedGif));
        _tab.Animation.SlowDown();
        _tab.Animation.TogglePause();

        _tab.Display(Model("b.webp", ImageType.AnimatedWebp));

        var playback = _tab.Animation.Primary.Value!;
        Assert.Equal(0.5, playback.Speed);
        Assert.False(playback.IsPaused);
    }

    [Fact]
    public void Display_StillImageInBetween_KeepsTheSpeed()
    {
        _tab.Display(Model("a.gif", ImageType.AnimatedGif));
        _tab.Animation.SpeedUp();

        _tab.Display(Model("b.jpg", ImageType.Bitmap));
        DisplaySideBySide(Model("c.gif", ImageType.AnimatedGif), Model("d.gif", ImageType.AnimatedGif));

        Assert.Equal(2, _tab.Animation.Primary.Value!.Speed);
        Assert.Equal(2, _tab.Animation.Secondary.Value!.Speed);
    }

    [Fact]
    public void UpdateShownFile_KeepsTheSpeed()
    {
        var primary = Model("a.gif", ImageType.AnimatedGif);
        _tab.Display(primary);
        _tab.Animation.SpeedUp();

        Assert.True(_tab.UpdateShownFile(primary.FileInfo!.FullName, new FileInfo("c.gif")));

        Assert.Equal(2, _tab.Animation.Primary.Value!.Speed);
    }

    [Fact]
    public void AnotherTab_StartsAtNormalSpeed()
    {
        _tab.Display(Model("a.gif", ImageType.AnimatedGif));
        _tab.Animation.SpeedUp();
        var other = new TabViewModel(null!);

        other.Display(Model("a.gif", ImageType.AnimatedGif));

        Assert.Equal(1, other.Animation.Primary.Value!.Speed);
    }

    [Fact]
    public void IsActive_FollowsWhetherAnAnimatedImageIsShown()
    {
        Assert.False(_tab.Animation.IsActive.Value);

        DisplaySideBySide(Model("a.jpg", ImageType.Bitmap), Model("b.gif", ImageType.AnimatedGif));
        Assert.True(_tab.Animation.IsActive.Value);

        _tab.ShowStandIn(new object());
        Assert.False(_tab.Animation.IsActive.Value);

        _tab.Display(Model("a.gif", ImageType.AnimatedGif));
        Assert.True(_tab.Animation.IsActive.Value);

        _tab.Display(Model("a.jpg", ImageType.Bitmap));
        Assert.False(_tab.Animation.IsActive.Value);
    }

    [Fact]
    public void Commands_WithNoPlayback_DoNothing()
    {
        _tab.Animation.TogglePause();
        _tab.Animation.Step(1);
        _tab.Animation.SetSpeed(2);

        Assert.Null(_tab.Animation.Primary.Value);
        Assert.Null(_tab.Animation.Secondary.Value);
    }

    private sealed class MockFrameProvider : FrameProvider
    {
        public override long GetFrameCount() => 0;
        public override void Register(IFrameRunnerWorkItem callback) => callback.MoveNext(0);
    }
}
