using System.Reactive.Linq;
using System.Runtime.InteropServices;
using Avalonia.Input;
using PicView.Avalonia.Input;
using PicView.Avalonia.UI;
using PicView.Avalonia.ViewModels;
using PicView.Avalonia.Views.UC;

namespace PicView.Avalonia.Crop;

public class CropKeyboardManager(CropControl control)
{
    public async Task KeyDownHandler(KeyEventArgs e)
    {
        if (control.DataContext is not ImageCropperViewModel vm)
        {
            return;
        }

        switch (e.Key)
        {
            case Key.Enter:
                await vm.CropImageCommand.Execute();
                return;
            case Key.Escape:
                CropFunctions.CloseCropControl(UIHelper.GetMainView.DataContext as MainViewModel);
                return;
        }

        KeyGesture currentKeys;
        if (MainKeyboardShortcuts.CtrlDown || MainKeyboardShortcuts.AltOrOptionDown ||
            MainKeyboardShortcuts.ShiftDown || MainKeyboardShortcuts.CommandDown)
        {
            var modifiers = KeyModifiers.None;

            if (MainKeyboardShortcuts.CtrlDown)
            {
                modifiers |= KeyModifiers.Control;
            }

            if (MainKeyboardShortcuts.AltOrOptionDown)
            {
                modifiers |= KeyModifiers.Alt;
            }

            if (MainKeyboardShortcuts.ShiftDown)
            {
                modifiers |= KeyModifiers.Shift;
            }

            if (MainKeyboardShortcuts.CommandDown && RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                modifiers |= KeyModifiers.Meta;
            }

            currentKeys = new KeyGesture(e.Key, modifiers);
        }
        else
        {
            currentKeys = new KeyGesture(e.Key);
        }

        if (KeybindingManager.CustomShortcuts.TryGetValue(currentKeys, out var func))
        {
            var function = await FunctionsHelper.GetFunctionByName(func.Method.Name);
            switch (function.Method.Name)
            {
                case "Up":
                case "RotateLeft":
                    Rotate(false);
                    return;
                case "Down":
                case "RotateRight":
                    Rotate(true);
                    return;
                case "ZoomIn":
                    ZoomIn();
                    return;
                case "ZoomOut":
                    ZoomOut();
                    return;
                case "ResetZoom":
                    ResetZoom();
                    return;
                case "Save":
                case "SaveAs":
                case "GalleryClick":
                    await vm.CropImageCommand.Execute();
                    return;
            }
        }
    }


    private void ZoomIn()
    {
    }

    private void ZoomOut()
    {
    }

    private void ResetZoom()
    {
    }

    private void Rotate(bool clockwise)
    {
    }
}