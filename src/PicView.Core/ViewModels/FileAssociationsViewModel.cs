using PicView.Core.FileAssociations;
using R3;

namespace PicView.Core.ViewModels;

/// <summary>
/// View model for managing file associations in PicView, using CySharp/R3.
/// Handles the binding between UI checkboxes and file type data.
/// </summary>
public class FileAssociationsViewModel : IDisposable
{
    public FileAssociationsViewModel()
    {
        // Create file type groups and populate with data
        FileTypeGroups.AddRange(FileTypeGroupHelper.GetFileTypes());

        // CanExecute as observable
        var canExecute = IsProcessing
            .AsObservable()
            .Select(processing => !processing);

        // Commands
        ApplyCommand = canExecute
            .ToReactiveCommand(async _ => await ApplyFileAssociations().ConfigureAwait(false));

        UnassociateCommand = canExecute
            .ToReactiveCommand(async _ => { await UnassociateFileAssociations().ConfigureAwait(false); });

        ClearFilterCommand = canExecute
            .ToReactiveCommand(_ => { FilterText.Value = string.Empty; });

        ResetCommand = canExecute
            .ToReactiveCommand(_ => { ResetFileTypesToDefault(); });

        SelectAllCommand = canExecute
            .ToReactiveCommand(_ => { SelectAllFileTypes(); });

        UnselectAllCommand = canExecute
            .ToReactiveCommand(_ => { UnselectAllFileTypes(); });

        // Opacity reacts to IsProcessing
        IsProcessing
            .AsObservable()
            .Subscribe(isProcessing => { Opacity.Value = isProcessing ? 0.3 : 1.0; });
    }

    /// <summary>
    /// Gets the read-only collection of file type groups that are available for association.
    /// </summary>
    public List<FileTypeGroup> FileTypeGroups { get; } = [];

    /// <summary>
    /// Gets or sets the filter text used to search and filter file type groups and items.
    /// </summary>
    public BindableReactiveProperty<string?> FilterText { get; } = new(string.Empty, StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets or sets a value indicating whether the view model is currently processing an operation.
    /// Used to disable UI interaction during long-running tasks.
    /// </summary>
    public BindableReactiveProperty<bool> IsProcessing { get; } = new(false);

    /// <summary>
    /// Gets or sets the opacity value for the UI, used to visually indicate processing state.
    /// </summary>
    public BindableReactiveProperty<double> Opacity { get; } = new(1.0);

    /// <summary>
    /// Command to apply the selected file associations.
    /// </summary>
    public ReactiveCommand? ApplyCommand { get; }

    /// <summary>
    /// Command to clear the current filter text.
    /// </summary>
    public ReactiveCommand? ClearFilterCommand { get; }

    /// <summary>
    /// Command to unassociate all file types from the application.
    /// </summary>
    public ReactiveCommand? UnassociateCommand { get; }

    /// <summary>
    /// Command to reset file type selections to their default state.
    /// </summary>
    public ReactiveCommand? ResetCommand { get; }

    /// <summary>
    /// Command to select all visible file types.
    /// </summary>
    public ReactiveCommand? SelectAllCommand { get; }

    /// <summary>
    /// Command to unselect all visible file types.
    /// </summary>
    public ReactiveCommand? UnselectAllCommand { get; }

    #region Selection

    private void UpdateSelection()
    {
        foreach (var group in FileTypeGroups)
        {
            group.IsSelected.Value = group.IsSelected.CurrentValue;
            foreach (var fileType in group.FileTypes)
            {
                fileType.IsSelected.Value = fileType.IsSelected.Value;
            }
        }
    }

    private void ResetFileTypesToDefault()
    {
        var defaultGroups = FileTypeGroupHelper.GetFileTypes();
        var currentGroups = FileTypeGroups.ToArray();

        foreach (var group in currentGroups)
        {
            var defaultGroup = defaultGroups.FirstOrDefault(g => g.Name == group.Name);
            if (defaultGroup == null)
            {
                continue;
            }

            group.IsSelected.Value = defaultGroup.IsSelected.CurrentValue;

            var fileTypes = group.FileTypes.ToArray();
            foreach (var fileType in fileTypes)
            {
                var defaultType = defaultGroup.FileTypes.FirstOrDefault(dt =>
                    dt.Description == fileType.Description);

                if (defaultType != null)
                {
                    fileType.IsSelected.Value = defaultType.IsSelected.CurrentValue;
                }
            }
        }
    }

    private void UnselectFileTypes()
    {
        var currentGroups = FileTypeGroups.ToArray();

        foreach (var group in currentGroups)
        {
            group.IsSelected.Value = false;
            var fileTypes = group.FileTypes.ToArray();
            foreach (var fileType in fileTypes)
            {
                fileType.IsSelected.Value = false;
            }
        }
    }

    private void SelectAllFileTypes()
    {
        var currentGroups = FileTypeGroups.ToArray();

        foreach (var group in currentGroups)
        {
            var fileTypes = group.FileTypes.ToArray();
            foreach (var fileType in fileTypes)
            {
                if (!fileType.IsVisible.CurrentValue)
                {
                    continue;
                }

                if (fileType.Extension.StartsWith(".zip") ||
                    fileType.Extension.StartsWith(".rar") ||
                    fileType.Extension.StartsWith(".7z") ||
                    fileType.Extension.StartsWith(".gzip"))
                {
                    continue;
                }

                fileType.IsSelected.Value = true;
            }
        }
    }

    private void UnselectAllFileTypes()
    {
        var currentGroups = FileTypeGroups.ToArray();
        var totalVisible = 0;
        var indeterminateCount = 0;

        foreach (var group in currentGroups)
        {
            foreach (var fileType in group.FileTypes.Where(ft => ft.IsVisible.CurrentValue))
            {
                totalVisible++;
                if (fileType.IsSelected.Value == null)
                {
                    indeterminateCount++;
                }
            }
        }

        var setToUnchecked = indeterminateCount >= totalVisible - indeterminateCount;

        foreach (var group in currentGroups)
        {
            foreach (var fileType in group.FileTypes.Where(ft => ft.IsVisible.CurrentValue))
            {
                fileType.IsSelected.Value = setToUnchecked ? false : null;
            }
        }
    }

    #endregion

    #region Associations

    private async Task<bool> ApplyFileAssociations()
        => await SetFileAssociations(false).ConfigureAwait(false);

    private async Task UnassociateFileAssociations()
        => await SetFileAssociations(true).ConfigureAwait(false);

    private async Task<bool> SetFileAssociations(bool unassociate)
    {
        try
        {
            IsProcessing.Value = true;

            return await Task.Run(async () =>
            {
                if (unassociate)
                {
                    UnselectFileTypes();
                }
                else
                {
                    UpdateSelection();
                }

                return await FileAssociationProcessor.SetFileAssociations(FileTypeGroups).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }
        finally
        {
            IsProcessing.Value = false;
        }
    }

    #endregion
    
    public void Dispose()
    {
        Disposable.Dispose(IsProcessing, Opacity, ApplyCommand, ClearFilterCommand, UnassociateCommand, ResetCommand,
            SelectAllCommand, UnselectAllCommand);
        foreach (var fileTypeGroup in FileTypeGroups)
        {
            fileTypeGroup.Dispose();
        }
        GC.SuppressFinalize(this);
    }
}