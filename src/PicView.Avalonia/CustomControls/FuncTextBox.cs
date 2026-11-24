using System.ComponentModel;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Media;
using PicView.Core.Localization;
using Path = Avalonia.Controls.Shapes.Path;

namespace PicView.Avalonia.CustomControls;

/// <summary>
/// A custom text box control that extends the base TextBox class.
/// This control includes functionality for handling context menu events and pointer interactions,
/// with specific behavior when clicking into the textbox without initial focus.
/// </summary>
public class FuncTextBox : TextBox
{
    private bool _contextMenuLoaded;
    private bool _initialFocus;

    public static readonly StyledProperty<ICommand?> CommandProperty =
        AvaloniaProperty.Register<FuncTextBox, ICommand?>(nameof(Command));

    public static readonly StyledProperty<object?> CommandParameterProperty =
        AvaloniaProperty.Register<FuncTextBox, object?>(nameof(CommandParameter));

    public static readonly StyledProperty<bool> CommitOnLostFocusProperty =
        AvaloniaProperty.Register<FuncTextBox, bool>(nameof(CommitOnLostFocus));

    /// <summary>
    /// When true, <see cref="Command"/> also executes when the box loses focus.
    /// </summary>
    public bool CommitOnLostFocus
    {
        get => GetValue(CommitOnLostFocusProperty);
        set => SetValue(CommitOnLostFocusProperty, value);
    }

    public ICommand? Command
    {
        get => GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }
    public FuncTextBox()
    {
        ContextMenu = new ContextMenu();

        ContextMenu.Opening += ContextMenuOnOpening;
        ContextMenu.Opened += ContextMenuOnOpened;
        ContextMenu.Closed += ContextMenuOnClosed;
        
        GotFocus += OnGotFocus;
        LostFocus += OnLostFocus;
        KeyDown += OnKeyDown;
    }

    private void ContextMenuOnClosed(object? sender, RoutedEventArgs e)
    {
        Classes.Remove("active");
    }

    private void ContextMenuOnOpened(object? sender, RoutedEventArgs e)
    {
        Classes.Add("active");
    }

    private void OnLostFocus(object? sender, FocusChangedEventArgs e)
    {
        _initialFocus = false;
        if (CommitOnLostFocus)
        {
            ExecuteCommand();
        }
    }

    private void OnGotFocus(object? sender, FocusChangedEventArgs e)
    {
        _initialFocus = true;
    }

    private void ContextMenuOnOpening(object? sender, CancelEventArgs e)
    {
        if (!_contextMenuLoaded)
        {
            LoadContextMenu();
        }
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key is not (Key.Enter or Key.Tab))
        {
            return;
        }

        // A multi-line box inserts a new line on Enter; only Ctrl+Enter (see OnKeyDown) and Tab commit.
        if (e.Key is Key.Enter && AcceptsReturn)
        {
            return;
        }

        ExecuteCommand();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        // TextBox handles Enter itself when AcceptsReturn is true, so the KeyDown event never reaches OnKeyDown above.
        if (e.Key is Key.Enter && AcceptsReturn && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            ExecuteCommand();
            e.Handled = true;
            return;
        }

        base.OnKeyDown(e);
    }

    private void ExecuteCommand()
    {
        if (Command?.CanExecute(CommandParameter) == true)
        {
            Command.Execute(CommandParameter);
        }
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        // A multi-line box, such as a prompt, places the caret where it is clicked, so a typo can be fixed in place
        if (IsReadOnly || AcceptsReturn)
        {
            _initialFocus = false;
            return;
        }

        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed || !_initialFocus)
        {
            return;
        }

        // When clicking into the textbox and it didn't have focus before
        SelectAll();
        CaretIndex = Text?.Length ?? 0;
        e.Handled = true;
        _initialFocus = false;
    }

    protected override Type StyleKeyOverride => typeof(TextBox);
    
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

    private void LoadContextMenu()
    {
        if (!Application.Current.TryGetResource("MainTextColor", Application.Current.RequestedThemeVariant, out var mainTextColor))
        {
            return;
        }

        var iconBrush = new SolidColorBrush((Color)(mainTextColor ?? Brushes.White));
        if (!Application.Current.TryGetResource("CopyGeometry", Application.Current.RequestedThemeVariant, out var copyGeometry))
        {
            return;
        }

        if (!Application.Current.TryGetResource("CutGeometry", Application.Current.RequestedThemeVariant, out var cutGeometry))
        {
            return;
        }

        if (!Application.Current.TryGetResource("RecycleGeometry", Application.Current.RequestedThemeVariant, out var recycleGeometry))
        {
            return;
        }

        if (!Application.Current.TryGetResource("PasteGeometry", Application.Current.RequestedThemeVariant, out var pasteGeometry))
        {
            return;
        }

        if (!Application.Current.TryGetResource("CheckboxOutlineImage", Application.Current.RequestedThemeVariant,
                out var checkboxOutlineImage))
        {
            return;
        }
            
        var selectAllMenuItem = new MenuItem
        {
            Header = TranslationManager.Translation.SelectAll,
            Icon = new Image
            {
                Width = 12,
                Height = 12,
                Source = checkboxOutlineImage as DrawingImage ?? null
            }
        };
        selectAllMenuItem.Click += (_, _) => SelectAll();
        ContextMenu.Items.Add(selectAllMenuItem);

        var cutMenuItem = new MenuItem
        {
            Header = TranslationManager.Translation.Cut,
            Icon = new Path
            {
                Width = 12,
                Height = 12,
                Fill = iconBrush,
                Stretch = Stretch.Fill,
                Data = cutGeometry as Geometry ?? null,
            }
        };
        cutMenuItem.Click += (_, _) => Cut();
        ContextMenu.Items.Add(cutMenuItem);

        var copyMenuItem = new MenuItem
        {
            Header = TranslationManager.Translation.Copy,
            Icon = new Path
            {
                Width = 12,
                Height = 12,
                Fill = iconBrush,
                Stretch = Stretch.Fill,
                Data = copyGeometry as Geometry ?? null
            },
        };
        copyMenuItem.Click += (_, _) => Copy();
        ContextMenu.Items.Add(copyMenuItem);

        var pasteMenuItem = new MenuItem
        {
            Header = TranslationManager.Translation.FilePaste,
            Icon = new Path
            {
                Width = 12,
                Height = 12,
                Fill = iconBrush,
                Stretch = Stretch.Fill,
                Data = pasteGeometry as Geometry ?? null
            }
        };
        pasteMenuItem.Click += (_, _) => Paste();
        ContextMenu.Items.Add(pasteMenuItem);

        var deleteMenuItem = new MenuItem
        {
            Header = TranslationManager.Translation.Clear,
            Icon = new Path
            {
                Width = 12,
                Height = 12,
                Fill = iconBrush,
                Stretch = Stretch.Fill,
                Data = recycleGeometry as Geometry ?? null
            }
        };
        deleteMenuItem.Click += (_, _) => Clear();
        ContextMenu.Items.Add(deleteMenuItem);

        ContextMenu.Opened += delegate
        {
            if (IsReadOnly)
            {
                deleteMenuItem.IsEnabled = false;
                cutMenuItem.IsEnabled = false;
                pasteMenuItem.IsEnabled = false;
            }
            else
            {
                deleteMenuItem.IsEnabled = true;
                cutMenuItem.IsEnabled = true;
                pasteMenuItem.IsEnabled = true;
            }
        };
            
        _contextMenuLoaded = true;
    }

    protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromLogicalTree(e);
        GotFocus -= OnGotFocus;
        LostFocus -= OnLostFocus;
        KeyDown -= OnKeyDown;
        ContextMenu.Opened -= ContextMenuOnOpened;
        ContextMenu.Closed -= ContextMenuOnClosed;
        ContextMenu.Opening -= ContextMenuOnOpening;
    }
}