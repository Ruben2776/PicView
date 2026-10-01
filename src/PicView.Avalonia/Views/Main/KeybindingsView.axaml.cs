using System.Collections.ObjectModel;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Input;
using PicView.Core.Keybindings;
using PicView.Core.Localization;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Avalonia.Views.Main;

public partial class KeybindingsView : UserControl
{
    private readonly List<KeybindCategoryGroup> _categories = [];
    private IDisposable? _filterSubscription;
    private bool _isPopulated;

    // Change tracking
    private KeybindSnapshot? _savedSnapshot;
    private readonly List<KeybindSnapshot> _undoStack = [];
    private readonly List<KeybindSnapshot> _redoStack = [];
    private bool _isApplyingSnapshot;
    private bool _buttonsInitialized;

    public KeybindingsView()
    {
        InitializeComponent();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        if (_isPopulated)
        {
            return;
        }

        if (DataContext is not CoreViewModel core)
        {
            return;
        }

        var translation = TranslationManager.Translation;
        var pressKeyText = translation.PressKey;

        PopulateCategory(NavigationHeader, NavigationItems,
            KeybindCategoryDefinition.GetNavigationEntries(translation), pressKeyText);

        PopulateCategory(ScrollAndRotateHeader, ScrollAndRotateItems,
            KeybindCategoryDefinition.GetScrollAndRotateEntries(translation), pressKeyText);

        PopulateCategory(ZoomHeader, ZoomItems,
            KeybindCategoryDefinition.GetZoomEntries(translation), pressKeyText);

        PopulateCategory(ImageControlHeader, ImageControlItems,
            KeybindCategoryDefinition.GetImageControlEntries(translation), pressKeyText);

        PopulateCategory(InterfaceConfigurationHeader, InterfaceConfigurationItems,
            KeybindCategoryDefinition.GetInterfaceConfigurationEntries(translation), pressKeyText);

        PopulateCategory(FileManagementHeader, FileManagementItems,
            KeybindCategoryDefinition.GetFileManagementEntries(translation), pressKeyText);

        PopulateCategory(SortFilesByHeader, SortFilesByItems,
            KeybindCategoryDefinition.GetSortFilesEntries(translation), pressKeyText);

        PopulateCategory(CopyHeader, CopyItems,
            KeybindCategoryDefinition.GetCopyEntries(translation), pressKeyText);

        PopulateCategory(TabManagementHeader, TabManagementItems,
            KeybindCategoryDefinition.GetTabManagementEntries(translation), pressKeyText);

        PopulateCategory(ToolWindowsHeader, ToolWindowsItems,
            KeybindCategoryDefinition.GetToolWindowsEntries(translation), pressKeyText);

        PopulateCategory(WindowManagementHeader, WindowManagementItems,
            KeybindCategoryDefinition.GetWindowManagementEntries(translation), pressKeyText);

        PopulateCategory(WindowScalingHeader, WindowScalingItems,
            KeybindCategoryDefinition.GetWindowScalingEntries(translation), pressKeyText);

        PopulateCategory(SetStarRatingHeader, SetStarRatingItems,
            KeybindCategoryDefinition.GetStarRatingEntries(translation), pressKeyText);

        // Subscribe to filter text changes
        if (core.Keybindings is not null)
        {
            _filterSubscription = core.Keybindings.FilterText
                .Subscribe(ApplyFilter);
        }

        _isPopulated = true;

        // Defer button setup until after the window has been shown
        Dispatcher.UIThread.Post(InitializeButtons, DispatcherPriority.Background);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _filterSubscription?.Dispose();
        _filterSubscription = null;
    }

    #region Button initialization (deferred)

    private void InitializeButtons()
    {
        if (_buttonsInitialized)
        {
            return;
        }

        _buttonsInitialized = true;

        // Take the initial saved snapshot
        _savedSnapshot = new KeybindSnapshot(_categories);

        // Subscribe to keybind changes on every box for change tracking
        foreach (var category in _categories)
        {
            foreach (var (box, _) in category.Entries)
            {
                box.Keybinds.CollectionChanged += (_, _) => OnKeybindChanged();
            }
        }

        // Wire up buttons
        CancelButton.Click += (_, _) => OnCancelClicked();
        DefaultButton.Click += (_, _) => OnResetClicked();
        ApplyButton.Click += async (_, _) => await OnApplyClicked().ConfigureAwait(false);
        SaveButton.Click += async (_, _) => await OnSaveClicked().ConfigureAwait(false);

        // Ctrl+Z / Ctrl+Y for undo/redo
        KeyDown += OnViewKeyDown;

        UpdateButtonStates();
    }

    #endregion

    #region Change tracking & Undo/Redo

    private void OnKeybindChanged()
    {
        if (_isApplyingSnapshot)
        {
            return;
        }

        var current = new KeybindSnapshot(_categories);

        // Only push to undo if actually different from the last snapshot
        if (_undoStack.Count > 0 && _undoStack[^1].Equals(current))
        {
            return;
        }

        _undoStack.Add(current);
        _redoStack.Clear();
        UpdateButtonStates();
    }

    private void Undo()
    {
        if (_undoStack.Count == 0)
        {
            return;
        }

        // Save current state to redo stack
        var current = new KeybindSnapshot(_categories);
        _redoStack.Add(current);

        // Pop and apply the previous state
        var previous = _undoStack[^1];
        _undoStack.RemoveAt(_undoStack.Count - 1);
        ApplySnapshot(previous);
        UpdateButtonStates();
    }

    private void Redo()
    {
        if (_redoStack.Count == 0)
        {
            return;
        }

        // Save current state to undo stack
        var current = new KeybindSnapshot(_categories);
        _undoStack.Add(current);

        // Pop and apply the next state
        var next = _redoStack[^1];
        _redoStack.RemoveAt(_redoStack.Count - 1);
        ApplySnapshot(next);
        UpdateButtonStates();
    }

    private void ApplySnapshot(KeybindSnapshot snapshot)
    {
        _isApplyingSnapshot = true;
        try
        {
            foreach (var category in _categories)
            {
                foreach (var (box, functionName) in category.Entries)
                {
                    if (!snapshot.Bindings.TryGetValue(functionName, out var keybinds))
                    {
                        box.Keybinds.Clear();
                        continue;
                    }

                    // Clear and repopulate
                    box.Keybinds.Clear();
                    foreach (var kb in keybinds)
                    {
                        box.Keybinds.Add(kb);
                    }
                }
            }
        }
        finally
        {
            _isApplyingSnapshot = false;
        }
    }

    private bool HasUnsavedChanges()
    {
        if (_savedSnapshot is null)
        {
            return false;
        }

        var current = new KeybindSnapshot(_categories);
        return !current.Equals(_savedSnapshot);
    }

    private static Dictionary<string, List<Keybind>> GetDefaultsByFunction(CoreViewModel core)
    {
        var defaultsByFunction = new Dictionary<string, List<Keybind>>(StringComparer.Ordinal);
        if (core.PlatformService is null)
        {
            return defaultsByFunction;
        }

        var defaults = KeybindingManager.GetDefaultShortcuts(core.PlatformService);
        if (defaults is null)
        {
            return defaultsByFunction;
        }

        foreach (var kvp in defaults)
        {
            if (!defaultsByFunction.TryGetValue(kvp.Value, out var list))
            {
                list = [];
                defaultsByFunction[kvp.Value] = list;
            }

            list.Add(kvp.Key);
        }

        return defaultsByFunction;
    }


    private bool AreCurrentBindingsDefault()
    {
        if (DataContext is not CoreViewModel core)
        {
            return false;
        }

        var defaultsByFunction = GetDefaultsByFunction(core);

        foreach (var category in _categories)
        {
            foreach (var (box, functionName) in category.Entries)
            {
                defaultsByFunction.TryGetValue(functionName, out var defaultBinds);
                var defaultCount = defaultBinds?.Count ?? 0;
                var currentCount = box.Keybinds?.Count ?? 0;

                if (currentCount != defaultCount)
                {
                    return false;
                }

                if (defaultBinds is not null && box.Keybinds is not null)
                {
                    for (var i = 0; i < defaultBinds.Count; i++)
                    {
                        if (box.Keybinds[i] != defaultBinds[i])
                        {
                            return false;
                        }
                    }
                }
            }
        }

        return true;
    }

    private void UpdateButtonStates()
    {
        var hasChanges = HasUnsavedChanges();
        var isDefault = AreCurrentBindingsDefault();

        ApplyButton.IsEnabled = hasChanges;
        SaveButton.IsEnabled = hasChanges;
        DefaultButton.IsEnabled = !isDefault;
    }

    #endregion

    #region Button handlers

    private void OnCancelClicked()
    {
        if (HasUnsavedChanges())
        {
            // Reset to saved state
            if (_savedSnapshot is null)
            {
                return;
            }

            _undoStack.Add(new KeybindSnapshot(_categories));
            _redoStack.Clear();
            ApplySnapshot(_savedSnapshot);
            UpdateButtonStates();
        }
        else
        {
            SafeClose();
        }
    }

    private void OnResetClicked()
    {
        if (DataContext is not CoreViewModel core)
        {
            return;
        }

        var defaultsByFunction = GetDefaultsByFunction(core);

        // Push current state for undo
        _undoStack.Add(new KeybindSnapshot(_categories));
        _redoStack.Clear();

        // Apply default bindings to all boxes
        _isApplyingSnapshot = true;
        try
        {
            foreach (var category in _categories)
            {
                foreach (var (box, functionName) in category.Entries)
                {
                    box.Keybinds.Clear();
                    if (!defaultsByFunction.TryGetValue(functionName, out var defaultBinds))
                    {
                        continue;
                    }

                    foreach (var keybind in defaultBinds)
                    {
                        box.Keybinds.Add(keybind);
                    }
                }
            }
        }
        finally
        {
            _isApplyingSnapshot = false;
        }

        UpdateButtonStates();
    }

    private async Task OnApplyClicked()
    {
        await SaveKeybindings().ConfigureAwait(false);
        Dispatcher.UIThread.Post(UpdateButtonStates);
    }

    private async Task OnSaveClicked()
    {
        await SaveKeybindings().ConfigureAwait(false);
        Dispatcher.UIThread.Post(SafeClose);
    }

    private async Task SaveKeybindings()
    {
        // Build key-value dictionary from UI
        var keyValues = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var category in _categories)
        {
            foreach (var (box, functionName) in category.Entries)
            {
                if (box.Keybinds is null)
                {
                    continue;
                }

                foreach (var kb in box.Keybinds)
                {
                    keyValues[kb.ToString()] = functionName;
                }
            }
        }

        // Update KeybindingManager's live shortcuts
        if (KeybindingManager.CustomShortcuts is not null)
        {
            KeybindingManager.CustomShortcuts.Clear();
        }
        KeybindingManager.PopulateCustomShortcuts(keyValues);

        // Serialize and save to file
        var json = JsonSerializer.Serialize(
            keyValues,
            typeof(Dictionary<string, string>),
            SourceGenerationContext.Default).Replace("\\u002B", "+", StringComparison.Ordinal);

        await KeybindingFunctions.SaveKeyBindingsFile(json).ConfigureAwait(false);

        // Update saved snapshot on UI thread
        Dispatcher.UIThread.Post(() =>
        {
            _savedSnapshot = new KeybindSnapshot(_categories);
            _undoStack.Clear();
            _redoStack.Clear();
        });
    }

    #endregion

    #region Keyboard shortcuts

    private void OnViewKeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Z when e.KeyModifiers == KeyModifiers.Control:
                Undo();
                e.Handled = true;
                break;
            case Key.Y when e.KeyModifiers == KeyModifiers.Control:
                Redo();
                e.Handled = true;
                break;
        }
    }

    #endregion

    #region Populate & Filter

    private void PopulateCategory(
        TextBlock header,
        ItemsControl container,
        List<(string FunctionName, string DisplayName)> entries,
        string? pressKeyText)
    {
        var boxEntries = new List<(KeybindBox Box, string FunctionName)>(entries.Count);

        foreach (var (functionName, displayName) in entries)
        {
            var keybindBox = new KeybindBox
            {
                ActionName = displayName,
                PlaceholderText = pressKeyText,
            };

            // Look up current keybinds from KeybindingManager
            if (KeybindingManager.CustomShortcuts is not null)
            {
                var currentBinds = new ObservableCollection<Keybind>();
                foreach (var kvp in KeybindingManager.CustomShortcuts)
                {
                    if (string.Equals(kvp.Value, functionName, StringComparison.Ordinal))
                    {
                        currentBinds.Add(kvp.Key);
                    }
                }

                if (currentBinds.Count > 0)
                {
                    keybindBox.Keybinds = currentBinds;
                }
            }

            container.Items.Add(keybindBox);
            boxEntries.Add((keybindBox, functionName));
        }

        _categories.Add(new KeybindCategoryGroup(header, container, boxEntries));
    }

    private void ApplyFilter(string filterText)
    {
        var hasFilter = !string.IsNullOrWhiteSpace(filterText);

        foreach (var category in _categories)
        {
            var anyCategoryMatch = false;

            foreach (var (box, functionName) in category.Entries)
            {
                if (!hasFilter)
                {
                    box.IsVisible = true;
                    anyCategoryMatch = true;
                    continue;
                }

                var matches = MatchesFilter(box, functionName, filterText);
                box.IsVisible = matches;
                if (matches)
                {
                    anyCategoryMatch = true;
                }
            }

            category.Header.IsVisible = anyCategoryMatch;
            category.Container.IsVisible = anyCategoryMatch;
        }
    }

    private static bool MatchesFilter(KeybindBox box, string functionName, string filterText)
    {
        // Match against display name
        if (box.ActionName is not null &&
            box.ActionName.Contains(filterText, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Match against function name
        if (functionName.Contains(filterText, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Match against keybind tag strings
        if (box.Tags is null)
        {
            return false;
        }

        for (var i = 0; i < box.Tags.Count; i++)
        {
            if (box.Tags[i].Contains(filterText, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    #endregion

    #region Window helpers

    private void SafeClose()
    {
        Dispatcher.Invoke(() =>
        {
            if (TopLevel.GetTopLevel(this) is not Window window)
            {
                return;
            }
            window.Close();
        });
    }

    private void MoveWindow(object? sender, PointerPressedEventArgs e)
    {
        if (TopLevel.GetTopLevel(this) is Window window)
        {
            window.BeginMoveDrag(e);
        }
    }

    #endregion
}
