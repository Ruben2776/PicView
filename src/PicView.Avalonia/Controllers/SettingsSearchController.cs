using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using PicView.Avalonia.UI;
using PicView.Avalonia.Views.Main;
using PicView.Core.SettingsSearch;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Avalonia.Controllers;

/// <summary>
/// Runs the Settings window's search and its category list. Typing only fills the suggestion list; the page is
/// never filtered. Picking a suggestion scrolls to its setting row, pulses the row once, moves focus into it and
/// clears the search, so nothing is left to dismiss.
/// </summary>
public class SettingsSearchController(SettingsView view) : IDisposable
{
    private const string ClassSearchPulse = "searchPulse";
    private const string AccentColorKey = "AccentColor";
    private const double PulseBackgroundOpacity = 0.14;

    // The row's brush transitions take 0.4 s each way, so the pulse is over after about 1.5 s
    private static readonly TimeSpan PulseHold = TimeSpan.FromMilliseconds(1100);

    private readonly Dictionary<SettingKey, Control> _rows = [];

    private bool _isScrollingProgrammatically;
    private bool _isUpdatingFromSpy;
    private List<KeyValuePair<SettingsCategory, Control>>? _orderedSections;
    private Dictionary<SettingsCategory, Control>? _sectionsMap;

    private Control? _pulsedRow;
    private IDisposable? _pulseTimer;
    private IDisposable? _categorySubscription;
    private IDisposable? _suggestionsSubscription;

    public void Initialize()
    {
        InitializeSections();
        IndexRows();

        if (OperatingSystem.IsMacOS())
        {
            // Keydown events are interrupted on macOS because of the Accent Menu
            view.FilterBox.KeyUp += OnFilterBoxKeyDown;
        }
        else
        {
            view.FilterBox.KeyDown += OnFilterBoxKeyDown;
        }

        view.SuggestionsListBox.Tapped += OnSuggestionTapped;
        view.CategoriesListBox.SelectionChanged += OnListBoxSelectionChanged;

        if (view.DataContext is not CoreViewModel { SettingsViewModel: { } settings } core)
        {
            return;
        }

        settings.SetSettingsOnPage(_rows.Keys.ToHashSet(), _sectionsMap!.Keys.ToHashSet());
        _categorySubscription = settings.SelectedCategory.Subscribe(OnViewModelCategoryChanged);
        _suggestionsSubscription = settings.Suggestions.Subscribe(OnSuggestionsChanged);
        core.GlobalSettings.InitializeExtraSettings();
    }

    public void Dispose()
    {
        if (OperatingSystem.IsMacOS())
        {
            view.FilterBox.KeyUp -= OnFilterBoxKeyDown;
        }
        else
        {
            view.FilterBox.KeyDown -= OnFilterBoxKeyDown;
        }

        view.SuggestionsListBox.Tapped -= OnSuggestionTapped;
        view.CategoriesListBox.SelectionChanged -= OnListBoxSelectionChanged;

        _categorySubscription?.Dispose();
        _suggestionsSubscription?.Dispose();
        EndPulse();
        _rows.Clear();

        GC.SuppressFinalize(this);
    }

    public void ClosePopup()
    {
        view.SuggestionsPopup.Close();
        view.FilterBox.Text = string.Empty;
    }

    public void OnViewModelCategoryChanged(SettingsCategory category)
    {
        // 1. Sync ListBox UI
        var item = view.CategoriesListBox.Items.OfType<ListBoxItem>()
            .FirstOrDefault(x => x.Tag is SettingsCategory cat && cat == category);

        if (item != null && !ReferenceEquals(view.CategoriesListBox.SelectedItem, item))
        {
            view.CategoriesListBox.SelectedItem = item;
        }

        // 2. Scroll to section (unless we are just following the user's scroll)
        if (!_isUpdatingFromSpy)
        {
            ScrollToCategory(category);
        }
    }

    public void HandleScrollChanged(double offset, double panelSpacing)
    {
        // Optimization: Exit early if logic is running, if data missing, or if pure UI scrolling
        if (_isScrollingProgrammatically || _orderedSections == null || view.DataContext is not CoreViewModel core)
        {
            return;
        }

        SettingsCategory? bestMatch = null;

        // Optimization: Iterate the pre-ordered list instead of Sorting every frame
        // We look for the last section whose top is above the current scroll offset
        foreach (var kvp in _orderedSections)
        {
            if (kvp.Value.Bounds.Y <= offset + panelSpacing)
            {
                bestMatch = kvp.Key;
            }
            else
            {
                break;
            }
        }

        // Default to General if at top
        if (!bestMatch.HasValue && _orderedSections.Count > 0)
        {
            bestMatch = SettingsCategory.General;
        }

        // Fix never selecting last match
        if (_orderedSections.Count > 1)
        {
            var secondLastMatch = _orderedSections[^2].Key;
            if (bestMatch == secondLastMatch)
            {
                // Check if we have scrolled to the absolute bottom
                var scrollViewer = view.ContentScrollViewer;

                // Using a 1.0 tolerance for floating point rounding differences
                if (offset + scrollViewer.Viewport.Height >= scrollViewer.Extent.Height - 1.0)
                {
                    bestMatch = _orderedSections.Last().Key;
                }
            }
        }

        // Only update ViewModel if value changed to avoid reactive loops
        if (!bestMatch.HasValue || core.SettingsViewModel.SelectedCategory.Value == bestMatch.Value)
        {
            return;
        }

        SelectCategoryWithoutScrolling(core, bestMatch.Value);
    }

    public void HandleKeyDown(KeyEventArgs e)
    {
        if (view.DataContext is not CoreViewModel core)
        {
            return;
        }

        var isSearchFocused = view.FilterBox.IsFocused || view.FilterBox.IsKeyboardFocusWithin;

        // Handle Escape
        if (e.Key == Key.Escape && isSearchFocused)
        {
            if (view.FilterBox.Text?.Length > 0)
            {
                view.FilterBox.Clear();
            }
            else
            {
                view.ContentScrollViewer.Focus(); // Unfocus search
            }

            e.Handled = true;
            return;
        }

        // Handle Ctrl+F or '?' to focus search
        var isCtrl = OperatingSystem.IsMacOS()
            ? e.KeyModifiers.HasFlag(KeyModifiers.Meta)
            : e.KeyModifiers.HasFlag(KeyModifiers.Control);

        if ((e.Key == Key.F && isCtrl) || (!isSearchFocused && e.Key == Key.OemQuestion))
        {
            view.FilterBox.Focus();
            e.Handled = true;
            return;
        }


        if (core.SettingsViewModel.IsOverviewVisible.CurrentValue)
        {
            return;
        }

        // Standard Scroll Navigation
        switch (e.Key)
        {
            case Key.Down:
            case Key.PageDown:
                view.ContentScrollViewer.LineDown();
                break;
            case Key.Up:
            case Key.PageUp:
                view.ContentScrollViewer.LineUp();
                break;
        }
    }

    /// <summary>
    /// Leads to what <paramref name="item"/> names and ends the search: a setting is scrolled to the middle of the
    /// page, pulsed and given focus; a category is scrolled to its top.
    /// </summary>
    public void Pick(SettingsSearchItem item)
    {
        if (view.DataContext is not CoreViewModel core)
        {
            return;
        }

        var settings = core.SettingsViewModel;
        view.SuggestionsPopup.IsOpen = false;
        settings.SearchQuery.Value = string.Empty;

        if (!settings.IsOverviewVisible.Value)
        {
            GoTo(core, item);
            return;
        }

        // The page has no layout while the overview covers it, so go there once it has been shown
        settings.IsOverviewVisible.Value = false;
        Dispatcher.UIThread.Post(() => GoTo(core, item), DispatcherPriority.Loaded);
    }

    private void GoTo(CoreViewModel core, SettingsSearchItem item)
    {
        if (item.Key is not { } key)
        {
            SelectCategoryWithoutScrolling(core, item.Category);
            ScrollToCategory(item.Category);
            view.ContentScrollViewer.Focus();
            return;
        }

        if (!_rows.TryGetValue(key, out var row))
        {
            return;
        }

        ScrollToControl(row);
        Pulse(row);
        FocusSetting(row);
    }

    private void FocusSetting(Control row)
    {
        var target = row.GetVisualDescendants()
            .OfType<InputElement>()
            .FirstOrDefault(x => x is { Focusable: true, IsEffectivelyVisible: true, IsEffectivelyEnabled: true });

        if (target is null)
        {
            view.ContentScrollViewer.Focus();
            return;
        }

        target.Focus(NavigationMethod.Tab);
    }

    private void Pulse(Control row)
    {
        EndPulse();

        _pulsedRow = row;
        row.Classes.Add(ClassSearchPulse);

        if (row is Border border &&
            row.TryFindResource(AccentColorKey, row.ActualThemeVariant, out var resource) &&
            resource is Color accent)
        {
            border.Background = new SolidColorBrush(accent, PulseBackgroundOpacity);
        }

        _pulseTimer = DispatcherTimer.RunOnce(EndPulse, PulseHold);
    }

    private void EndPulse()
    {
        _pulseTimer?.Dispose();
        _pulseTimer = null;

        if (_pulsedRow is null)
        {
            return;
        }

        _pulsedRow.Classes.Remove(ClassSearchPulse);
        _pulsedRow.ClearValue(Border.BackgroundProperty);
        _pulsedRow = null;
    }

    private void InitializeSections()
    {
        _sectionsMap = new Dictionary<SettingsCategory, Control>();
        var tempOrdered = new List<KeyValuePair<SettingsCategory, Control>>();

        foreach (var category in Enum.GetValues<SettingsCategory>())
        {
            var sectionName = $"{category}Section";
            var control = view.FindControl<Control>(sectionName);
            if (control is not { IsVisible: true })
            {
                continue;
            }

            _sectionsMap[category] = control;
            tempOrdered.Add(new KeyValuePair<SettingsCategory, Control>(category, control));
        }

        _orderedSections = tempOrdered.OrderBy(x => x.Value.Bounds.Y).ToList();
    }

    private void IndexRows()
    {
        _rows.Clear();

        if (_orderedSections == null)
        {
            return;
        }

        foreach (var (_, section) in _orderedSections)
        {
            foreach (var child in section.GetVisualDescendants().OfType<Control>())
            {
                if (SearchProperties.GetKey(child) is { } key && IsShownWithin(child, section))
                {
                    _rows[key] = child;
                }
            }
        }
    }

    // Not IsEffectivelyVisible: the whole page is hidden while the overview shows, and its rows are still there
    private static bool IsShownWithin(Control row, Control section)
    {
        for (Visual? visual = row; visual is not null && visual != section; visual = visual.GetVisualParent())
        {
            if (!visual.IsVisible)
            {
                return false;
            }
        }

        return true;
    }

    private void OnListBoxSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (view.CategoriesListBox.SelectedItem is ListBoxItem { Tag: SettingsCategory category } &&
            view.DataContext is CoreViewModel core &&
            core.SettingsViewModel.SelectedCategory.Value != category)
        {
            core.SettingsViewModel.SelectedCategory.Value = category;
        }
    }

    private void OnSuggestionsChanged(List<SettingsSearchItem> suggestions)
    {
        // A language change sets the suggestions off the UI thread, and the list box has yet to take them
        Dispatcher.UIThread.Post(() =>
        {
            var hasSuggestions = suggestions.Count > 0;
            view.SuggestionsPopup.IsOpen = hasSuggestions;
            if (hasSuggestions)
            {
                // The first suggestion is the one Enter picks
                view.SuggestionsListBox.SelectedIndex = 0;
            }
        }, DispatcherPriority.Input);
    }

    private void OnSuggestionTapped(object? sender, TappedEventArgs e)
    {
        if (e.Source is Visual source &&
            source.FindAncestorOfType<ListBoxItem>(true) is { DataContext: SettingsSearchItem item })
        {
            Pick(item);
        }
    }

    private void OnFilterBoxKeyDown(object? sender, KeyEventArgs e)
    {
        var count = view.SuggestionsListBox.ItemCount;
        if (count == 0 || !view.SuggestionsPopup.IsOpen)
        {
            return;
        }

        switch (e.Key)
        {
            case Key.Down:
                view.SuggestionsListBox.SelectedIndex = (view.SuggestionsListBox.SelectedIndex + 1) % count;
                break;
            case Key.Up:
                view.SuggestionsListBox.SelectedIndex = (view.SuggestionsListBox.SelectedIndex - 1 + count) % count;
                break;
            case Key.Enter:
                if (view.SuggestionsListBox.SelectedItem is SettingsSearchItem item)
                {
                    Pick(item);
                }

                e.Handled = true;
                return;
            default:
                return;
        }

        view.SuggestionsListBox.ScrollIntoView(view.SuggestionsListBox.SelectedIndex);
        e.Handled = true;
    }

    private void SelectCategoryWithoutScrolling(CoreViewModel core, SettingsCategory category)
    {
        if (core.SettingsViewModel.SelectedCategory.Value == category)
        {
            return;
        }

        _isUpdatingFromSpy = true;
        try
        {
            core.SettingsViewModel.SelectedCategory.Value = category;
        }
        finally
        {
            _isUpdatingFromSpy = false;
        }
    }

    private void ScrollToCategory(SettingsCategory category)
    {
        if (_sectionsMap == null || !_sectionsMap.TryGetValue(category, out var section))
        {
            return;
        }

        PerformProgrammaticScroll(section.Bounds.Y);
    }

    private void ScrollToControl(Control target)
    {
        var relativePoint = target.TranslatePoint(new Point(0, 0), view.ContentScrollViewer);
        if (!relativePoint.HasValue)
        {
            return;
        }

        var targetOffset = view.ContentScrollViewer.Offset.Y + relativePoint.Value.Y
                           - view.ContentScrollViewer.Viewport.Height / 2
                           + target.Bounds.Height / 2;

        PerformProgrammaticScroll(targetOffset, target);
    }

    private void PerformProgrammaticScroll(double offset, Control? targetForCategoryUpdate = null)
    {
        _isScrollingProgrammatically = true;
        try
        {
            view.ContentScrollViewer.Offset = new Vector(0, offset);

            if (targetForCategoryUpdate != null)
            {
                UpdateCategoryForMatch(targetForCategoryUpdate);
            }
        }
        finally
        {
            Dispatcher.UIThread.Post(() => _isScrollingProgrammatically = false, DispatcherPriority.Input);
        }
    }

    private void UpdateCategoryForMatch(Control target)
    {
        if (_sectionsMap == null || view.DataContext is not CoreViewModel core)
        {
            return;
        }

        foreach (var (category, section) in _sectionsMap)
        {
            if (section != target && !section.IsVisualAncestorOf(target))
            {
                continue;
            }

            SelectCategoryWithoutScrolling(core, category);
            return;
        }
    }
}
