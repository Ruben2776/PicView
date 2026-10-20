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
/// Edits a <see cref="KeymapDraft"/> through the keybindings view. The boxes show the draft,
/// and every box edit and button press is passed on to it.
/// </summary>
public class KeybindingsViewController(KeybindingsView view) : IDisposable
{
    private readonly List<KeybindCategoryGroup> _categories = [];
    private readonly Dictionary<KeybindBox, NotifyCollectionChangedEventHandler> _collectionChangedHandlers = [];

    private KeymapDraft _draft = new(Keymap.Empty, Keymap.Empty);
    private IDisposable? _filterSubscription;
    private bool _boxesSubscribed;
    private bool _buttonsInitialized;
    private bool _isShowingDraft;
    private bool _isDisposed;

    internal KeybindCategoryGroup? FavoritesCategory { get; private set; }

    /// <summary>
    /// The Contextual Actions. Their Keybinds are a separate layer, so they only conflict with each other.
    /// </summary>
    internal KeybindCategoryGroup? ContextualCategory { get; private set; }

    #if DEBUG
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

            foreach (var (box, functionName, _) in category.Entries)
            {
                if (string.Equals(functionName, action, StringComparison.Ordinal))
                {
                    return box;
                }
            }
        }

        return null;
    }
    #endif

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

    public void Undo() => Apply(_draft.Undo());

    public void Redo() => Apply(_draft.Redo());

    public bool HasUnsavedChanges() => _draft.IsDirty;

    public void UpdateButtonStates()
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(UpdateButtonStates);
            return;
        }

        view.ApplyButton.IsEnabled = _draft.IsDirty;
        view.SaveButton.IsEnabled = _draft.IsDirty;
        view.DefaultButton.IsEnabled = !_draft.IsDefault;
    }

    public async Task SaveKeybindings()
    {
        Keymaps.Replace(_draft.Current);
        _draft.MarkSaved();

        // The favorites menu shows each favorite's Keybind
        ResolveFavoritesViewModel()?.UpdateFavorites();
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
            foreach (var (box, action, _) in category.Entries)
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

        // The edit may have taken Keybinds from other boxes, which Apply shows
        Apply(_draft.SetKeybinds(action, box.Keybinds));
    }

    /// <summary>
    /// Shows the draft after a draft operation that changed it, and updates the buttons.
    /// </summary>
    /// <returns><paramref name="changed"/>.</returns>
    private bool Apply(bool changed)
    {
        if (changed)
        {
            ShowDraft();
        }

        UpdateButtonStates();
        return changed;
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
                foreach (var (box, action, _) in category.Entries)
                {
                    var keybinds = _draft.Current.KeybindsFor(action);

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

    private void OnCancelClicked(object? sender, RoutedEventArgs e)
    {
        if (!Apply(_draft.Revert()))
        {
            SafeClose();
        }
    }

    private void OnResetClicked(object? sender, RoutedEventArgs e) =>
        Apply(_draft.ResetToDefaults());

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

        var core = view.DataContext as CoreViewModel;
        _draft = new KeymapDraft(Keymaps.Current, Keymaps.Defaults(core?.PlatformService));

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

        PopulateCategory(view.ContextualHeader, view.ContextualItems,
            KeybindCategoryDefinition.GetContextualEntries(translation), pressKeyText);
        ContextualCategory = _categories[^1];

        PopulateFavoritesCategory(translation, pressKeyText);

        SubscribeKeybindBoxes();
    }

    /// <summary>
    /// Shows a box for each favorite with a slot, editing the Keybind of that slot's action.
    /// </summary>
    private void PopulateFavoritesCategory(LanguageModel translation, string? pressKeyText)
    {
        view.FavoritesItems.Items.Clear();
        var boxEntries = new List<(KeybindBox Box, string FunctionName, string DisplayName)>();
        foreach (var entry in FavoritesManager.AllEntries)
        {
            if (entry.Slot is not { } slot)
            {
                continue;
            }

            var action = KeybindCategoryDefinition.FavoriteAction(slot);
            var folderName = Path.GetFileName(Path.TrimEndingDirectorySeparator(entry.FilePath));
            var displayName = string.IsNullOrEmpty(folderName) ? entry.FilePath : folderName;
            var keybindBox = new KeybindBox
            {
                ActionName = displayName,
                PlaceholderText = pressKeyText,
                MaxTags = 1
            };

            var currentBinds = _draft.Current.KeybindsFor(action);
            if (currentBinds.Count > 0)
            {
                keybindBox.Keybinds = new ObservableCollection<Keybind>(currentBinds);
            }

            view.FavoritesItems.Items.Add(keybindBox);
            boxEntries.Add((keybindBox, action, displayName));
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
        var boxEntries = new List<(KeybindBox Box, string FunctionName, string DisplayName)>(entries.Count);

        foreach (var (functionName, displayName) in entries)
        {
            var keybindBox = new KeybindBox
            {
                ActionName = displayName,
                PlaceholderText = pressKeyText,
            };

            var currentBinds = _draft.Current.KeybindsFor(functionName);
            if (currentBinds.Count > 0)
            {
                keybindBox.Keybinds = new ObservableCollection<Keybind>(currentBinds);
            }

            container.Items.Add(keybindBox);
            boxEntries.Add((keybindBox, functionName, displayName));
        }

        _categories.Add(new KeybindCategoryGroup(header, container, boxEntries));
    }

    private void ApplyFilter(string? filterText)
    {
        var hasFilter = !string.IsNullOrWhiteSpace(filterText);

        foreach (var category in _categories)
        {
            var anyCategoryMatch = false;

            foreach (var (box, functionName, displayName) in category.Entries)
            {
                if (!hasFilter)
                {
                    box.IsVisible = true;
                    anyCategoryMatch = true;
                    continue;
                }

                var matches = KeybindFilter.Matches(filterText, functionName, displayName, KeybindTexts(functionName));
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

    /// <summary>
    /// The text of each Keybind of an action, as the draft holds it, for the filter to match against.
    /// </summary>
    private IEnumerable<string> KeybindTexts(string functionName) =>
        _draft.Current.KeybindsFor(functionName).Select(keybind => keybind.GetFormattedWithIcon());
    
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
        ContextualCategory = null;

        GC.SuppressFinalize(this);
    }
}
