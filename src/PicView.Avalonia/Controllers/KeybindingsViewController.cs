using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Input;
using PicView.Avalonia.Views.Main;
using PicView.Core.Keybindings;
using PicView.Core.Localization;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Avalonia.Controllers;

public class KeybindingsViewController(KeybindingsView view) : IDisposable
{
    private readonly List<KeybindCategoryGroup> _categories = [];
    private readonly List<KeybindSnapshot> _undoStack = [];
    private readonly List<KeybindSnapshot> _redoStack = [];

    private KeybindSnapshot? _savedSnapshot;
    private IDisposable? _filterSubscription;
    private bool _buttonsInitialized;
    private bool _isApplyingSnapshot;
    private bool _isDisposed;

    public void Initialize()
    {
        if (view.DataContext is not CoreViewModel core)
        {
            return;
        }
        
        PopulateCategories();
        
        core.GlobalSettings.Initialize();

        _filterSubscription?.Dispose();
        _filterSubscription = core.Keybindings.FilterText.Subscribe(ApplyFilter);

        // Defer button setup until after the window has been shown
        Dispatcher.UIThread.Post(() =>
        {
            InitializeButtons();
            core.Keybindings.IsLoading.Value = false;
        }, DispatcherPriority.Background);
    }

    public void HandleKeyPressed(KeyEventArgs e)
    {
        var isSearchFocused = view.FilterBox.IsFocused || view.FilterBox.IsKeyboardFocusWithin;

        // Handle Escape
        if (e.Key is Key.Escape)
        {
            if (isSearchFocused)
            {
                if (view.FilterBox.Text?.Length > 0)
                {
                    view.FilterBox.Clear();
                }
                else
                {
                    TopLevel.GetTopLevel(view)?.FocusManager?.Focus(null);
                }

                e.Handled = true;
                return;
            }
            SafeClose();
        }

        var isCtrl = OperatingSystem.IsMacOS()
            ? e.KeyModifiers.HasFlag(KeyModifiers.Meta)
            : e.KeyModifiers.HasFlag(KeyModifiers.Control);

        // Handle Ctrl+F or '?' to focus search
        if ((e.Key is Key.F && isCtrl) || (!isSearchFocused && e.Key is Key.OemQuestion))
        {
            view.FilterBox.Focus();
            e.Handled = true;
            return;
        }

        switch (e.Key)
        {
            case Key.Z when isCtrl && !e.KeyModifiers.HasFlag(KeyModifiers.Shift):
                Undo();
                e.Handled = true;
                break;
            case Key.Y when isCtrl:
            case Key.Z when isCtrl && e.KeyModifiers.HasFlag(KeyModifiers.Shift):
                Redo();
                e.Handled = true;
                break;
        }
    }

    public void Undo()
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

    public void Redo()
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

    public bool HasUnsavedChanges()
    {
        if (_savedSnapshot is null)
        {
            return false;
        }

        var current = new KeybindSnapshot(_categories);
        return !current.Equals(_savedSnapshot);
    }

    public void UpdateButtonStates()
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(UpdateButtonStates);
            return;
        }

        var hasChanges = HasUnsavedChanges();
        var isDefault = AreCurrentBindingsDefault();

        view.ApplyButton.IsEnabled = hasChanges;
        view.SaveButton.IsEnabled = hasChanges;
        view.DefaultButton.IsEnabled = !isDefault;
    }

    public async Task SaveKeybindings()
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
                    keyValues[kb.GetFormattedForJSON()] = functionName;
                }
            }
        }

        // Update KeybindingManager's live shortcuts
        if (KeybindingManager.CustomShortcuts is not null)
        {
            KeybindingManager.CustomShortcuts.Clear();
        }
        KeybindingManager.PopulateCustomShortcuts(keyValues);
        await KeybindingManager.UpdateKeyBindingsFile().ConfigureAwait(false);

        // Update saved snapshot on UI thread
        Dispatcher.UIThread.Post(() =>
        {
            _savedSnapshot = new KeybindSnapshot(_categories);
            _undoStack.Clear();
            _redoStack.Clear();
            UpdateButtonStates();
        });
    }

    private void InitializeButtons()
    {
        if (_buttonsInitialized || _isDisposed)
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
                box.Keybinds.CollectionChanged += OnKeybindCollectionChanged;
            }
        }

        // Wire up buttons
        view.CancelButton.Click += OnCancelClicked;
        view.DefaultButton.Click += OnResetClicked;
        view.ApplyButton.Click += OnApplyClicked;
        view.SaveButton.Click += OnSaveClicked;

        UpdateButtonStates();
    }

    private void OnKeybindCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnKeybindChanged();
    }

    private void OnKeybindChanged()
    {
        if (_isApplyingSnapshot || _isDisposed)
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

    private bool AreCurrentBindingsDefault()
    {
        if (view.DataContext is not CoreViewModel core)
        {
            return false;
        }

        var defaultsByFunction = KeybindingManager.GetDefaultsByFunction(core);

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

                if (defaultBinds is null || box.Keybinds is null)
                {
                    continue;
                }

                for (var i = 0; i < defaultBinds.Count; i++)
                {
                    if (box.Keybinds[i] != defaultBinds[i])
                    {
                        return false;
                    }
                }
            }
        }

        return true;
    }

    private void OnCancelClicked(object? sender, RoutedEventArgs e)
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

    private void OnResetClicked(object? sender, RoutedEventArgs e)
    {
        if (view.DataContext is not CoreViewModel core)
        {
            return;
        }

        var defaultsByFunction = KeybindingManager.GetDefaultsByFunction(core);

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

    private void OnApplyClicked(object? sender, RoutedEventArgs e)
    {
        _ = SaveKeybindings().ConfigureAwait(false);
    }

    private void OnSaveClicked(object? sender, RoutedEventArgs e)
    {
        _ = SaveKeybindings().ConfigureAwait(false);
        SafeClose();
    }

    private void SafeClose()
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (TopLevel.GetTopLevel(view) is not Window window)
            {
                return;
            }
            window.Close();
        });
    }

    private void PopulateCategories()
    {
        if (_categories.Count > 0)
        {
            return;
        }

        var translation = TranslationManager.Translation;
        var pressKeyText = translation.PressKey;

        PopulateCategory(view.NavigationHeader, view.NavigationItems,
            KeybindCategoryDefinition.GetNavigationEntries(translation), pressKeyText);

        PopulateCategory(view.ScrollAndRotateHeader, view.ScrollAndRotateItems,
            KeybindCategoryDefinition.GetScrollAndRotateEntries(translation), pressKeyText);

        PopulateCategory(view.ZoomHeader, view.ZoomItems,
            KeybindCategoryDefinition.GetZoomEntries(translation), pressKeyText);

        PopulateCategory(view.ImageControlHeader, view.ImageControlItems,
            KeybindCategoryDefinition.GetImageControlEntries(translation), pressKeyText);

        PopulateCategory(view.InterfaceConfigurationHeader, view.InterfaceConfigurationItems,
            KeybindCategoryDefinition.GetInterfaceConfigurationEntries(translation), pressKeyText);

        PopulateCategory(view.FileManagementHeader, view.FileManagementItems,
            KeybindCategoryDefinition.GetFileManagementEntries(translation), pressKeyText);

        PopulateCategory(view.SortFilesByHeader, view.SortFilesByItems,
            KeybindCategoryDefinition.GetSortFilesEntries(translation), pressKeyText);

        PopulateCategory(view.CopyHeader, view.CopyItems,
            KeybindCategoryDefinition.GetCopyEntries(translation), pressKeyText);

        PopulateCategory(view.TabManagementHeader, view.TabManagementItems,
            KeybindCategoryDefinition.GetTabManagementEntries(translation), pressKeyText);

        PopulateCategory(view.ToolWindowsHeader, view.ToolWindowsItems,
            KeybindCategoryDefinition.GetToolWindowsEntries(translation), pressKeyText);

        PopulateCategory(view.WindowManagementHeader, view.WindowManagementItems,
            KeybindCategoryDefinition.GetWindowManagementEntries(translation), pressKeyText);

        PopulateCategory(view.WindowScalingHeader, view.WindowScalingItems,
            KeybindCategoryDefinition.GetWindowScalingEntries(translation), pressKeyText);

        PopulateCategory(view.SetStarRatingHeader, view.SetStarRatingItems,
            KeybindCategoryDefinition.GetStarRatingEntries(translation), pressKeyText);
    }

    private void PopulateCategory(
        TextBlock header,
        ItemsControl container,
        List<(string FunctionName, string DisplayName)> entries,
        string? pressKeyText)
    {
        container.Items.Clear();
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

    private void ApplyFilter(string? filterText)
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

                var matches = MatchesFilter(box, functionName, filterText!);
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
    
    public void Dispose()
    {
        _isDisposed = true;

        _filterSubscription?.Dispose();
        _filterSubscription = null;

        view.CancelButton.Click -= OnCancelClicked;
        view.DefaultButton.Click -= OnResetClicked;
        view.ApplyButton.Click -= OnApplyClicked;
        view.SaveButton.Click -= OnSaveClicked;

        foreach (var category in _categories)
        {
            foreach (var (box, _) in category.Entries)
            {
                if (box.Keybinds is not null)
                {
                    box.Keybinds.CollectionChanged -= OnKeybindCollectionChanged;
                }
            }
        }

        _categories.Clear();
        _undoStack.Clear();
        _redoStack.Clear();
        _savedSnapshot = null;

        GC.SuppressFinalize(this);
    }
}
