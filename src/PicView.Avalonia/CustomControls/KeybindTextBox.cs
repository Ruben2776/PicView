using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using PicView.Avalonia.Functions;
using PicView.Avalonia.Input;
using PicView.Core.DebugTools;
using PicView.Core.Localization;
using R3;

namespace PicView.Avalonia.CustomControls;

/// <summary>
/// A custom TextBox control for managing key bindings.
/// </summary>
public class KeybindTextBox : TextBox
{
    public static readonly AvaloniaProperty<KeyGesture?> KeybindProperty =
        AvaloniaProperty.Register<KeybindTextBox, KeyGesture?>(nameof(Keybind));

    public static readonly AvaloniaProperty<string?> MethodNameProperty =
        AvaloniaProperty.Register<KeybindTextBox, string?>(nameof(MethodName));

    public static readonly AvaloniaProperty<bool?> AltProperty =
        AvaloniaProperty.Register<KeybindTextBox, bool?>(nameof(Alt));
    
    private DisposableBag _disposables;

    public KeybindTextBox()
    {
        SetupKeyEventHandlers();
        Loaded += OnLoaded;
        GotFocus += OnGotFocus;
        LostFocus += OnLostFocus;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        SubscribeToMethodNameChanges();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _disposables.Dispose();
    }

    protected override Type StyleKeyOverride => typeof(TextBox);

    public KeyGesture? Keybind
    {
        get => GetValue(KeybindProperty) as KeyGesture;
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

    private void SubscribeToMethodNameChanges()
    {
        if (string.IsNullOrEmpty(MethodName))
        {
            return;
        }
        this.GetObservable(MethodNameProperty).ToObservable()
            .Subscribe(_ =>
            {
                Text = FunctionsKeyHelper.GetFunctionKeyName(MethodName, IsReadOnly, Alt);
            }, DebugHelper.LogError(nameof(KeybindTextBox), nameof(FunctionsKeyHelper.GetFunctionKeyName)))
            .AddTo(ref _disposables);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        if (change.Property == IsReadOnlyProperty)
        {
            if (IsReadOnly)
            {
                PseudoClasses.Add(":readonly");
            }
            else
            {
                PseudoClasses.Remove(":readonly");
            }
        }
        base.OnPropertyChanged(change);
    }

    private void SetupKeyEventHandlers()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            // On macOS, we only get KeyUp because the option to select a different character
            // when a key is held down interferes with keyboard shortcuts
            var keyUp = Observable.FromEventHandler<KeyEventArgs>(handler => KeyUp += handler, handler => KeyUp -= handler);
            keyUp.Select(e => e.e)
                .SubscribeAwait(async (e, _) =>
                {
                    await AssociateKey(e);
                }, DebugHelper.LogError(nameof(KeybindTextBox), nameof(AssociateKey)))
                .AddTo(ref _disposables);
        }
        else
        {
            var keyDown = Observable.FromEventHandler<KeyEventArgs>(handler => KeyDown += handler, handler => KeyDown -= handler);
            keyDown.Select(e => e.e)
                .SubscribeAwait(async (e, _) =>
                {
                    await AssociateKey(e);
                }, DebugHelper.LogError(nameof(KeybindTextBox), nameof(AssociateKey)))
                .AddTo(ref _disposables);
            
            var keyUp = Observable.FromEventHandler<KeyEventArgs>(handler => KeyUp += handler, handler => KeyUp -= handler);
            keyUp.Select(e => e.e)
                .Subscribe(_ =>
                {
                    KeyUpHandler();
                }, DebugHelper.LogError(nameof(KeybindTextBox), nameof(KeyUpHandler)))
                .AddTo(ref _disposables);
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        // Disable keyboard behavior #248
        
        // Fix tab
        if (e.Key == Key.Tab)
        {
            _ = AssociateKey(e);
        }
    }

    private void OnGotFocus(object? sender, FocusChangedEventArgs e)
    {
        if (IsReadOnly)
        {
            ApplyReadOnlyBorderColor();
            return;
        }

        ApplyEditableForegroundColor();
        Text = TranslationManager.Translation.PressKey;
        CaretIndex = 0;
        MainKeyboardShortcuts.IsEscKeyEnabled = false;
    }

    private void OnLostFocus(object? sender, RoutedEventArgs e)
    {
        ApplyDefaultForegroundColor();
        Text = FunctionsKeyHelper.GetFunctionKeyName(MethodName, IsReadOnly, Alt);
        MainKeyboardShortcuts.IsEscKeyEnabled = true;
    }

    private void KeyUpHandler()
    {
        ApplyDefaultForegroundColor();
        Text = FunctionsKeyHelper.GetFunctionKeyName(MethodName, IsReadOnly, Alt);
    }

    private void ApplyReadOnlyBorderColor()
    {
        if (this.TryFindResource("MainBorderColor", ThemeVariant.Default, out var borderColor))
        {
            var borderBrush = new SolidColorBrush((Color)(borderColor ?? Color.Parse("#FFf6f4f4")));
            BorderBrush = borderBrush;
        }
    }

    private void ApplyEditableForegroundColor()
    {
        if (this.TryFindResource("MainTextColorFaded", ThemeVariant.Default, out var color))
        {
            Foreground = new SolidColorBrush((Color)(color ?? Color.Parse("#d6d4d4")));
        }
    }

    private void ApplyDefaultForegroundColor()
    {
        if (this.TryFindResource("MainTextColor", ThemeVariant.Default, out var color))
        {
            Foreground = new SolidColorBrush((Color)(color ?? Color.Parse("#FFf6f4f4")));
        }
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
        
        if (string.IsNullOrEmpty(MethodName))
        {
            return;
        }

        if (e.Key == Key.Escape)
        {
            e.Handled = true;
            MainKeyboardShortcuts.IsEscKeyEnabled = false;

            await Dispatcher.UIThread.InvokeAsync(() => { Text = string.Empty; });

            Remove();
            await Save();

            return;
        }

        // Handle whether it's an alternative key or not
        if (Alt)
        {
            if (KeybindingManager.CustomShortcuts.ContainsValue(MethodName))
            {
                // If the main key is not present, add a new entry with the alternative key
                var altKey = (Key)Enum.Parse(typeof(Key), e.Key.ToString());
                var keyGesture = new KeyGesture(altKey, e.KeyModifiers);
                KeybindingManager.CustomShortcuts[keyGesture] = MethodName;
            }
            else
            {
                // Update the key and function name in the CustomShortcuts dictionary
                var keyGesture = new KeyGesture(e.Key, e.KeyModifiers);
                KeybindingManager.CustomShortcuts[keyGesture] = MethodName;
            }
        }
        else
        {
            // Remove if it already contains
            if (KeybindingManager.CustomShortcuts.ContainsValue(MethodName))
            {
                Remove();
            }

            var keyGesture = new KeyGesture(e.Key, e.KeyModifiers);
            KeybindingManager.CustomShortcuts[keyGesture] = MethodName;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            KeyUpHandler();
        }

        await Save();
        return;

        async Task Save()
        {
            await KeybindingManager.UpdateKeyBindingsFile();
        }

        void Remove()
        {
            var keys = KeybindingManager.CustomShortcuts.Where(x => x.Value == MethodName)
                .Select(x => x.Key).ToArray();
            
            if (keys.Length > 0)
            {
                KeybindingManager.CustomShortcuts.Remove(Alt ? keys.LastOrDefault() : keys.FirstOrDefault());
            }
        }
    }

    protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromLogicalTree(e);
        _disposables.Dispose();
        Loaded -= OnLoaded;
        GotFocus -= OnGotFocus;
        LostFocus -= OnLostFocus;
    }
}