using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using PicView.Avalonia.Keybindings;
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
        AddHandler(KeyDownEvent, KeyDownHandler, RoutingStrategies.Tunnel);
        AddHandler(KeyUpEvent, KeyUpHandler, RoutingStrategies.Tunnel);
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
            Text = TranslationHelper.GetTranslation("PressKey");
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
                return;
        }

        KeybindingsHelper.CustomShortcuts.Remove(new KeyGesture(e.Key, e.KeyModifiers));

        var function = await KeybindingsHelper.GetFunctionByName(MethodName);

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
            if (KeybindingsHelper.CustomShortcuts.ContainsValue(function))
            {
                // If the main key is not present, add a new entry with the alternative key
                var altKey = (Key)Enum.Parse(typeof(Key), e.Key.ToString());
                var keyGesture = new KeyGesture(altKey, e.KeyModifiers);
                KeybindingsHelper.CustomShortcuts[keyGesture] = function;
            }
            else
            {
                // Update the key and function name in the CustomShortcuts dictionary
                var keyGesture = new KeyGesture(e.Key, e.KeyModifiers);
                KeybindingsHelper.CustomShortcuts[keyGesture] = function;
            }
        }
        else
        {
            // Remove if it already contains
            if (KeybindingsHelper.CustomShortcuts.ContainsValue(function))
            {
                Remove();
            }
            var keyGesture = new KeyGesture(e.Key, e.KeyModifiers);
            KeybindingsHelper.CustomShortcuts[keyGesture] = function;
        }

        await Save();
        return;

        async Task Save()
        {
            await KeybindingsHelper.UpdateKeyBindingsFile();
        }

        void Remove()
        {
            var prevKey = KeybindingsHelper.CustomShortcuts.FirstOrDefault(x => x.Value == function).Key;
            KeybindingsHelper.CustomShortcuts.Remove(prevKey);
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
                case "LastImage":
                    var lastKey = KeybindingsHelper.CustomShortcuts.Where(x => x.Value?.Method?.Name == "Next")
                        ?.Select(x => x.Key).ToList() ?? null;
                    return lastKey is not { Count: > 0 } ?
                        string.Empty :
                        $"{TranslationHelper.GetTranslation("Ctrl")} + {(Alt ? lastKey.LastOrDefault().ToString() : lastKey.FirstOrDefault().ToString())}";

                case "FirstImage":
                    var firstKey = KeybindingsHelper.CustomShortcuts.Where(x => x.Value?.Method?.Name == "Prev")
                        ?.Select(x => x.Key).ToList() ?? null;
                    return firstKey is not { Count: > 0 } ?
                        string.Empty :
                        $"{TranslationHelper.GetTranslation("Ctrl")} + {(Alt ? firstKey.LastOrDefault().ToString() : firstKey.FirstOrDefault().ToString())}";

                case "NextFolder":
                    var nextFolderKey = KeybindingsHelper.CustomShortcuts.Where(x => x.Value?.Method?.Name == "Next")
                        ?.Select(x => x.Key).ToList() ?? null;
                    return nextFolderKey is not { Count: > 0 } ?
                        string.Empty :
                        $"{TranslationHelper.GetTranslation("Shift")} + {(Alt ? nextFolderKey.LastOrDefault().ToString() : nextFolderKey.FirstOrDefault().ToString())}";

                case "PrevFolder":
                    var prevFolderKey = KeybindingsHelper.CustomShortcuts.Where(x => x.Value?.Method?.Name == "Prev")
                        ?.Select(x => x.Key).ToList() ?? null;
                    return prevFolderKey is not { Count: > 0 } ?
                        string.Empty :
                        $"{TranslationHelper.GetTranslation("Shift")} + {(Alt ? prevFolderKey.LastOrDefault().ToString() : prevFolderKey.FirstOrDefault().ToString())}";

                case "ScrollUpInternal":
                    var rotateRightKey = KeybindingsHelper.CustomShortcuts.Where(x => x.Value?.Method?.Name == "Up")
                        ?.Select(x => x.Key).ToList() ?? null;
                    return rotateRightKey is not { Count: > 0 } ?
                        string.Empty :
                        Alt ? rotateRightKey.LastOrDefault().ToString() : rotateRightKey.FirstOrDefault().ToString();

                case "ScrollDownInternal":
                    var rotateLeftKey = KeybindingsHelper.CustomShortcuts.Where(x => x.Value?.Method?.Name == "Down")
                        ?.Select(x => x.Key).ToList() ?? null;
                    return rotateLeftKey is not { Count: > 0 } ?
                        string.Empty :
                        Alt ? rotateLeftKey.LastOrDefault().ToString() : rotateLeftKey.FirstOrDefault().ToString();
            }
        }

        // Find the key associated with the specified function
        //var keys = KeybindingsHelper.CustomShortcuts.Where(x => x.Value?.Method?.Name == MethodName && x.Value?.Method != null).Select(x => x.Key);
        var keys = KeybindingsHelper.CustomShortcuts.Where(x => x.Value?.Method?.Name == MethodName)?.Select(x => x.Key).ToList() ?? null;

        if (keys is null)
        {
            return string.Empty;
        }
        return keys.Count switch
        {
            <= 0 => string.Empty,
            1 => Alt ? string.Empty : keys.FirstOrDefault().ToString(),
            _ => Alt ? keys.LastOrDefault().ToString() : keys.FirstOrDefault().ToString()
        };
    }
}