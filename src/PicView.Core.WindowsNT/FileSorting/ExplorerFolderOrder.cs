using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using PicView.Core.DebugTools;
using PicView.Core.FileSorting;

namespace PicView.Core.WindowsNT.FileSorting;

/// <summary>
/// The order Windows Explorer's open windows show a folder in, read from each Explorer view of it through the shell's
/// COM interfaces. The calls go to the Explorer process, so they can wait on it: call from a pool thread, and bound
/// the wait. The views are read on a thread of their own in a single-threaded apartment, as an Explorer tab only tells
/// its window to one (<c>IOleWindow::GetWindow</c> is input-synchronous), and the window tells which of an Explorer
/// window's tabs is shown. The interfaces are called through their vtables, as <c>IShellWindows</c> is an <c>IDispatch</c>
/// interface, which source-generated COM does not declare, and plain function pointers need no marshalling code
/// under Native AOT.
/// </summary>
public sealed unsafe partial class ExplorerFolderOrder : IFolderOrderSource
{
    private static readonly Guid ClsidShellWindows = new("9BA05972-F6A8-11CF-A442-00A0C90A8F39");
    private static readonly Guid IidShellWindows = new("85CB6900-4D95-11CF-960C-0080C7F4EE85");
    private static readonly Guid IidServiceProvider = new("6D5140C1-7436-11CE-8034-00AA006009FA");
    private static readonly Guid SidTopLevelBrowser = new("4C96BE40-915C-11CF-99D3-00AA004AE837");
    private static readonly Guid IidShellBrowser = new("000214E2-0000-0000-C000-000000000046");
    private static readonly Guid IidFolderView2 = new("1AF3A467-214F-4298-908E-06B03E0B39F9");
    private static readonly Guid IidPersistFolder2 = new("1AC3D9F0-175C-11D1-95BE-00609797EA4F");
    private static readonly Guid IidWebBrowserApp = new("0002DF05-0000-0000-C000-000000000046");

    // Vtable slots, counted from IUnknown's three
    private const int ShellWindowsGetCount = 7;         // after IDispatch's four
    private const int ShellWindowsItem = 8;
    private const int WebBrowserAppGetHwnd = 37;        // after IDispatch's four, IWebBrowser's 25 and five of its own
    private const int ServiceProviderQueryService = 3;
    private const int ShellBrowserQueryActiveShellView = 15; // after IOleWindow's two and ten of its own
    private const int OleWindowGetWindow = 3;           // IShellBrowser starts with IOleWindow
    private const int FolderViewGetFolder = 5;
    private const int FolderView2GetGroupBy = 18;       // after IFolderView's fourteen and SetGroupBy
    private const int FolderView2GetSortColumnCount = 26;
    private const int FolderView2GetSortColumns = 28;
    private const int PersistFolder2GetCurFolder = 5;   // after IPersist's and IPersistFolder's one each

    private const int Ok = 0;
    private const uint ClsctxLocalServer = 0x4;
    private const int CoENotInitialized = unchecked((int)0x800401F0);
    private const ushort VtI4 = 3;
    private const int LongPath = 32768;
    private const int MostSortColumns = 8;
    private const uint NextWindow = 2;                  // GW_HWNDNEXT
    private const int MostWindows = 4096;
    private const int NotInFront = int.MaxValue;
    private const string TabWindowClass = "ShellTabWindowClass";

    [StructLayout(LayoutKind.Sequential)]
    private struct SortColumn
    {
        public ShellPropertyKey Key;
        public int Direction;
    }

    // A VARIANT holding a number: 24 bytes on the 64-bit platforms PicView builds for
    [StructLayout(LayoutKind.Sequential)]
    private struct Variant
    {
        public ushort Type;
        public ushort Reserved1;
        public ushort Reserved2;
        public ushort Reserved3;
        public long Value;
        public nint Padding;
    }

    [LibraryImport("ole32.dll")]
    private static partial int CoCreateInstance(in Guid clsid, nint outer, uint context, in Guid iid, out nint instance);

    [LibraryImport("ole32.dll")]
    private static partial int CoInitializeEx(nint reserved, uint model);

    [LibraryImport("ole32.dll")]
    private static partial void CoTaskMemFree(nint memory);

    [LibraryImport("shell32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SHGetPathFromIDListEx(nint idList, char* path, uint pathLength, uint options);

    [LibraryImport("user32.dll")]
    private static partial nint FindWindowExW(nint parent, nint after, char* className, char* title);

    [LibraryImport("user32.dll")]
    private static partial nint GetTopWindow(nint parent);

    [LibraryImport("user32.dll")]
    private static partial nint GetWindow(nint window, uint which);

    /// <inheritdoc />
    public FileOrder? OrderOf(string directory)
    {
        if (!OperatingSystem.IsWindows() || DirectoryOrders.KeyOf(directory) is not { } key)
        {
            return null;
        }

        try
        {
            return ExplorerSort.Followed(ViewsOf(key));
        }
        catch (Exception e)
        {
            // Explorer gone or restarting mid-call: the folder is then listed as if no window showed it
            DebugHelper.LogDebug(nameof(ExplorerFolderOrder), nameof(OrderOf), e);
            return null;
        }
    }

    /// <summary>
    /// How each Explorer view that shows <paramref name="directory"/> shows it: one per Explorer tab on it, in any
    /// Explorer window, with whether its window shows that tab now and how far in front the window is.
    /// </summary>
    public static List<ExplorerView> ViewsOf(string directory)
    {
        if (Thread.CurrentThread.GetApartmentState() is ApartmentState.STA)
        {
            return ReadViews(directory);
        }

        List<ExplorerView> views = [];
        ExceptionDispatchInfo? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                views = ReadViews(directory);
            }
            catch (Exception e)
            {
                failure = ExceptionDispatchInfo.Capture(e);
            }
        })
        {
            IsBackground = true,
            Name = nameof(ExplorerFolderOrder)
        };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        failure?.Throw();
        return views;
    }

    private static List<ExplorerView> ReadViews(string directory)
    {
        var views = new List<ExplorerView>();
        // The Explorer window of each view, beside it
        var roots = new List<nint>();
        var result = CoCreateInstance(ClsidShellWindows, 0, ClsctxLocalServer, IidShellWindows, out var windows);
        if (result == CoENotInitialized)
        {
            // A thread no one gave an apartment, which a pool thread has: multithreaded, as the pool's are. Left
            // initialized, as the thread goes back to the pool, whose threads are
            _ = CoInitializeEx(0, 0);
            result = CoCreateInstance(ClsidShellWindows, 0, ClsctxLocalServer, IidShellWindows, out windows);
        }

        if (result != Ok || windows == 0)
        {
            return views;
        }

        try
        {
            int count;
            var getCount = (delegate* unmanaged[Stdcall]<nint, int*, int>)Slot(windows, ShellWindowsGetCount);
            if (getCount(windows, &count) != Ok)
            {
                return views;
            }

            var item = (delegate* unmanaged[Stdcall]<nint, Variant, nint*, int>)Slot(windows, ShellWindowsItem);
            // One buffer for the paths of every window
            var path = new char[LongPath];
            for (var i = 0; i < count; i++)
            {
                nint entry = 0;
                // A window that closed since the count gives no entry
                if (item(windows, new Variant { Type = VtI4, Value = i }, &entry) != Ok || entry == 0)
                {
                    continue;
                }

                try
                {
                    if (ViewOf(entry, directory, path, out var root) is { } view)
                    {
                        views.Add(view);
                        roots.Add(root);
                    }
                }
                finally
                {
                    Marshal.Release(entry);
                }
            }
        }
        finally
        {
            Marshal.Release(windows);
        }

        // One view needs no telling apart
        if (views.Count > 1)
        {
            for (var i = 0; i < views.Count; i++)
            {
                views[i] = views[i] with { Depth = DepthOf(roots[i]) };
            }
        }

        return views;
    }

    // How many top-level windows are in front of the window, which the desktop keeps in the order they were last
    // used in
    private static int DepthOf(nint root)
    {
        if (root == 0)
        {
            return NotInFront;
        }

        var window = GetTopWindow(0);
        // Bounded, as the windows change order while they are walked
        for (var depth = 0; window != 0 && depth < MostWindows; depth++)
        {
            if (window == root)
            {
                return depth;
            }

            window = GetWindow(window, NextWindow);
        }

        return NotInFront;
    }

    // How the Explorer view of one shell window shows the directory, or null when it shows another folder, or is
    // not a folder view at all
    private static ExplorerView? ViewOf(nint entry, string directory, char[] pathBuffer, out nint root)
    {
        root = 0;
        nint provider = 0, browser = 0, shellView = 0, folderView = 0, app = 0;
        try
        {
            if (Marshal.QueryInterface(entry, in IidServiceProvider, out provider) != Ok || provider == 0)
            {
                return null;
            }

            var service = SidTopLevelBrowser;
            var browserId = IidShellBrowser;
            var queryService = (delegate* unmanaged[Stdcall]<nint, Guid*, Guid*, nint*, int>)
                Slot(provider, ServiceProviderQueryService);
            if (queryService(provider, &service, &browserId, &browser) != Ok || browser == 0)
            {
                return null;
            }

            var queryActiveShellView = (delegate* unmanaged[Stdcall]<nint, nint*, int>)
                Slot(browser, ShellBrowserQueryActiveShellView);
            if (queryActiveShellView(browser, &shellView) != Ok || shellView == 0)
            {
                return null;
            }

            if (Marshal.QueryInterface(shellView, in IidFolderView2, out folderView) != Ok || folderView == 0)
            {
                return null;
            }

            if (!string.Equals(DirectoryOrders.KeyOf(PathOf(folderView, pathBuffer)), directory,
                    StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            // The Explorer window the view is in, which every tab of it gives
            if (Marshal.QueryInterface(entry, in IidWebBrowserApp, out app) == Ok && app != 0)
            {
                nint window = 0;
                var getHwnd = (delegate* unmanaged[Stdcall]<nint, nint*, int>)Slot(app, WebBrowserAppGetHwnd);
                if (getHwnd(app, &window) == Ok)
                {
                    root = window;
                }
            }

            // Each tab has a window of its own in the Explorer window, and the tab shown is the first of them. A
            // tab that does not tell its window, as to a thread in another apartment than a single-threaded one, or
            // an Explorer window without tabs, counts as shown
            nint tabWindow = 0;
            var getWindow = (delegate* unmanaged[Stdcall]<nint, nint*, int>)Slot(browser, OleWindowGetWindow);
            var isShown = true;
            if (root != 0 && getWindow(browser, &tabWindow) == Ok && tabWindow != 0)
            {
                nint shownTab;
                fixed (char* tabClass = TabWindowClass)
                {
                    shownTab = FindWindowExW(root, 0, tabClass, null);
                }

                isShown = shownTab == 0 || shownTab == tabWindow;
            }

            return Read(folderView) with { IsShown = isShown };
        }
        finally
        {
            Release(app);
            Release(folderView);
            Release(shellView);
            Release(browser);
            Release(provider);
        }
    }

    // The file system path of the folder a view shows; null for a folder that has none, as search results
    private static string? PathOf(nint folderView, char[] buffer)
    {
        nint folder = 0, idList = 0;
        try
        {
            var folderId = IidPersistFolder2;
            var getFolder = (delegate* unmanaged[Stdcall]<nint, Guid*, nint*, int>)
                Slot(folderView, FolderViewGetFolder);
            if (getFolder(folderView, &folderId, &folder) != Ok || folder == 0)
            {
                return null;
            }

            var getCurFolder = (delegate* unmanaged[Stdcall]<nint, nint*, int>)
                Slot(folder, PersistFolder2GetCurFolder);
            if (getCurFolder(folder, &idList) != Ok || idList == 0)
            {
                return null;
            }

            fixed (char* path = buffer)
            {
                return SHGetPathFromIDListEx(idList, path, (uint)buffer.Length, 0) ? new string(path) : null;
            }
        }
        finally
        {
            if (idList != 0)
            {
                CoTaskMemFree(idList);
            }

            Release(folder);
        }
    }

    // The first column the view is sorted by, with its direction, and the column it is grouped by
    private static ExplorerView Read(nint folderView)
    {
        ShellPropertyKey? sortedBy = null;
        var ascending = true;
        int count;
        var getSortColumnCount = (delegate* unmanaged[Stdcall]<nint, int*, int>)
            Slot(folderView, FolderView2GetSortColumnCount);
        if (getSortColumnCount(folderView, &count) == Ok && count > 0)
        {
            // Room for every column the view is sorted by, of which the first is the one read, as a view sorted by
            // more than one may not answer into less. A view sorted by more than these is not followed
            var columns = stackalloc SortColumn[MostSortColumns];
            var getSortColumns = (delegate* unmanaged[Stdcall]<nint, SortColumn*, int, int>)
                Slot(folderView, FolderView2GetSortColumns);
            if (count <= MostSortColumns && getSortColumns(folderView, columns, count) == Ok)
            {
                sortedBy = columns[0].Key;
                // SORT_ASCENDING is 1 and SORT_DESCENDING is -1
                ascending = columns[0].Direction > 0;
            }
        }

        ShellPropertyKey? groupedBy = null;
        ShellPropertyKey group;
        int groupAscending;
        var getGroupBy = (delegate* unmanaged[Stdcall]<nint, ShellPropertyKey*, int*, int>)
            Slot(folderView, FolderView2GetGroupBy);
        // S_FALSE, with an empty key, for a view that is not grouped
        var groupsAscending = true;
        if (getGroupBy(folderView, &group, &groupAscending) == Ok && group.FormatId != Guid.Empty)
        {
            groupedBy = group;
            groupsAscending = groupAscending != 0;
        }

        return new ExplorerView(sortedBy, ascending, groupedBy, groupsAscending);
    }

    private static void* Slot(nint instance, int slot) => (*(void***)instance)[slot];

    private static void Release(nint instance)
    {
        if (instance != 0)
        {
            Marshal.Release(instance);
        }
    }
}
