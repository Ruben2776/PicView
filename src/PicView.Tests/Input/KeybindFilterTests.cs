using System.Collections.ObjectModel;
using Avalonia.Input;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Input;

namespace PicView.Tests.Input;

public class KeybindFilterTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Matches_BlankFilter_MatchesEveryRow(string? filter)
    {
        Assert.True(KeybindFilter.Matches(filter, "Next", "Next image", []));
    }

    [Fact]
    public void Matches_DisplayName_IgnoresCase()
    {
        Assert.True(KeybindFilter.Matches("NEXT IMAGE", "Next", "Next image", []));
    }

    [Fact]
    public void Matches_FunctionName_WhenTheDisplayNameDoesNot()
    {
        Assert.True(KeybindFilter.Matches("RotateRight", "RotateRight", "Turn clockwise", []));
    }

    [Fact]
    public void Matches_KeybindText_WhenNeitherNameDoes()
    {
        Assert.True(KeybindFilter.Matches("ctrl", "Copy", "Copy path", ["Ctrl+C"]));
    }

    [Fact]
    public void Matches_NothingMatches_ReturnsFalse()
    {
        Assert.False(KeybindFilter.Matches("zoom", "Next", "Next image", ["Right"]));
    }

    [AvaloniaFact]
    public void KeybindText_FromTheDraft_EqualsTheTagTheBoxShows()
    {
        var keybinds = new[] { new Keybind(Key.Left), new Keybind(Key.C, KeyModifiers.Control) };
        var box = new KeybindBox
        {
            Keybinds = new ObservableCollection<Keybind>(keybinds)
        };

        var draftTexts = keybinds.Select(keybind => keybind.GetFormattedWithIcon());

        Assert.Equal(draftTexts, box.Tags);
    }
}
