using System.Runtime.InteropServices;
using Avalonia.Input;
using PicView.Avalonia.Input;
using MouseButton = PicView.Avalonia.Input.MouseButton;

namespace PicView.Tests.Input;

public class KeybindTests
{
    [Fact]
    public void ToString_SingleKey_ReturnsKeyName()
    {
        var keybind = new Keybind(Key.F5);
        Assert.Equal("F5", keybind.ToString());
    }

    [Fact]
    public void ToString_SingleMouseButton_ReturnsButtonName()
    {
        var keybind = new Keybind(MouseButton.Left);
        Assert.Equal("Left", keybind.ToString());
    }

    [Fact]
    public void ToString_ModifierAndKey_ReturnsCombinedString()
    {
        var keybind = new Keybind(Key.A, KeyModifiers.Control);
        Assert.Equal("Ctrl + A", keybind.ToString());
    }

    [Fact]
    public void ToString_TwoModifiersAndKey_ReturnsCombinedString()
    {
        var isMac = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        var altName = isMac ? "Option" : "Alt";

        var keybind = new Keybind(Key.O, KeyModifiers.Control | KeyModifiers.Alt);
        Assert.Equal($"Ctrl + {altName} + O", keybind.ToString());
    }

    [Fact]
    public void ToString_TwoModifiersAndMouseButton_ReturnsCombinedString()
    {
        var isMac = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        var altName = isMac ? "Option" : "Alt";

        var keybind = new Keybind(MouseButton.Right, KeyModifiers.Shift | KeyModifiers.Alt);
        Assert.Equal($"Shift + {altName} + Right", keybind.ToString());
    }

    [Fact]
    public void ToString_MetaModifier_ReturnsCorrectPlatformName()
    {
        var isMac = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        var metaName = isMac ? "Cmd" : "Win";

        var keybind = new Keybind(Key.S, KeyModifiers.Meta);
        Assert.Equal($"{metaName} + S", keybind.ToString());
    }

    [Fact]
    public void ToString_DefaultKeybind_ReturnsEmpty()
    {
        var keybind = default(Keybind);
        Assert.Equal(string.Empty, keybind.ToString());
    }

    [Theory]
    [InlineData("Ctrl+A", "Ctrl + A")]
    [InlineData("Shift+Delete", "Shift + Delete")]
    [InlineData("Ctrl+Shift+Z", "Ctrl + Shift + Z")]
    [InlineData("Ctrl + A", "Ctrl + A")]
    [InlineData("Shift + Delete", "Shift + Delete")]
    [InlineData("Ctrl + Shift + Z", "Ctrl + Shift + Z")]
    public void Parse_And_ToString_Roundtrips(string input, string expected)
    {
        var parsed = Keybind.Parse(input);
        Assert.Equal(expected, parsed.ToString());
    }
}
