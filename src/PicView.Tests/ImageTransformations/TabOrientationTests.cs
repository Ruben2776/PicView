using PicView.Core.ImageTransformations;
using PicView.Core.ViewModels;

namespace PicView.Tests.ImageTransformations;

public class TabOrientationTests : IDisposable
{
    private readonly TranslationViewModel _translation = new();
    private readonly TabOrientation _orientation;

    public TabOrientationTests()
    {
        _translation.Flip.Value = "Flip";
        _translation.UnFlip.Value = "Unflip";
        _orientation = new TabOrientation(_translation);
    }

    public void Dispose() => _orientation.Dispose();

    [Fact]
    public void New_IsUprightWithTheZeroRadioChecked()
    {
        Assert.Equal(Orientation.Upright, _orientation.Current.CurrentValue);
        Assert.Equal(0, _orientation.Degrees.Value);
        Assert.Equal(1d, _orientation.ScaleX.Value);
        Assert.Equal([true, false, false, false], RadioFlags());
    }

    [Fact]
    public void TurnRight_TurnsAQuarterAndMovesTheRadio()
    {
        _orientation.TurnRight();

        Assert.Equal(90, _orientation.Degrees.Value);
        Assert.Equal([false, true, false, false], RadioFlags());
    }

    [Fact]
    public void TurnLeft_FromUpright_TurnsToTwoSeventy()
    {
        _orientation.TurnLeft();

        Assert.Equal(270, _orientation.Degrees.Value);
        Assert.Equal([false, false, false, true], RadioFlags());
    }

    [Fact]
    public void TurnTo_SetsTheQuarterTurns()
    {
        _orientation.TurnTo(2);

        Assert.Equal(180, _orientation.Degrees.Value);
        Assert.Equal([false, false, true, false], RadioFlags());
    }

    [Fact]
    public void Flip_MirrorsTheImage()
    {
        _orientation.Flip();

        Assert.True(_orientation.Current.CurrentValue.IsFlipped);
        Assert.Equal(-1d, _orientation.ScaleX.Value);
    }

    [Fact]
    public void Reset_AfterTurningAndFlipping_IsUprightAndNotFlipped()
    {
        _orientation.TurnRight();
        _orientation.Flip();

        _orientation.Reset();

        Assert.Equal(Orientation.Upright, _orientation.Current.CurrentValue);
        Assert.Equal(1d, _orientation.ScaleX.Value);
        Assert.Equal([true, false, false, false], RadioFlags());
    }

    [Fact]
    public void FlipLabel_NamesWhatTheFlipActionWouldDo()
    {
        var beforeFlipping = _orientation.FlipLabel.Value;
        _orientation.Flip();
        var whileFlipped = _orientation.FlipLabel.Value;
        _orientation.Flip();

        Assert.Equal("Flip", beforeFlipping);
        Assert.Equal("Unflip", whileFlipped);
        Assert.Equal("Flip", _orientation.FlipLabel.Value);
    }

    [Fact]
    public void FlipLabel_FollowsALanguageChange()
    {
        _orientation.Flip();

        _translation.UnFlip.Value = "Spejlvend tilbage";

        Assert.Equal("Spejlvend tilbage", _orientation.FlipLabel.Value);
    }

    private bool[] RadioFlags() =>
    [
        _orientation.IsRotated0.Value, _orientation.IsRotated90.Value, _orientation.IsRotated180.Value,
        _orientation.IsRotated270.Value
    ];
}
