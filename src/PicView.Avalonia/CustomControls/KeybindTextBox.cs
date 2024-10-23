using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using PicView.Avalonia.Input;
using PicView.Avalonia.UI;
using PicView.Core.Localization;

namespace PicView.Avalonia.CustomControls;

public class KeybindTextBox : TextBox
{
    protected override Type StyleKeyOverride => typeof(TextBox);
    
    public static readonly AvaloniaProperty<KeyGesture?> KeybindProperty =
        AvaloniaProperty.Register<KeybindTextBox, KeyGesture?>(nameof(Keybind));

    public static readonly AvaloniaProperty<string?> MethodNameProperty =
        AvaloniaProperty.Register<KeybindTextBox, string?>(nameof(MethodName));

    public static readonly AvaloniaProperty<bool?> AltProperty =
        AvaloniaProperty.Register<KeybindTextBox, bool?>(nameof(Alt));

    public KeyGesture? Keybind
    {
        get => (KeyGesture?)GetValue(KeybindProperty);
        set => SetValue(KeybindProperty, value);
    }

    public string MethodName
    {
        get => (string)(GetValue(MethodNameProperty) ?? "");
        set => SetValue(MethodNameProperty, value);
    }

    public bool Alt
    {
        get => (bool)(GetValue(AltProperty) ?? false);
        set => SetValue(AltProperty, value);
    }

    public KeybindTextBox()
    {
        this.GetObservable(MethodNameProperty).Subscribe(_ => Text = GetFunctionKey());
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            AddHandler(KeyUpEvent, KeyDownHandler, RoutingStrategies.Tunnel);
            // On macOS, we only get KeyUp, because the option to select a different character
            // when a key is held down interferes with the keyboard shortcuts
        }
        else
        {
            AddHandler(KeyDownEvent, KeyDownHandler, RoutingStrategies.Tunnel);
            AddHandler(KeyUpEvent, KeyUpHandler, RoutingStrategies.Tunnel);
        }
        
        GotFocus += delegate
        {
            if (IsReadOnly)
            {
                if (!this.TryFindResource("MainBorderColor", ThemeVariant.Default, out var borderColor))
                    return;
                var borderBrush = new SolidColorBrush((Color)(borderColor ?? Color.Parse("#FFf6f4f4")));
                BorderBrush = borderBrush;
                return;
            }
            if (!this.TryFindResource("MainTextColorFaded", ThemeVariant.Default, out var color))
                return;
            var foreground = new SolidColorBrush((Color)(color ?? Color.Parse("#d6d4d4")));
            Foreground = foreground;
            Text = TranslationHelper.Translation.PressKey;
            CaretIndex = 0;
        };
        LostFocus += delegate
        {
            if (!this.TryFindResource("MainTextColor", ThemeVariant.Default, out var color))
                return;
            var foreground = new SolidColorBrush((Color)(color ?? Color.Parse("#FFf6f4f4")));
            Foreground = foreground;
            this.GetObservable(MethodNameProperty).Subscribe(_ => Text = GetFunctionKey());
        };
    }

    private void KeyUpHandler(object? sender, KeyEventArgs e)
    {
        // TODO: figure out how to clear focus
        if (!this.TryFindResource("MainTextColor", ThemeVariant.Default, out var color))
            return;
        var foreground = new SolidColorBrush((Color)(color ?? Color.Parse("#FFf6f4f4")));
        Foreground = foreground;
        this.GetObservable(MethodNameProperty).Subscribe(_ => Text = GetFunctionKey());
    }

    private async Task KeyDownHandler(object? sender, KeyEventArgs e)
    {
        await AssociateKey(e);
    }

    private async Task AssociateKey(KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.LeftShift:
            case Key.RightShift:
            case Key.LeftCtrl:
            case Key.RightCtrl:
            case Key.LeftAlt:
            case Key.RightAlt:
            case Key.LWin:
            case Key.RWin:
                return;
        }

        KeybindingManager.CustomShortcuts.Remove(new KeyGesture(e.Key, e.KeyModifiers));

        var function = await FunctionsHelper.GetFunctionByName(MethodName);

        if (function == null)
        {
            return;
        }

        if (e.Key == Key.Escape)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                Text = string.Empty;
            });

            Remove();
            await Save();
            return;
        }

        // Handle whether it's an alternative key or not
        if (Alt)
        {
            if (KeybindingManager.CustomShortcuts.ContainsValue(function))
            {
                // If the main key is not present, add a new entry with the alternative key
                var altKey = (Key)Enum.Parse(typeof(Key), e.Key.ToString());
                var keyGesture = new KeyGesture(altKey, e.KeyModifiers);
                KeybindingManager.CustomShortcuts[keyGesture] = function;
            }
            else
            {
                // Update the key and function name in the CustomShortcuts dictionary
                var keyGesture = new KeyGesture(e.Key, e.KeyModifiers);
                KeybindingManager.CustomShortcuts[keyGesture] = function;
            }
        }
        else
        {
            // Remove if it already contains
            if (KeybindingManager.CustomShortcuts.ContainsValue(function))
            {
                Remove();
            }
            var keyGesture = new KeyGesture(e.Key, e.KeyModifiers);
            KeybindingManager.CustomShortcuts[keyGesture] = function;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            KeyUpHandler(this, e);
        }

        await Save();
        return;

        async Task Save()
        {
            await KeybindingManager.UpdateKeyBindingsFile();
        }

        void Remove()
        {
            var keys = KeybindingManager.CustomShortcuts.Where(x => x.Value?.Method?.Name == MethodName)
                ?.Select(x => x.Key).ToList() ?? null;
            if (keys is not null)
            {
                KeybindingManager.CustomShortcuts.Remove(Alt ? keys.LastOrDefault() : keys.FirstOrDefault());
            }
        }
    }

    public string? GetFunctionKey()
    {
        if (string.IsNullOrEmpty(MethodName))
        {
            return string.Empty;
        }
        
        if (IsReadOnly)
        {
            switch (MethodName)
            {
                case "ScrollUpInternal":
                    var rotateRightKey = KeybindingManager.CustomShortcuts.Where(x => x.Value?.Method?.Name == "Up")
                        ?.Select(x => x.Key).ToList() ?? null;
                    return rotateRightKey is not { Count: > 0 } ?
                        string.Empty :
                        Alt ? rotateRightKey.LastOrDefault().ToString() : rotateRightKey.FirstOrDefault().ToString();

                case "ScrollDownInternal":
                    var rotateLeftKey = KeybindingManager.CustomShortcuts.Where(x => x.Value?.Method?.Name == "Down")
                        ?.Select(x => x.Key).ToList() ?? null;
                    return rotateLeftKey is not { Count: > 0 } ?
                        string.Empty :
                        Alt ? rotateLeftKey.LastOrDefault().ToString() : rotateLeftKey.FirstOrDefault().ToString();
            }
        }

        // Find the key associated with the specified function
        var keys = KeybindingManager.CustomShortcuts.Where(x => x.Value?.Method?.Name == MethodName)?.Select(x => x.Key).ToList() ?? null;

        if (keys is null)
        {
            return string.Empty;
        }
        return keys.Count switch
        {
            <= 0 => string.Empty,
            1 => Alt ? string.Empty : FormatPlus(keys.FirstOrDefault().ToString()),
            _ => Alt ? FormatPlus(keys.LastOrDefault().ToString()) : FormatPlus(keys.FirstOrDefault().ToString())
        };

        string FormatPlus(string value)
        {
            return string.IsNullOrEmpty(value) ? string.Empty : value.Replace("+", " + ");
        }
    }
}