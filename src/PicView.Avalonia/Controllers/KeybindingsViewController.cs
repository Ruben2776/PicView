using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Input;
using PicView.Avalonia.Views.Main;
using PicView.Core.Favorites;
using PicView.Core.Keybindings;
using PicView.Core.Localization;
using PicView.Core.ViewModels;
using R3;
using ZLinq;
using MouseButton = PicView.Avalonia.Input.MouseButton;

namespace PicView.Avalonia.Controllers;

public class KeybindingsViewController(KeybindingsView view) : IDisposable
{
    private readonly List<KeybindCategoryGroup> _categories = [];
    private readonly List<KeybindSnapshot> _undoStack = [];
    private readonly List<KeybindSnapshot> _redoStack = [];
    private readonly Dictionary<KeybindBox, long> _boxModificationOrder = [];
    private readonly Dictionary<KeybindBox, NotifyCollectionChangedEventHandler> _collectionChangedHandlers = [];

    private KeybindSnapshot? _savedSnapshot;
    private IDisposable? _filterSubscription;
    private long _modificationSequence;
    private bool _boxesSubscribed;
    private bool _buttonsInitialized;
    private bool _isApplyingSnapshot;
    private bool _isDisposed;

    internal KeybindCategoryGroup? FavoritesCategory { get; private set; }

    public void Initialize()
    {
        if (view.DataContext is not CoreViewModel core)
        {
            return;
        }
        
        PopulateCategories();
        
        core.GlobalSettings?.Initialize();

        _filterSubscription?.Dispose();
        if (core.Keybindings is not null)
        {
            _filterSubscription = core.Keybindings.FilterText.Subscribe(ApplyFilter);
        }

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
        // Rebind the actions shown in the UI categories, keeping bindings for actions not shown there
        var keymap = Keymaps.Current;
        foreach (var category in _categories)
        {
            if (ReferenceEquals(category, FavoritesCategory))
            {
                continue;
            }

            foreach (var (_, functionName) in category.Entries)
            {
                foreach (var kb in keymap.KeybindsFor(functionName))
                {
                    keymap = keymap.Unassign(kb);
                }
            }
        }

        foreach (var category in _categories)
        {
            if (ReferenceEquals(category, FavoritesCategory))
            {
                continue;
            }

            foreach (var (box, functionName) in category.Entries)
            {
                if (box.Keybinds is null)
                {
                    continue;
                }

                foreach (var kb in box.Keybinds)
                {
                    keymap = keymap.Assign(kb, functionName);
                }
            }
        }

        Keymaps.Replace(keymap);

        var hasFavoritesCategory = FavoritesCategory is { Entries.Count: > 0 };
        if (hasFavoritesCategory)
        {
            var resolvedFavoriteHotKeys = ResolveDeduplicatedFavoriteHotKeys(FavoritesCategory!.Entries);
            for (var i = 0; i < FavoritesCategory.Entries.Count; i++)
            {
                var (_, filePath) = FavoritesCategory.Entries[i];
                FavoritesManager.SetHotKey(filePath, resolvedFavoriteHotKeys[i]);
            }

            FinalizeSaveOnUIThread(resolvedFavoriteHotKeys);
        }
        else
        {
            FinalizeSaveOnUIThread(null);
        }

        await Keymaps.SaveAsync().ConfigureAwait(false);
        if (hasFavoritesCategory)
        {
            await FavoritesManager.SaveToFileAsync().ConfigureAwait(false);
        }
    }

    private string[] ResolveDeduplicatedFavoriteHotKeys(List<(KeybindBox Box, string FunctionName)> entries)
    {
        var count = entries.Count;
        var rawHotKeys = new string[count];
        var orderIndices = new int[count];

        for (var i = 0; i < count; i++)
        {
            var box = entries[i].Box;
            rawHotKeys[i] = box.Keybinds is { Count: > 0 }
                ? box.Keybinds[^1].GetFormattedForJSON()
                : string.Empty;
            orderIndices[i] = i;
        }

        Array.Sort(orderIndices, (a, b) =>
        {
            var orderA = _boxModificationOrder.GetValueOrDefault(entries[a].Box, 0L);
            var orderB = _boxModificationOrder.GetValueOrDefault(entries[b].Box, 0L);
            var cmp = orderA.CompareTo(orderB);
            return cmp != 0 ? cmp : a.CompareTo(b);
        });

        var resolved = new string[count];
        var activeOwners = new Dictionary<string, int>(StringComparer.Ordinal);

        for (var i = 0; i < count; i++)
        {
            var idx = orderIndices[i];
            var hotKey = rawHotKeys[idx];
            if (string.IsNullOrWhiteSpace(hotKey))
            {
                resolved[idx] = string.Empty;
                continue;
            }

            if (activeOwners.TryGetValue(hotKey, out var previousIdx))
            {
                resolved[previousIdx] = string.Empty;
            }

            activeOwners[hotKey] = idx;
            resolved[idx] = hotKey;
        }

        return resolved;
    }

    private void FinalizeSaveOnUIThread(string[]? resolvedFavoriteHotKeys)
    {
        if (FavoritesCategory is not null && resolvedFavoriteHotKeys is not null)
        {
            _isApplyingSnapshot = true;
            try
            {
                for (var i = 0; i < FavoritesCategory.Entries.Count && i < resolvedFavoriteHotKeys.Length; i++)
                {
                    var (box, _) = FavoritesCategory.Entries[i];
                    var expectedHotKey = resolvedFavoriteHotKeys[i];
                    if (string.IsNullOrWhiteSpace(expectedHotKey))
                    {
                        if (box.Keybinds.Count > 0)
                        {
                            box.Keybinds.Clear();
                        }
                    }
                    else
                    {
                        var parsed = Keybind.Parse(expectedHotKey);
                        if (parsed.HasValue && (box.Keybinds.Count != 1 || box.Keybinds[0] != parsed.Value))
                        {
                            box.Keybinds.Clear();
                            box.Keybinds.Add(parsed.Value);
                        }
                    }
                }
            }
            finally
            {
                _isApplyingSnapshot = false;
            }

            var favoritesVm = ResolveFavoritesViewModel();
            if (favoritesVm is not null)
            {
                if (FavoritesManager.Count > 0)
                {
                    favoritesVm.UpdateFavorites();
                }
                else
                {
                    for (var i = 0; i < FavoritesCategory.Entries.Count && i < resolvedFavoriteHotKeys.Length; i++)
                    {
                        var (_, filePath) = FavoritesCategory.Entries[i];
                        var entryVm = favoritesVm.Entries.FirstOrDefault(e =>
                            string.Equals(e.FilePath.CurrentValue, filePath, StringComparison.Ordinal));
                        if (entryVm is not null)
                        {
                            entryVm.HotKey.Value = resolvedFavoriteHotKeys[i];
                        }
                    }
                }
            }
        }

        _boxModificationOrder.Clear();
        _savedSnapshot = new KeybindSnapshot(_categories);
        _undoStack.Clear();
        _redoStack.Clear();
        UpdateButtonStates();
    }

    private void SubscribeKeybindBoxes()
    {
        if (_boxesSubscribed || _isDisposed)
        {
            return;
        }

        _boxesSubscribed = true;
        _savedSnapshot = new KeybindSnapshot(_categories);

        foreach (var category in _categories)
        {
            foreach (var (box, _) in category.Entries)
            {
                NotifyCollectionChangedEventHandler handler = (_, _) => OnBoxKeybindChanged(box);
                _collectionChangedHandlers[box] = handler;
                box.Keybinds.CollectionChanged += handler;
            }
        }
    }

    private void InitializeButtons()
    {
        if (_buttonsInitialized || _isDisposed)
        {
            return;
        }

        _buttonsInitialized = true;
        SubscribeKeybindBoxes();

        // Wire up buttons
        view.CancelButton.Click += OnCancelClicked;
        view.DefaultButton.Click += OnResetClicked;
        view.ApplyButton.Click += OnApplyClicked;
        view.SaveButton.Click += OnSaveClicked;

        UpdateButtonStates();
    }

    private void OnBoxKeybindChanged(KeybindBox box)
    {
        if (_isApplyingSnapshot || _isDisposed)
        {
            return;
        }

        _boxModificationOrder[box] = ++_modificationSequence;
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
    
        var defaults = KeybindingManager.GetDefaultShortcuts(core.PlatformService);
        if (defaults is null)
        {
            return false;
        }

        var viewCount = 0;
        var expectedDefaultsCount = 0;

        foreach (var category in _categories)
        {
            if (ReferenceEquals(category, FavoritesCategory))
            {
                continue;
            }

            foreach (var (box, functionName) in category.Entries)
            {
                // Count how many default keybindings map to this specific UI function
                foreach (var kvp in defaults)
                {
                    if (string.Equals(kvp.Value, functionName, StringComparison.Ordinal))
                    {
                        expectedDefaultsCount++;
                    }
                }

                if (box.Keybinds is null)
                {
                    continue;
                }

                foreach (var kb in box.Keybinds)
                {
                    viewCount++;

                    // If the keybind doesn't exist in defaults, or maps to a different function, it's not default
                    if (!defaults.TryGetValue(kb, out var defaultFuncName) || 
                        !string.Equals(defaultFuncName, functionName, StringComparison.Ordinal))
                    {
                        return false;
                    }
                }
            }
        }

        // Ensure there aren't missing default bindings for the functions exposed in the UI
        return viewCount == expectedDefaultsCount;
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

        var defaults = KeybindingManager.GetDefaultShortcuts(core.PlatformService);
        if (defaults is null)
        {
            return;
        }

        // Push current state for undo
        _undoStack.Add(new KeybindSnapshot(_categories));
        _redoStack.Clear();

        // Apply default bindings to all standard boxes
        _isApplyingSnapshot = true;
        try
        {
            foreach (var category in _categories)
            {
                if (ReferenceEquals(category, FavoritesCategory))
                {
                    continue;
                }

                foreach (var (box, functionName) in category.Entries)
                {
                    box.Keybinds.Clear();
                    var matches = defaults.Where(x =>
                            string.Equals(x.Value, functionName, StringComparison.Ordinal))
                        .AsValueEnumerable().Where(x => x.Value is not null);
                    foreach (var match in matches)
                    {
                        box.Keybinds.Add(match.Key);
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

        PopulateFavoritesCategory(translation, pressKeyText);

        SubscribeKeybindBoxes();
    }

    private void PopulateFavoritesCategory(LanguageModel translation, string? pressKeyText)
    {
        var favoritesVm = ResolveFavoritesViewModel();
        if (favoritesVm is null || favoritesVm.Entries.Count == 0)
        {
            view.FavoritesHeader.IsVisible = false;
            view.FavoritesItems.IsVisible = false;
            view.FavoritesItems.Items.Clear();
            FavoritesCategory = null;
            return;
        }

        view.FavoritesHeader.Text = translation.Favorites;
        view.FavoritesHeader.IsVisible = true;
        view.FavoritesItems.IsVisible = true;
        view.FavoritesItems.Items.Clear();

        var boxEntries = new List<(KeybindBox Box, string FunctionName)>(favoritesVm.Entries.Count);
        foreach (var entry in favoritesVm.Entries)
        {
            var keybindBox = new KeybindBox
            {
                ActionName = entry.FolderName.CurrentValue,
                PlaceholderText = pressKeyText,
                MaxTags = 1
            };

            var hotKey = entry.HotKey.CurrentValue;
            if (!string.IsNullOrWhiteSpace(hotKey))
            {
                var parsed = Keybind.Parse(hotKey);
                if (parsed.HasValue && (parsed.Value.Key != Key.None || parsed.Value.MouseButton != MouseButton.None))
                {
                    keybindBox.Keybinds = [parsed.Value];
                }
            }

            view.FavoritesItems.Items.Add(keybindBox);
            boxEntries.Add((keybindBox, entry.FilePath.CurrentValue));
        }

        FavoritesCategory = new KeybindCategoryGroup(view.FavoritesHeader, view.FavoritesItems, boxEntries);
        _categories.Add(FavoritesCategory);
    }

    private FavoritesViewModel? ResolveFavoritesViewModel()
    {
        if (view.DataContext is not CoreViewModel core)
        {
            return null;
        }

        var mainWindowVm = core.MainWindows.ActiveWindow.CurrentValue
                           ?? (core.MainWindows.MainWindows.Count > 0 ? core.MainWindows.MainWindows[0] : null);
        if (mainWindowVm is null)
        {
            return null;
        }

        if (mainWindowVm.Favorites is null && FavoritesManager.Count > 0)
        {
            mainWindowVm.Favorites = new FavoritesViewModel(mainWindowVm);
        }

        if (mainWindowVm.Favorites is not null &&
            mainWindowVm.Favorites.Entries.Count == 0 &&
            FavoritesManager.Count > 0)
        {
            mainWindowVm.Favorites.UpdateFavorites();
        }

        return mainWindowVm.Favorites;
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

            var currentBinds = Keymaps.Current.KeybindsFor(functionName);
            if (currentBinds.Count > 0)
            {
                keybindBox.Keybinds = new ObservableCollection<Keybind>(currentBinds);
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
    
    public void Dispose()
    {
        _isDisposed = true;

        _filterSubscription?.Dispose();
        _filterSubscription = null;

        view.CancelButton.Click -= OnCancelClicked;
        view.DefaultButton.Click -= OnResetClicked;
        view.ApplyButton.Click -= OnApplyClicked;
        view.SaveButton.Click -= OnSaveClicked;

        foreach (var (box, handler) in _collectionChangedHandlers)
        {
            if (box.Keybinds is not null)
            {
                box.Keybinds.CollectionChanged -= handler;
            }
        }

        _collectionChangedHandlers.Clear();
        _boxModificationOrder.Clear();
        _categories.Clear();
        FavoritesCategory = null;
        _undoStack.Clear();
        _redoStack.Clear();
        _savedSnapshot = null;

        GC.SuppressFinalize(this);
    }
}
