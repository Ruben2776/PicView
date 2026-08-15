using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using PicView.Avalonia.UI;
using PicView.Avalonia.Views.Main;
using PicView.Core.SettingsSearch;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Avalonia.Controllers;

public class SettingsSearchController(SettingsView view) : IDisposable
{
    private const string ClassSearchDim = "searchDim";
    private const string ClassSearchMatch = "searchMatch";
    private const string ClassSearchHeader = "searchHeader";
    private const string ClassSearchHeaderDim = "searchHeaderDim";

    private readonly List<Control> _searchMatches = [];
    private List<(Control Control, SettingsCategory Category)>? _allSearchableControls;

    private int _currentMatchIndex = -1;
    private bool _isScrollingProgrammatically;
    private bool _areCategoriesFiltered;
    private bool _suppressSidebarFilter;
    private bool _isUpdatingFromSpy;
    private bool _isNavigatingSuggestionsViaKeys;
    private List<KeyValuePair<SettingsCategory, Control>>? _orderedSections;
    private Dictionary<SettingsCategory, Control>? _sectionsMap;
    
    private IDisposable? _subscription;

    public void Initialize()
    {
        InitializeSections();
        IndexSearchableControls();

        view.FilterBox.TextChanged += OnSearchTextChanged;
        
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            // Keydown events are interrupted on macOS because of the Accent Menu
            view.FilterBox.KeyUp += OnFilterBoxKeyDown;
        }
        else
        {
            view.FilterBox.KeyDown += OnFilterBoxKeyDown;
        }
        
        view.SuggestionsListBox.SelectionChanged += OnSuggestionsSelectionChanged;
        view.CategoriesListBox.SelectionChanged += OnListBoxSelectionChanged;
        view.SuggestionsPopup.Closed += OnPopupClosed;

        if (view.DataContext is CoreViewModel core)
        {
            _subscription = core.SettingsViewModel.SelectedCategory.Subscribe(OnViewModelCategoryChanged);
        }
    }

    public void Dispose()
    {
        view.FilterBox.TextChanged -= OnSearchTextChanged;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            view.FilterBox.KeyUp -= OnFilterBoxKeyDown;
        }
        else
        {
            view.FilterBox.KeyDown -= OnFilterBoxKeyDown;
        }

        view.SuggestionsListBox.SelectionChanged -= OnSuggestionsSelectionChanged;
        view.CategoriesListBox.SelectionChanged -= OnListBoxSelectionChanged;
        view.SuggestionsPopup.Closed -= OnPopupClosed;
        
        _subscription?.Dispose();
        _allSearchableControls = null;
        _searchMatches.Clear();
        
        GC.SuppressFinalize(this);
    }
    
    public void ResetFilters()
    {
        if (!_areCategoriesFiltered)
        {
            return;
        }

        _areCategoriesFiltered = false;
        foreach (var item in view.CategoriesListBox.Items.OfType<ListBoxItem>())
        {
            item.IsVisible = true;
        }
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

        if (_areCategoriesFiltered)
        {
            ResetFilters();
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

        _isUpdatingFromSpy = true;
        try
        {
            core.SettingsViewModel.SelectedCategory.Value = bestMatch.Value;
        }
        finally
        {
            _isUpdatingFromSpy = false;
        }
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
        var isCtrl = RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
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

    private void InitializeSections()
    {
        _sectionsMap = new Dictionary<SettingsCategory, Control>();
        var tempOrdered = new List<KeyValuePair<SettingsCategory, Control>>();

        foreach (var category in Enum.GetValues<SettingsCategory>())
        {
            var sectionName = $"{category}Section";
            var control = view.FindControl<Control>(sectionName);
            if (control == null)
            {
                continue;
            }

            _sectionsMap[category] = control;
            tempOrdered.Add(new KeyValuePair<SettingsCategory, Control>(category, control));
        }

        _orderedSections = tempOrdered.OrderBy(x => x.Value.Bounds.Y).ToList();
    }

    private void IndexSearchableControls()
    {
        _allSearchableControls = [];

        if (_orderedSections == null)
        {
            return;
        }

        foreach (var (category, sectionControl) in _orderedSections)
        {
            var children = sectionControl.GetVisualDescendants().OfType<Control>();
            foreach (var child in children)
            {
                if (child.Tag != null || !string.IsNullOrEmpty(SearchProperties.GetKeywords(child)))
                {
                    _allSearchableControls.Add((child, category));
                }
            }
        }
    }
    
    private void OnPopupClosed(object? sender, EventArgs e)
    {
        ResetFilters();
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

    private void OnFilterBoxKeyDown(object? sender, KeyEventArgs e)
    {
        if (view.FilterBox.Text?.Length > 0 && view.SuggestionsListBox.ItemCount > 0)
        {
            view.SuggestionsPopup.IsOpen = true;
        }
        
        if (view.SuggestionsListBox.ItemCount > 0 && (e.Key == Key.Down || e.Key == Key.Up))
        {
            _isNavigatingSuggestionsViaKeys = true;
            try
            {
                var current = view.SuggestionsListBox.SelectedIndex;
                var count = view.SuggestionsListBox.ItemCount;

                if (e.Key == Key.Down)
                {
                    if (current < count - 1)
                        view.SuggestionsListBox.SelectedIndex++;
                    else
                        view.SuggestionsListBox.SelectedIndex = 0;
                }
                else // Key.Up
                {
                    if (current > 0)
                        view.SuggestionsListBox.SelectedIndex--;
                    else
                        view.SuggestionsListBox.SelectedIndex = count - 1;
                }
                
                view.SuggestionsListBox.ScrollIntoView(view.SuggestionsListBox.SelectedItem);
            }
            finally
            {
                _isNavigatingSuggestionsViaKeys = false;
            }

            e.Handled = true;
            return;
        }
        
        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            
            if (view.SuggestionsPopup.IsOpen && view.SuggestionsListBox.SelectedItem != null)
            {
                CommitSuggestion();
            }
            else
            {
                CycleToNextMatch();
            }
        }
    }

    private void OnSuggestionsSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_isNavigatingSuggestionsViaKeys)
        {
            return;
        }
        
        CommitSuggestion();
    }

    private void CommitSuggestion()
    {
        if (view.SuggestionsListBox.SelectedItem is not SettingsSearchItem item ||
            view.DataContext is not CoreViewModel { SettingsViewModel: not null } core)
        {
            return;
        }

        _suppressSidebarFilter = true;
            
        core.SettingsViewModel.SelectedSuggestion.Value = item;
        view.SuggestionsPopup.IsOpen = false;
            
        Dispatcher.UIThread.Post(() =>
        {
            if (view.FilterBox.Text != null)
            {
                view.FilterBox.CaretIndex = view.FilterBox.Text.Length;
            }
        }, DispatcherPriority.Input);
    }
    
    private void OnSearchTextChanged(object? sender, TextChangedEventArgs e)
    {
        var searchText = view.FilterBox.Text;

        if (string.IsNullOrWhiteSpace(searchText))
        {
            view.SuggestionsPopup.IsOpen = false;
        }

        if (view.DataContext is CoreViewModel core &&
            !string.IsNullOrWhiteSpace(searchText) &&
            core.SettingsViewModel.IsOverviewVisible.Value)
        {
            core.SettingsViewModel.IsOverviewVisible.Value = false;
        }

        PerformSearch(searchText);

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            Dispatcher.UIThread.Post(ScrollToNearestMatch, DispatcherPriority.Input);
        }
    }

    private void PerformSearch(string? query)
    {
        if (_sectionsMap == null)
        {
            return;
        }

        _searchMatches.Clear();
        _currentMatchIndex = -1;

        if (_allSearchableControls == null)
        {
            IndexSearchableControls();
        }

        var isQueryEmpty = string.IsNullOrWhiteSpace(query);

        if (isQueryEmpty)
        {
            if (_allSearchableControls == null)
            {
                return;
            }

            foreach (var (child, _) in _allSearchableControls)
            {
                child.Classes.Remove(ClassSearchMatch);
                child.Classes.Remove(ClassSearchDim);
            }

            UpdateSectionHeaders(false);

            foreach (var item in view.CategoriesListBox.Items.OfType<ListBoxItem>())
            {
                item.IsVisible = true;
            }

            _areCategoriesFiltered = false;
            return;
        }

        var matchingCategories = new HashSet<SettingsCategory>();

        foreach (var (child, category) in _allSearchableControls!)
        {
            var isMatch = IsControlMatch(child, query!);

            if (isMatch)
            {
                child.Classes.Remove(ClassSearchDim);
                child.Classes.Add(ClassSearchMatch);
                _searchMatches.Add(child);
                matchingCategories.Add(category);
            }
            else
            {
                child.Classes.Remove(ClassSearchMatch);
                child.Classes.Add(ClassSearchDim);
            }
        }

        UpdateSectionHeaders(true);
        
        var shouldShowAll = _suppressSidebarFilter;
        if (shouldShowAll)
        {
            _suppressSidebarFilter = false;
            _areCategoriesFiltered = false;
        }
        else
        {
            _areCategoriesFiltered = true;
        }

        foreach (var item in view.CategoriesListBox.Items.OfType<ListBoxItem>())
        {
            if (shouldShowAll)
            {
                item.IsVisible = true;
            }
            else if (item.Tag is SettingsCategory category)
            {
                item.IsVisible = matchingCategories.Contains(category);
            }
        }
    }

    private static bool IsControlMatch(Control child, string query)
    {
        var keywordsString = SearchProperties.GetKeywords(child);

        if (string.IsNullOrEmpty(keywordsString) && child.Tag is string tag)
        {
            keywordsString = tag;
        }

        if (string.IsNullOrEmpty(keywordsString))
        {
            return false;
        }

        return keywordsString.Contains(query, StringComparison.OrdinalIgnoreCase);
    }

    private void UpdateSectionHeaders(bool isSearchActive)
    {
        if (_orderedSections == null)
        {
            return;
        }

        foreach (var section in _orderedSections.Select(kvp => kvp.Value))
        {
            if (section is UserControl { Content: Panel panel })
            {
                ProcessSectionPanel(panel, isSearchActive);
            }
        }
    }

    private void ProcessSectionPanel(Panel panel, bool isSearchActive)
    {
        if (!isSearchActive)
        {
            foreach (var child in panel.Children.Where(IsHeader))
            {
                child.Classes.Remove(ClassSearchHeaderDim);
                if (!child.Classes.Contains(ClassSearchHeader))
                {
                    child.Classes.Add(ClassSearchHeader);
                }
            }

            return;
        }

        Control? currentHeader = null;
        var currentHeaderHasMatches = false;

        foreach (var child in panel.Children)
        {
            if (IsHeader(child))
            {
                if (currentHeader != null)
                {
                    ApplyHeaderState(currentHeader, currentHeaderHasMatches);
                }

                currentHeader = child;
                currentHeaderHasMatches = false;
            }
            else
            {
                if (HasMatch(child))
                {
                    currentHeaderHasMatches = true;
                }
            }
        }

        if (currentHeader != null)
        {
            ApplyHeaderState(currentHeader, currentHeaderHasMatches);
        }
    }

    private static bool IsHeader(Control control)
    {
        return control.Classes.Contains(ClassSearchHeader) || control.Classes.Contains(ClassSearchHeaderDim);
    }

    private static void ApplyHeaderState(Control header, bool hasMatches)
    {
        if (hasMatches)
        {
            header.Classes.Remove(ClassSearchHeaderDim);
            if (!header.Classes.Contains(ClassSearchHeader))
            {
                header.Classes.Add(ClassSearchHeader);
            }
        }
        else
        {
            header.Classes.Remove(ClassSearchHeader);
            if (!header.Classes.Contains(ClassSearchHeaderDim))
            {
                header.Classes.Add(ClassSearchHeaderDim);
            }
        }
    }

    private static bool HasMatch(Control control)
    {
        if (control.Classes.Contains(ClassSearchMatch))
        {
            return true;
        }

        return control.GetVisualDescendants()
            .OfType<Control>()
            .Any(c => c.Classes.Contains(ClassSearchMatch));
    }

    private void CycleToNextMatch()
    {
        if (_searchMatches.Count == 0)
        {
            return;
        }

        _currentMatchIndex = (_currentMatchIndex + 1) % _searchMatches.Count;
        ScrollToControl(_searchMatches[_currentMatchIndex]);
    }

    private void ScrollToNearestMatch()
    {
        if (_searchMatches.Count == 0)
        {
            return;
        }

        var viewportCenter = view.ContentScrollViewer.Viewport.Height / 2;

        var bestMatch = _searchMatches.MinBy(match =>
        {
            var relativePoint = match.TranslatePoint(new Point(0, 0), view.ContentScrollViewer);
            if (!relativePoint.HasValue)
            {
                return double.MaxValue;
            }

            var elementCenter = relativePoint.Value.Y + match.Bounds.Height / 2;
            return Math.Abs(elementCenter - viewportCenter);
        });

        if (bestMatch == null)
        {
            return;
        }

        _currentMatchIndex = _searchMatches.IndexOf(bestMatch);
        ScrollToControl(bestMatch);
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

            return;
        }
    }
}
