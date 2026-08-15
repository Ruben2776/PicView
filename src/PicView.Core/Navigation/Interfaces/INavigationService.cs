using System.Collections.ObjectModel;
using PicView.Core.FileSearch;
using PicView.Core.FileSorting;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Core.Navigation.Interfaces;

public interface INavigationService
{
    ValueTask LoadFromFileAsync(string source, TabViewModel tab, CancellationTokenSource ct);
    
    ValueTask LoadFromFileAsync(FileInfo fileInfo, TabViewModel tab, CancellationTokenSource ct);
    
    ValueTask LoadFromDirectoryAsync(FileInfo source, TabViewModel tab, CancellationTokenSource ct);

    ValueTask<bool> LoadFromStringAsync(string source, TabViewModel tab, CancellationTokenSource ct);
    ReactiveCommand<string>? LoadFromStringCommand { get; set; }
    
    ValueTask LoadFromUrlAsync(string source, TabViewModel tab, CancellationTokenSource ct);

    ValueTask<bool> LoadFromArchiveAsync(string archivePath, TabViewModel tab, CancellationTokenSource ct);

    ValueTask NavigateAsync(TabViewModel tab, NavigateTo to, CancellationTokenSource ct);

    ValueTask NavigateByIncrementsAsync(TabViewModel tab, SkipAmount skipAmount, bool forwards, CancellationTokenSource ct);
    
    ValueTask RepopulateIterator(FileInfo fileInfo, TabViewModel tab, CancellationTokenSource ct, List<FileInfo>? files = null);

    /// <summary>
    /// Sorts the files in the current tab according to the specified sort order,
    /// updates the current index to maintain the current image, and resynchronizes the cache.
    /// </summary>
    ValueTask SortAsync(TabViewModel tab, SortFilesBy sortOrder, CancellationTokenSource ct);

    /// <summary>
    /// Updates the sort direction (ascending/descending) for the current tab,
    /// updates the current index to maintain the current image, and resynchronizes the cache.
    /// </summary>
    ValueTask SortAsync(TabViewModel tab, bool ascending, CancellationTokenSource ct);

    ValueTask<bool> LoadLastFileAsync(TabViewModel tab, CancellationTokenSource ct);

    /// <summary>
    /// Navigates to the next folder relative to the current image's directory.
    /// If <see cref="PicView.Core.Config.Sorting.IncludeSubDirectories"/> is true, it explores subdirectories first.
    /// </summary>
    ValueTask NavigateToNextFolderAsync(TabViewModel tab, CancellationTokenSource ct);

    /// <summary>
    /// Navigates to the previous folder relative to the current image's directory.
    /// If <see cref="PicView.Core.Config.Sorting.IncludeSubDirectories"/> is true, it explores subdirectories in reverse order.
    /// </summary>
    ValueTask NavigateToPreviousFolderAsync(TabViewModel tab, CancellationTokenSource ct);

    /// <summary>
    /// Navigates to the next archive file relative to the current image's directory.
    /// An archive is identified using <see cref="PicView.Core.FileHandling.SupportedFiles"/>'s <c>IsArchive</c> check.
    /// If <see cref="PicView.Core.Config.Sorting.IncludeSubDirectories"/> is true, subdirectories are explored first.
    /// </summary>
    ValueTask NavigateToNextArchiveAsync(TabViewModel tab, CancellationTokenSource ct);

    /// <summary>
    /// Navigates to the previous archive file relative to the current image's directory.
    /// An archive is identified using <see cref="PicView.Core.FileHandling.SupportedFiles"/>'s <c>IsArchive</c> check.
    /// If <see cref="PicView.Core.Config.Sorting.IncludeSubDirectories"/> is true, subdirectories are explored in reverse order.
    /// </summary>
    ValueTask NavigateToPreviousArchiveAsync(TabViewModel tab, CancellationTokenSource ct);

    public BindableReactiveProperty<ObservableCollection<FileSearchResult>?>? FilteredFileInfos { get; set; }

}