using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.UI;
using PicView.Core.FileAssociations;
using PicView.Core.Localization;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Avalonia.Win32.Views;

public partial class FileAssociationWindow : GenericWindow
{
    private readonly List<(CheckBox CheckBox, string SearchText)> _allCheckBoxes = [];
    private readonly CompositeDisposable _disposables = new();
    
    public FileAssociationWindow()
    {
        InitializeComponent();

        GenericWindowHelper.AboutWindowInitialize(this);
        
        Loaded += delegate
        {
            InitializeCheckBoxesCollection();

            KeyDown += OnKeyDown;
            Closing += OnClosing;
        };
    }

    private void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        _disposables.Dispose();
        if (DataContext is not CoreViewModel core)
        {
            return;
        }

        core.AssociationsViewModel.Dispose();
        core.AssociationsViewModel = null;
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        var ctrl = e.KeyModifiers.HasFlag(KeyModifiers.Control) || e.KeyModifiers.HasFlag(KeyModifiers.Meta);
        if (e.Key == Key.F && ctrl)
        {
            FilterBox.Focus();
        }
    }

    private void InitializeCheckBoxesCollection()
    {
        var container = FileTypesContainer;

        if (DataContext is not CoreViewModel core)
        {
            return;
        }

        core.AssociationsViewModel ??= new FileAssociationsViewModel();

        // Subscribe to changes in the filter text
        Observable.EveryValueChanged(core.AssociationsViewModel, x => x.FilterText.Value)
            .Subscribe(FilterCheckBoxes)
            .AddTo(_disposables);

        core.AssociationsViewModel.ResetCommand.Subscribe(UpdateCheckBoxesFromViewModel).AddTo(_disposables);
        core.AssociationsViewModel.SelectAllCommand.Subscribe(UpdateCheckBoxesFromViewModel).AddTo(_disposables);
        core.AssociationsViewModel.UnselectAllCommand.Subscribe(UpdateCheckBoxesFromViewModel).AddTo(_disposables);

        // Create checkboxes for each file type group and item
        foreach (var fileTypeGroup in core.AssociationsViewModel.FileTypeGroups)
        {
            if (fileTypeGroup.Name is null)
            {
                // If going into this view too fast, sometimes the name is null. This is a workaround
                if (fileTypeGroup.FileTypes.Any(x => x.Extension.StartsWith(".png")))
                {
                    fileTypeGroup.Name = TranslationManager.Translation.Normal!;
                }
                else if (fileTypeGroup.FileTypes.Any(x => x.Extension.StartsWith(".svg")))
                {
                    fileTypeGroup.Name = TranslationManager.Translation.Graphics!;
                }
                else if (fileTypeGroup.FileTypes.Any(x => x.Extension.StartsWith(".raw")))
                {
                    fileTypeGroup.Name = TranslationManager.Translation.RawCamera!;
                }
                else if (fileTypeGroup.FileTypes.Any(x => x.Extension.StartsWith(".wpg")))
                {
                    fileTypeGroup.Name = TranslationManager.Translation.Uncommon!;
                }
                else if (fileTypeGroup.FileTypes.Any(x => x.Extension.StartsWith(".zip")))
                {
                    fileTypeGroup.Name = TranslationManager.Translation.Archives!;
                }
            }

            var brush = UIHelper.GetBrush("SecondaryTextColor");

            // Create group header checkbox
            var groupCheckBox = new CheckBox
            {
                Classes = { "altHover", "y", "changeColor" },
                Tag = fileTypeGroup,
                Foreground = brush,
                IsChecked = fileTypeGroup.IsSelected.CurrentValue
            };

            var groupTextBlock = new TextBlock
            {
                Classes = { "txt" },
                Text = fileTypeGroup.Name,
                Foreground = brush,
                FontFamily = new FontFamily("avares://PicView.Avalonia/Assets/Fonts/Roboto-Bold.ttf#Roboto")
            };

            groupCheckBox.Content = groupTextBlock;

            // Add to container
            container.Children.Add(groupCheckBox);

            // Add to the collection for filtering
            _allCheckBoxes.Add((groupCheckBox, fileTypeGroup.Name));

            // Subscribe to changes in the file type group's IsSelected property
            Observable.EveryValueChanged(fileTypeGroup, x => x.IsSelected.Value)
                .Subscribe(isSelected => { groupCheckBox.IsChecked = isSelected; })
                .AddTo(_disposables);

            // Handle group checkbox changes to update all items in the group
            groupCheckBox.Click += delegate
            {
                var isChecked = groupCheckBox.IsChecked;

                foreach (var fileType in fileTypeGroup.FileTypes)
                {
                    fileType.IsSelected.Value = isChecked;
                }
            };

            // Create checkboxes for each file type item in the group
            foreach (var fileType in fileTypeGroup.FileTypes)
            {
                var fileCheckBox = new CheckBox
                {
                    Classes = { "altHover", "x", "changeColor" },
                    Tag = fileType,
                    IsChecked = fileType.IsSelected.CurrentValue,
                    Foreground = brush
                };

                var fileTextBlock = new TextBlock
                {
                    Classes = { "txt" },
                    Text = $"{fileType.Description} ({fileType.Extension})",
                    Margin = new Thickness(0),
                    Padding = new Thickness(0, 1, 5, 0),
                    Foreground = brush
                };

                fileCheckBox.Content = fileTextBlock;

                // Add to container
                container.Children.Add(fileCheckBox);

                // Add to the collection for filtering
                _allCheckBoxes.Add((fileCheckBox, $"{fileType.Description} {fileType.Extension}"));

                // Bind the checkbox to the file type's IsSelected property
                fileCheckBox.IsCheckedChanged += delegate
                {
                    // Update the model - important to handle null state correctly
                    fileType.IsSelected.Value = fileCheckBox.IsChecked;

                    // Now update the group checkbox state
                    UpdateGroupCheckboxState(fileTypeGroup);
                };

                // Subscribe to changes in the file type's IsSelected property
                Observable.EveryValueChanged(fileType, x => x.IsSelected.Value)
                    .Subscribe(isSelected => { fileCheckBox.IsChecked = isSelected; })
                    .AddTo(_disposables);

                // Subscribe to changes in the file type's IsVisible property
                Observable.EveryValueChanged(fileType, x => x.IsVisible.Value)
                    .Subscribe(isVisible => { fileCheckBox.IsVisible = isVisible; })
                    .AddTo(_disposables);
            }
        }
    }

    private void UpdateGroupCheckboxState(FileTypeGroup group)
    {
        // Find all checkboxes that are part of this group
        var fileTypeCheckboxes = FileTypesContainer.Children.OfType<CheckBox>()
            .Where(c => c.Tag is FileTypeItem ft && group.FileTypes.Contains(ft) && c.IsVisible);

        var allTrue = true;
        var allFalse = true;
        var anyNull = false;

        foreach (var cb in fileTypeCheckboxes)
        {
            if (!cb.IsChecked.HasValue)
            {
                anyNull = true;
                allTrue = false;
                allFalse = false;
            }
            else if (cb.IsChecked.Value)
            {
                allFalse = false;
            }
            else
            {
                allTrue = false;
            }
        }

        // Find the group checkbox
        var groupCheckbox = FileTypesContainer.Children.OfType<CheckBox>()
            .FirstOrDefault(c => c.Tag == group);

        if (groupCheckbox == null)
        {
            return;
        }

        if (anyNull)
        {
            groupCheckbox.IsChecked = null;
        }
        else if (allTrue)
        {
            groupCheckbox.IsChecked = true;
        }
        else if (allFalse)
        {
            groupCheckbox.IsChecked = false;
        }
        else
        {
            groupCheckbox.IsChecked = null;
        }

        // Update the ViewModel
        group.IsSelected.Value = groupCheckbox.IsChecked;
    }


    private void UpdateCheckBoxesFromViewModel(Unit unit)
    {
        if (DataContext is not CoreViewModel core)
        {
            return;
        }

        var checkBoxes = FileTypesContainer.Children.OfType<CheckBox>().ToArray();

        foreach (var group in core.AssociationsViewModel.FileTypeGroups)
        {
            if (group?.Name is null)
            {
                continue;
            }

            foreach (var fileType in group.FileTypes)
            {
                foreach (var checkBox in checkBoxes.Where(x => x.Tag == fileType))
                {
                    checkBox.IsChecked = fileType.IsSelected.Value;
                }
            }

            // Also update the group checkbox
            var groupCheckBox = checkBoxes.FirstOrDefault(x => x.Tag == group);
            groupCheckBox?.IsChecked = group.IsSelected.Value;
        }
    }

    private void FilterCheckBoxes(string? filterText)
    {
        if (string.IsNullOrWhiteSpace(filterText))
        {
            // Show all checkboxes
            foreach (var (checkBox, _) in _allCheckBoxes)
            {
                checkBox.IsVisible = true;
            }
        }
        else
        {
            foreach (var (checkBox, searchText) in _allCheckBoxes)
            {
                checkBox.IsVisible = searchText.Contains(filterText, StringComparison.InvariantCultureIgnoreCase);
            }
        }
        
        if (DataContext is not CoreViewModel core)
        {
            return;
        }
        
        foreach (var group in core.AssociationsViewModel.FileTypeGroups)
        {
            UpdateGroupCheckboxState(group);
        }
    }
}