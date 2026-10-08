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
        var keybind = new Keybind(MouseButton.Middle);
        Assert.Equal("Middle", keybind.ToString());
    }

    [Fact]
    public void ToString_ModifierAndKey_ReturnsCombinedString()
    {
        var isMac = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        var ctrlName = isMac ? Keybind.MacCtrlModifier : Keybind.CtrlModifier;

        var keybind = new Keybind(Key.A, KeyModifiers.Control);
        Assert.Equal($"{ctrlName} + A", keybind.ToString());
    }

    [Fact]
    public void ToString_TwoModifiersAndKey_ReturnsCombinedString()
    {
        var isMac = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        var ctrlName = isMac ? Keybind.MacCtrlModifier : Keybind.CtrlModifier;
        var altName = isMac ? Keybind.MacOptionModifier : Keybind.AltModifier;

        var keybind = new Keybind(Key.O, KeyModifiers.Control | KeyModifiers.Alt);
        Assert.Equal($"{ctrlName} + {altName} + O", keybind.ToString());
    }

    [Fact]
    public void ToString_TwoModifiersAndMouseButton_ReturnsCombinedString()
    {
        var isMac = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        var shiftName = isMac ? Keybind.MacShiftModifier : Keybind.ShiftModifier;
        var altName = isMac ? Keybind.MacOptionModifier : Keybind.AltModifier;

        var keybind = new Keybind(MouseButton.XButton1, KeyModifiers.Shift | KeyModifiers.Alt);
        Assert.Equal($"{shiftName} + {altName} + XButton1", keybind.ToString());
    }

    [Fact]
    public void ToString_SideMouseButton2_ReturnsButtonName()
    {
        var keybind = new Keybind(MouseButton.XButton2);
        Assert.Equal("XButton2", keybind.ToString());
    }

    [Fact]
    public void ToString_MetaModifier_ReturnsCorrectPlatformName()
    {
        var isMac = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        var metaName = isMac ? Keybind.MacCmdModifier : Keybind.WinModifier;

        var keybind = new Keybind(Key.S, KeyModifiers.Meta);
        Assert.Equal($"{metaName} + S", keybind.ToString());
    }

    [Fact]
    public void ToString_DefaultKeybind_ReturnsEmpty()
    {
        var keybind = default(Keybind);
        Assert.Equal(string.Empty, keybind.ToString());
    }

    [Fact]
    public void ToString_OnMac_UsesModifierSymbols()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return;
        }

        Assert.Equal("⌃ Ctrl + A", new Keybind(Key.A, KeyModifiers.Control).ToString());
        Assert.Equal("⇧ Shift + A", new Keybind(Key.A, KeyModifiers.Shift).ToString());
        Assert.Equal("⌥ Option + A", new Keybind(Key.A, KeyModifiers.Alt).ToString());
        Assert.Equal("⌘ Cmd + A", new Keybind(Key.A, KeyModifiers.Meta).ToString());
    }

    [Theory]
    [InlineData("Ctrl+A")]
    [InlineData("Shift+Delete")]
    [InlineData("Ctrl+Shift+Z")]
    [InlineData("Ctrl + A")]
    [InlineData("Shift + Delete")]
    [InlineData("Ctrl + Shift + Z")]
    public void Parse_And_ToString_Roundtrips(string input)
    {
        var parsed = Keybind.Parse(input);
        Assert.NotNull(parsed);
        var str = parsed.Value.ToString();
        var reparsed = Keybind.Parse(str);
        Assert.Equal(parsed, reparsed);
    }

    [Theory]
    [InlineData("⌃ Ctrl + A", Key.A, KeyModifiers.Control)]
    [InlineData("⇧ Shift + Delete", Key.Delete, KeyModifiers.Shift)]
    [InlineData("⌥ Option + O", Key.O, KeyModifiers.Alt)]
    [InlineData("⌘ Cmd + S", Key.S, KeyModifiers.Meta)]
    [InlineData("⌘ Cmd + ⇧ Shift + Z", Key.Z, KeyModifiers.Meta | KeyModifiers.Shift)]
    [InlineData("⌃ + A", Key.A, KeyModifiers.Control)]
    [InlineData("⌥ + O", Key.O, KeyModifiers.Alt)]
    [InlineData("⇧ + Delete", Key.Delete, KeyModifiers.Shift)]
    [InlineData("⌘ + S", Key.S, KeyModifiers.Meta)]
    public void Parse_MacModifierPrefixes_ParsesCorrectly(string input, Key expectedKey, KeyModifiers expectedModifiers)
    {
        var parsed = Keybind.Parse(input);
        Assert.NotNull(parsed);
        Assert.Equal(expectedKey, parsed.Value.Key);
        Assert.Equal(expectedModifiers, parsed.Value.Modifiers);
    }
}
