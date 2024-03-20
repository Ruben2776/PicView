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
            Text = GetFunctionKey();
        };
    }

    private void KeyUpHandler(object? sender, KeyEventArgs e)
    {
        // TODO: figure out how to change focus to the next control or clear focus
        if (!this.TryFindResource("MainTextColor", ThemeVariant.Default, out var color))
            return;
        var foreground = new SolidColorBrush((Color)(color ?? Color.Parse("#FFf6f4f4")));
        Foreground = foreground;
        Text = GetFunctionKey();
    }

    private async Task KeyDownHandler(object? sender, KeyEventArgs e)
    {
        await AssociateKey(e);
    }

    private async Task AssociateKey(KeyEventArgs e)
    {
        KeybindingsHelper.CustomShortcuts.Remove(e.Key);

        if (e.Key == Key.Escape)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                Text = string.Empty;
            });

            return;
        }

        var function = await KeybindingsHelper.GetFunctionByName(MethodName);

        // Handle whether it's an alternative key or not
        if (Alt)
        {
            if (KeybindingsHelper.CustomShortcuts.ContainsValue(function))
            {
                // If the main key is not present, add a new entry with the alternative key
                var altKey = (Key)Enum.Parse(typeof(Key), Text);
                KeybindingsHelper.CustomShortcuts[altKey] = function;
            }
            else
            {
                // Update the key and function name in the CustomShortcuts dictionary
                KeybindingsHelper.CustomShortcuts[e.Key] = function;
            }
        }
        else
        {
            // Remove if it already contains
            if (KeybindingsHelper.CustomShortcuts.ContainsValue(function))
            {
                Remove();
            }
            KeybindingsHelper.CustomShortcuts[e.Key] = function;
        }

        return;

        void Remove()
        {
            var prevKey = KeybindingsHelper.CustomShortcuts.FirstOrDefault(x => x.Value == function).Key;
            KeybindingsHelper.CustomShortcuts.Remove(prevKey);
        }
    }

    private string GetFunctionKey()
    {
        if (string.IsNullOrEmpty(MethodName))
        {
            return string.Empty;
        }

        // Find the key associated with the specified function
        var keys = KeybindingsHelper.CustomShortcuts.Where(x => x.Value?.Method?.Name == MethodName)?.Select(x => x.Key).ToList() ?? null;

        return keys.Count switch
        {
            <= 0 => string.Empty,
            1 => Alt ? string.Empty : keys.FirstOrDefault().ToString(),
            _ => Alt ? keys.LastOrDefault().ToString() : keys.FirstOrDefault().ToString()
        };
    }
}