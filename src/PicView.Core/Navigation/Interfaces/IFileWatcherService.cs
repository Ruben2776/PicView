using PicView.Core.ViewModels;

namespace PicView.Core.Navigation.Interfaces;

public interface IFileWatcherService
{
    void Watch(TabViewModel tab, string? directory = null);
    void Unwatch(TabViewModel tab);

    /// <summary>
    /// Whether <paramref name="tab"/> hears the file events of <paramref name="directory"/> itself now, so the files
    /// it lists from there are kept as the folder has them. A service that cannot tell says no.
    /// </summary>
    bool IsWatching(TabViewModel tab, string directory) => false;

    /// <summary>
    /// Starts every folder watch again, as when Include Subdirectories changes.
    /// </summary>
    void RestartWatches();
}
