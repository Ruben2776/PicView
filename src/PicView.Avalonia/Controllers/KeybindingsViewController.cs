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

namespace PicView.Avalonia.Controllers;

/// <summary>
/// Edits a draft <see cref="Keymap"/> through the keybindings view. The boxes show the draft;
/// every box edit produces a new draft, and undo/redo step through previous drafts.
/// </summary>
public class KeybindingsViewController(KeybindingsView view) : IDisposable
{
    private readonly List<KeybindCategoryGroup> _categories = [];
    private readonly List<Keymap> _undoStack = [];
    private readonly List<Keymap> _redoStack = [];
    private readonly Dictionary<KeybindBox, NotifyCollectionChangedEventHandler> _collectionChangedHandlers = [];

    private Keymap _saved = Keymap.Empty;
    private Keymap _draft = Keymap.Empty;
    private IDisposable? _filterSubscription;
    private bool _boxesSubscribed;
    private bool _buttonsInitialized;
    private bool _isShowingDraft;
    private bool _isDisposed;

    internal KeybindCategoryGroup? FavoritesCategory { get; private set; }

    /// <summary>
    /// Returns the box editing the Keybinds of <paramref name="action"/>, if it is shown in a category.
    /// </summary>
    internal KeybindBox? ActionBox(string action)
    {
        foreach (var category in _categories)
        {
            if (ReferenceEquals(category, FavoritesCategory))
            {
                continue;
            }

            foreach (var (box, functionName) in category.Entries)
            {
                if (string.Equals(functionName, action, StringComparison.Ordinal))
                {
                    return box;
                }
            }
        }

        return null;
    }

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

        _redoStack.Add(_draft);
        _draft = _undoStack[^1];
        _undoStack.RemoveAt(_undoStack.Count - 1);
        ShowDraft();
        UpdateButtonStates();
    }

    public void Redo()
    {
        if (_redoStack.Count == 0)
        {
            return;
        }

        _undoStack.Add(_draft);
        _draft = _redoStack[^1];
        _redoStack.RemoveAt(_redoStack.Count - 1);
        ShowDraft();
        UpdateButtonStates();
    }

    public bool HasUnsavedChanges() => !_draft.Equals(_saved);

    public void UpdateButtonStates()
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(UpdateButtonStates);
            return;
        }

        var hasChanges = HasUnsavedChanges();

        view.ApplyButton.IsEnabled = hasChanges;
        view.SaveButton.IsEnabled = hasChanges;
        view.DefaultButton.IsEnabled = !_draft.Equals(DefaultsKeepingFavorites());
    }

    public async Task SaveKeybindings()
    {
        var draft = _draft;
        Keymaps.Replace(draft);

        // The favorites menu shows each favorite's Keybind
        ResolveFavoritesViewModel()?.UpdateFavorites();

        _saved = draft;
        _undoStack.Clear();
        _redoStack.Clear();
        UpdateButtonStates();

        await Keymaps.SaveAsync().ConfigureAwait(false);
    }

    private void SubscribeKeybindBoxes()
    {
        if (_boxesSubscribed || _isDisposed)
        {
            return;
        }

        _boxesSubscribed = true;

        foreach (var category in _categories)
        {
            foreach (var (box, action) in category.Entries)
            {
                NotifyCollectionChangedEventHandler handler = (_, _) => OnBoxEdited(box, action);
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

    private void OnBoxEdited(KeybindBox box, string action)
    {
        if (_isShowingDraft || _isDisposed)
        {
            return;
        }

        var next = _draft.WithKeybinds(action, box.Keybinds);
        if (next.Equals(_draft))
        {
            return;
        }

        _undoStack.Add(_draft);
        _redoStack.Clear();
        _draft = next;

        // The edit may have taken Keybinds from other boxes
        ShowDraft();
        UpdateButtonStates();
    }

    /// <summary>
    /// Updates every box whose Keybinds differ from the draft.
    /// </summary>
    private void ShowDraft()
    {
        _isShowingDraft = true;
        try
        {
            foreach (var category in _categories)
            {
                foreach (var (box, action) in category.Entries)
                {
                    var keybinds = _draft.KeybindsFor(action);

                    if (box.Keybinds.Count == keybinds.Count && keybinds.All(box.Keybinds.Contains))
                    {
                        continue;
                    }

                    box.Keybinds.Clear();
                    foreach (var keybind in keybinds)
                    {
                        box.Keybinds.Add(keybind);
                    }
                }
            }
        }
        finally
        {
            _isShowingDraft = false;
        }
    }

    /// <summary>
    /// The platform defaults, with the draft's favorite Keybinds taking precedence.
    /// </summary>
    private Keymap DefaultsKeepingFavorites()
    {
        var core = view.DataContext as CoreViewModel;
        var defaults = Keymaps.Defaults(core?.PlatformService);
        foreach (var action in KeybindCategoryDefinition.FavoriteActionNames)
        {
            defaults = defaults.WithKeybinds(action, _draft.KeybindsFor(action));
        }

        return defaults;
    }

    private void ReplaceDraft(Keymap keymap)
    {
        if (keymap.Equals(_draft))
        {
            return;
        }

        _undoStack.Add(_draft);
        _redoStack.Clear();
        _draft = keymap;
        ShowDraft();
        UpdateButtonStates();
    }

    private void OnCancelClicked(object? sender, RoutedEventArgs e)
    {
        if (HasUnsavedChanges())
        {
            ReplaceDraft(_saved);
        }
        else
        {
            SafeClose();
        }
    }

    private void OnResetClicked(object? sender, RoutedEventArgs e) =>
        ReplaceDraft(DefaultsKeepingFavorites());

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

        _saved = _draft = Keymaps.Current;

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

    /// <summary>
    /// Shows a box for each favorite with a slot, editing the Keybind of that slot's action.
    /// </summary>
    private void PopulateFavoritesCategory(LanguageModel translation, string? pressKeyText)
    {
        view.FavoritesItems.Items.Clear();
        var boxEntries = new List<(KeybindBox Box, string FunctionName)>();
        foreach (var entry in FavoritesManager.AllEntries)
        {
            if (entry.Slot is not { } slot)
            {
                continue;
            }

            var action = KeybindCategoryDefinition.FavoriteAction(slot);
            var folderName = Path.GetFileName(Path.TrimEndingDirectorySeparator(entry.FilePath));
            var keybindBox = new KeybindBox
            {
                ActionName = string.IsNullOrEmpty(folderName) ? entry.FilePath : folderName,
                PlaceholderText = pressKeyText,
                MaxTags = 1
            };

            var currentBinds = _draft.KeybindsFor(action);
            if (currentBinds.Count > 0)
            {
                keybindBox.Keybinds = new ObservableCollection<Keybind>(currentBinds);
            }

            view.FavoritesItems.Items.Add(keybindBox);
            boxEntries.Add((keybindBox, action));
        }

        var hasFavorites = boxEntries.Count > 0;
        view.FavoritesHeader.IsVisible = hasFavorites;
        view.FavoritesItems.IsVisible = hasFavorites;
        if (!hasFavorites)
        {
            FavoritesCategory = null;
            return;
        }

        view.FavoritesHeader.Text = translation.Favorites;
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
        return mainWindowVm?.Favorites;
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

            var currentBinds = _draft.KeybindsFor(functionName);
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
        _categories.Clear();
        FavoritesCategory = null;
        _undoStack.Clear();
        _redoStack.Clear();

        GC.SuppressFinalize(this);
    }
}
