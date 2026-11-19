using PicView.Core.DebugTools;
using PicView.Core.Extensions;
using PicView.Core.FileHandling;

namespace PicView.Core.FileSorting;

// One instance is shared by the app, so it holds no order: every call is given the ordering of the tab it steps for
public class FileAndDirectoryNavigator
{
    public string? FindNextArchive(string currentDir, bool next, string currentFilePath, FileOrdering ordering)
    {
        // Look for next archive in the current directory after the current file
        var archives = GetSortedArchivesInDirectory(currentDir, ordering);
        var idx = archives.FindIndex(a => a.FullName.Equals(currentFilePath, StringComparison.OrdinalIgnoreCase));
        if (!next)
        {
            return idx switch
            {
                > 0 => archives[idx - 1].FullName,
                < 0 when archives.Length > 0 => archives[^1].FullName,
                _ => GetLastArchiveInPreviousSiblingOrAncestor(currentDir, ordering)
            };
        }
        switch (idx)
        {
            case >= 0 when idx + 1 < archives.Length:
                return archives[idx + 1].FullName;
            case < 0 when archives.Length > 0:
                return archives[0].FullName;
        }
    
        if (!Settings.Sorting.IncludeSubDirectories)
        {
            return GetFirstArchiveInNextSiblingOrAncestor(currentDir, ordering);
        }
    
        var firstInChild = GetFirstArchiveInDescendants(currentDir, ordering);
        return firstInChild ?? GetFirstArchiveInNextSiblingOrAncestor(currentDir, ordering);
    }

    public string? GetFirstArchiveInDescendants(string path, FileOrdering ordering)
    {
        try
        {
            var dir = new DirectoryInfo(path);
            var subDirs = dir.GetDirectories();
            SortDirectories(subDirs, ordering);

            foreach (var sub in subDirs)
            {
                var archives = GetSortedArchivesInDirectory(sub.FullName, ordering);
                if (archives.Length > 0)
                {
                    return archives[0].FullName;
                }

                if (!Settings.Sorting.IncludeSubDirectories)
                {
                    continue;
                }

                var child = GetFirstArchiveInDescendants(sub.FullName, ordering);
                if (child != null)
                {
                    return child;
                }
            }
        }
        catch (Exception ex)
        {
            DebugHelper.LogDebug(nameof(FileAndDirectoryNavigator), nameof(GetFirstArchiveInDescendants), ex);
        }
        return null;
    }

    public string? GetFirstArchiveInNextSiblingOrAncestor(string path, FileOrdering ordering)
    {
        var dir = new DirectoryInfo(path);
        var parent = dir.Parent;
        if (parent == null)
        {
            return null;
        }

        try
        {
            var siblings = parent.GetDirectories();
            SortDirectories(siblings, ordering);
            var index = siblings.FindIndex(d => d.FullName.Equals(path, StringComparison.OrdinalIgnoreCase));

            for (var i = index + 1; i < siblings.Length; i++)
            {
                var sibling = siblings[i];
                var archives = GetSortedArchivesInDirectory(sibling.FullName, ordering);
                if (archives.Length > 0)
                {
                    return archives[0].FullName;
                }

                if (!Settings.Sorting.IncludeSubDirectories)
                {
                    continue;
                }

                var child = GetFirstArchiveInDescendants(sibling.FullName, ordering);
                if (child != null)
                {
                    return child;
                }
            }
        }
        catch (Exception ex)
        {
            DebugHelper.LogDebug(nameof(FileAndDirectoryNavigator), nameof(GetFirstArchiveInNextSiblingOrAncestor), ex);
        }

        return GetFirstArchiveInNextSiblingOrAncestor(parent.FullName, ordering);
    }

    public string? GetLastArchiveInPreviousSiblingOrAncestor(string currentPath, FileOrdering ordering)
    {
        var dir = new DirectoryInfo(currentPath);
        var parent = dir.Parent;
        if (parent is null)
        {
            return null;
        }

        try
        {
            var siblings = parent.GetDirectories();
            SortDirectories(siblings, ordering);
            var index = siblings.FindIndex(d => d.FullName.Equals(currentPath, StringComparison.OrdinalIgnoreCase));

            if (index <= 0)
            {
                var parentArchives = GetSortedArchivesInDirectory(parent.FullName, ordering);
                if (parentArchives.Length > 0)
                {
                    return parentArchives[^1].FullName;
                }
                return GetLastArchiveInPreviousSiblingOrAncestor(parent.FullName, ordering);
            }

            for (var i = index - 1; i >= 0; i--)
            {
                var sibling = siblings[i];
                var lastChild = GetLastArchiveInDescendantOrSelf(sibling.FullName, ordering);
                if (lastChild != null)
                {
                    return lastChild;
                }
            }

            var parentArchivesFallback = GetSortedArchivesInDirectory(parent.FullName, ordering);
            return parentArchivesFallback.Length > 0 ?
                parentArchivesFallback[^1].FullName : GetLastArchiveInPreviousSiblingOrAncestor(parent.FullName, ordering);
        }
        catch (Exception ex)
        {
            DebugHelper.LogDebug(nameof(FileAndDirectoryNavigator), nameof(GetLastArchiveInPreviousSiblingOrAncestor), ex);
            return null;
        }
    }

    public string? GetLastArchiveInDescendantOrSelf(string path, FileOrdering ordering)
    {
        if (!Settings.Sorting.IncludeSubDirectories)
        {
            var archives = GetSortedArchivesInDirectory(path, ordering);
            return archives.Length > 0 ? archives[^1].FullName : null;
        }

        try
        {
            var dir = new DirectoryInfo(path);
            var subDirs = dir.GetDirectories();
            SortDirectories(subDirs, ordering);

            for (var i = subDirs.Length - 1; i >= 0; i--)
            {
                var lastChild = GetLastArchiveInDescendantOrSelf(subDirs[i].FullName, ordering);
                if (lastChild != null) return lastChild;
            }
        }
        catch (Exception ex)
        {
            DebugHelper.LogDebug(nameof(FileAndDirectoryNavigator), nameof(GetLastArchiveInDescendantOrSelf), ex);
        }

        var archivesHere = GetSortedArchivesInDirectory(path, ordering);
        return archivesHere.Length > 0 ? archivesHere[^1].FullName : null;
    }

    public string? FindNextValidDirectory(string currentPath, FileOrdering ordering)
    {
        if (!Settings.Sorting.IncludeSubDirectories)
        {
            return GetNextSiblingOrAncestorSibling(currentPath, ordering);
        }

        var firstChild = GetFirstValidChild(currentPath, ordering);
        return firstChild ?? GetNextSiblingOrAncestorSibling(currentPath, ordering);
    }

    public string? GetFirstValidChild(string path, FileOrdering ordering)
    {
        try
        {
            var dir = new DirectoryInfo(path);
            var subDirs = dir.GetDirectories();
            SortDirectories(subDirs, ordering);

            foreach (var sub in subDirs)
            {
                if (IsDirectoryValid(sub.FullName)) return sub.FullName;
                if (!Settings.Sorting.IncludeSubDirectories)
                {
                    continue;
                }

                var child = GetFirstValidChild(sub.FullName, ordering);
                if (child != null)
                {
                    return child;
                }
            }
        }
        catch (Exception ex)
        {
            DebugHelper.LogDebug(nameof(FileAndDirectoryNavigator), nameof(GetFirstValidChild), ex);
        }
        return null;
    }

    public string? GetNextSiblingOrAncestorSibling(string path, FileOrdering ordering)
    {
        var dir = new DirectoryInfo(path);
        var parent = dir.Parent;
        if (parent == null)
        {
            return null;
        }

        try
        {
            var siblings = parent.GetDirectories();
            SortDirectories(siblings, ordering);
            var index = siblings.FindIndex(d => d.FullName.Equals(path, StringComparison.OrdinalIgnoreCase));

            for (var i = index + 1; i < siblings.Length; i++)
            {
                var sibling = siblings[i];
                if (IsDirectoryValid(sibling.FullName)) return sibling.FullName;
                if (!Settings.Sorting.IncludeSubDirectories)
                {
                    continue;
                }

                var child = GetFirstValidChild(sibling.FullName, ordering);
                if (child != null) return child;
            }
        }
        catch (Exception ex)
        {
            DebugHelper.LogDebug(nameof(FileAndDirectoryNavigator), nameof(GetNextSiblingOrAncestorSibling), ex);
        }

        return GetNextSiblingOrAncestorSibling(parent.FullName, ordering);
    }

    public string? FindPreviousValidDirectory(string currentPath, FileOrdering ordering)
    {
        var dir = new DirectoryInfo(currentPath);
        var parent = dir.Parent;
        if (parent is null)
        {
            return null;
        }

        try
        {
            var siblings = parent.GetDirectories();
            SortDirectories(siblings, ordering);
            var index = siblings.FindIndex(d => d.FullName.Equals(currentPath, StringComparison.OrdinalIgnoreCase));

            if (index <= 0)
            {
                return IsDirectoryValid(parent.FullName)
                    ? parent.FullName
                    : FindPreviousValidDirectory(parent.FullName, ordering);
            }

            for (var i = index - 1; i >= 0; i--)
            {
                var sibling = siblings[i];
                var lastChild = GetLastValidDescendantOrSelf(sibling.FullName, ordering);
                if (lastChild != null)
                {
                    return lastChild;
                }
            }

            return IsDirectoryValid(parent.FullName) ?
                parent.FullName : FindPreviousValidDirectory(parent.FullName, ordering);
        }
        catch (Exception ex)
        {
            DebugHelper.LogDebug(nameof(FileAndDirectoryNavigator), nameof(FindPreviousValidDirectory), ex);
            return null;
        }
    }

    public string? GetLastValidDescendantOrSelf(string path, FileOrdering ordering)
    {
        if (!Settings.Sorting.IncludeSubDirectories)
        {
            return IsDirectoryValid(path) ? path : null;
        }

        try
        {   
            var dir = new DirectoryInfo(path);
            var subDirs = dir.GetDirectories();
            SortDirectories(subDirs, ordering);

            for (var i = subDirs.Length - 1; i >= 0; i--)
            {
                var lastChild = GetLastValidDescendantOrSelf(subDirs[i].FullName, ordering);
                if (lastChild != null) return lastChild;
            }
        }
        catch (Exception ex)
        {
            DebugHelper.LogDebug(nameof(FileAndDirectoryNavigator), nameof(GetLastValidDescendantOrSelf), ex);
        }

        return IsDirectoryValid(path) ? path : null;
    }

    public bool IsDirectoryValid(string path)
    {
        try
        {
            var dir = new DirectoryInfo(path);
            if (!dir.Exists)
            {
                return false;
            }
            
            return Settings.Sorting.IncludeSubDirectories ?
                dir.EnumerateFiles("*", SearchOption.AllDirectories).Any(f => f.FullName.IsSupported()) :
                dir.EnumerateFiles("*", SearchOption.TopDirectoryOnly).Any(f => f.FullName.IsSupported());
        }
        catch (Exception ex)
        {
            DebugHelper.LogDebug(nameof(FileAndDirectoryNavigator), nameof(IsDirectoryValid), ex);
            return false;
        }
    }

    // Directories go by name and direction whatever the field files are sorted by
    public void SortDirectories(DirectoryInfo[] dirs, FileOrdering ordering)
    {
        dirs.Sort((x, y) => ordering.CompareNames(x.Name, y.Name));
    }

    private static FileInfo[] GetSortedArchivesInDirectory(string path, FileOrdering ordering)
    {
        try
        {
            var dir = new DirectoryInfo(path);
            if (!dir.Exists)
            {
                return [];
            }

            return ordering.Sort(dir.EnumerateFiles("*", SearchOption.TopDirectoryOnly)
                .Where(f => f.FullName.IsArchive())).ToArray();
        }
        catch (Exception ex)
        {
            DebugHelper.LogDebug(nameof(FileAndDirectoryNavigator), nameof(GetSortedArchivesInDirectory), ex);
            return [];
        }
    }
}
