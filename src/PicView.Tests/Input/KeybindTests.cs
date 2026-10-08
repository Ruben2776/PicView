using System.Runtime.InteropServices;
using Avalonia.Input;
using PicView.Avalonia.Input;
using MouseButton = PicView.Avalonia.Input.MouseButton;

namespace PicView.Tests.Input;

public class KeybindTests
{
    [Fact]
    public void GetFormattedWithIcon_SingleKey_ReturnsKeyName()
    {
        var keybind = new Keybind(Key.F5);
        Assert.Equal("F5", keybind.GetFormattedWithIcon());
        Assert.Equal("F5", keybind.ToString());
    }

    [Fact]
    public void GetFormattedWithIcon_SingleMouseButton_ReturnsButtonName()
    {
        var keybind = new Keybind(MouseButton.Middle);
        Assert.Equal("Middle", keybind.GetFormattedWithIcon());
        Assert.Equal("Middle", keybind.ToString());
    }

    [Fact]
    public void GetFormattedWithIcon_ModifierAndKey_ReturnsCombinedString()
    {
        var isMac = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        var ctrlName = isMac ? Keybind.MacCtrlModifier : Keybind.CtrlModifier;

        var keybind = new Keybind(Key.A, KeyModifiers.Control);
        Assert.Equal($"{ctrlName} + A", keybind.GetFormattedWithIcon());
        Assert.Equal($"{ctrlName} + A", keybind.ToString());
    }

    [Fact]
    public void GetFormattedWithIcon_TwoModifiersAndKey_ReturnsCombinedString()
    {
        var isMac = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        var ctrlName = isMac ? Keybind.MacCtrlModifier : Keybind.CtrlModifier;
        var altName = isMac ? Keybind.MacOptionModifier : Keybind.AltModifier;

        var keybind = new Keybind(Key.O, KeyModifiers.Control | KeyModifiers.Alt);
        Assert.Equal($"{ctrlName} + {altName} + O", keybind.GetFormattedWithIcon());
        Assert.Equal($"{ctrlName} + {altName} + O", keybind.ToString());
    }

    [Fact]
    public void GetFormattedWithIcon_TwoModifiersAndMouseButton_ReturnsCombinedString()
    {
        var isMac = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        var shiftName = isMac ? Keybind.MacShiftModifier : Keybind.ShiftModifier;
        var altName = isMac ? Keybind.MacOptionModifier : Keybind.AltModifier;

        var keybind = new Keybind(MouseButton.XButton1, KeyModifiers.Shift | KeyModifiers.Alt);
        Assert.Equal($"{shiftName} + {altName} + XButton1", keybind.GetFormattedWithIcon());
        Assert.Equal($"{shiftName} + {altName} + XButton1", keybind.ToString());
    }

    [Fact]
    public void GetFormattedWithIcon_SideMouseButton2_ReturnsButtonName()
    {
        var keybind = new Keybind(MouseButton.XButton2);
        Assert.Equal("XButton2", keybind.GetFormattedWithIcon());
        Assert.Equal("XButton2", keybind.ToString());
    }

    [Fact]
    public void GetFormattedWithIcon_MetaModifier_ReturnsCorrectPlatformName()
    {
        var isMac = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        var metaName = isMac ? Keybind.MacCmdModifier : Keybind.WinModifier;

        var keybind = new Keybind(Key.S, KeyModifiers.Meta);
        Assert.Equal($"{metaName} + S", keybind.GetFormattedWithIcon());
        Assert.Equal($"{metaName} + S", keybind.ToString());
    }

    [Fact]
    public void GetFormattedWithIcon_DefaultKeybind_ReturnsEmpty()
    {
        var keybind = default(Keybind);
        Assert.Equal(string.Empty, keybind.GetFormattedWithIcon());
        Assert.Equal(string.Empty, keybind.ToString());
    }

    [Fact]
    public void GetFormattedWithIcon_OnMac_UsesModifierSymbols()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return;
        }

        Assert.Equal("⌃ Ctrl + A", new Keybind(Key.A, KeyModifiers.Control).GetFormattedWithIcon());
        Assert.Equal("⇧ Shift + A", new Keybind(Key.A, KeyModifiers.Shift).GetFormattedWithIcon());
        Assert.Equal("⌥ Option + A", new Keybind(Key.A, KeyModifiers.Alt).GetFormattedWithIcon());
        Assert.Equal("⌘ Cmd + A", new Keybind(Key.A, KeyModifiers.Meta).GetFormattedWithIcon());
    }

    [Fact]
    public void GetFormattedForJSON_DoesNotIncludeModifierIcons()
    {
        var isMac = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        var altName = isMac ? Keybind.OptionModifier : Keybind.AltModifier;
        var metaName = isMac ? Keybind.CmdModifier : Keybind.WinModifier;

        var ctrl = new Keybind(Key.A, KeyModifiers.Control);
        var shift = new Keybind(Key.Delete, KeyModifiers.Shift);
        var alt = new Keybind(Key.O, KeyModifiers.Alt);
        var meta = new Keybind(Key.S, KeyModifiers.Meta);
        var combo = new Keybind(Key.Z, KeyModifiers.Control | KeyModifiers.Shift);

        Assert.Equal("Ctrl + A", ctrl.GetFormattedForJSON());
        Assert.Equal("Shift + Delete", shift.GetFormattedForJSON());
        Assert.Equal($"{altName} + O", alt.GetFormattedForJSON());
        Assert.Equal($"{metaName} + S", meta.GetFormattedForJSON());
        Assert.Equal("Ctrl + Shift + Z", combo.GetFormattedForJSON());
    }

    [Theory]
    [InlineData("Ctrl+A", Key.A, KeyModifiers.Control)]
    [InlineData("Shift+Delete", Key.Delete, KeyModifiers.Shift)]
    [InlineData("Ctrl+Shift+Z", Key.Z, KeyModifiers.Control | KeyModifiers.Shift)]
    [InlineData("Ctrl + A", Key.A, KeyModifiers.Control)]
    [InlineData("Shift + Delete", Key.Delete, KeyModifiers.Shift)]
    [InlineData("Ctrl + Shift + Z", Key.Z, KeyModifiers.Control | KeyModifiers.Shift)]
    public void Parse_And_GetFormattedForJSON_Roundtrips(string input, Key expectedKey, KeyModifiers expectedModifiers)
    {
        var parsed = Keybind.Parse(input);
        Assert.NotNull(parsed);
        var expected = new Keybind(expectedKey, expectedModifiers);
        Assert.Equal(expected, parsed.Value);
        Assert.Equal(expected.GetFormattedForJSON(), parsed.Value.GetFormattedForJSON());

        var reparsed = Keybind.Parse(parsed.Value.GetFormattedForJSON());
        Assert.NotNull(reparsed);
        Assert.Equal(expected, reparsed.Value);
    }
}
