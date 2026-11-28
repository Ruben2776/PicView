using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using PicView.Core.DebugTools;
using PicView.Core.FileSorting;

namespace PicView.Core.MacOS.FileSorting;

/// <summary>
/// The order Finder shows a folder in, from two sources (ADR-0050, On macOS): its open windows, asked with Apple
/// Events, and the view it saved for the folder in its parent's <c>.DS_Store</c>, which is read only for a folder a
/// window shows. The events are sent in-process with <c>AESendMessage</c>, which may be called from any thread and
/// takes a timeout, and wait on the Finder process: call from a pool thread. One ask has half a second for all of
/// its events together. macOS asks the user to let PicView control Finder only in <see cref="RequestAccess"/>: an
/// event never raises that question, and until the user has allowed it every event fails, which reads as no answer.
/// </summary>
public sealed unsafe partial class FinderFolderOrder : IFolderOrderSource
{
    private const string CoreServices = "/System/Library/Frameworks/CoreServices.framework/CoreServices";
    private const string FinderBundle = "com.apple.finder";

    private static readonly uint BundleId = FinderSort.Code("bund");
    private static readonly uint CoreSuite = FinderSort.Code("core");
    private static readonly uint GetData = FinderSort.Code("getd");
    private static readonly uint Direct = FinderSort.Code("----");
    private static readonly uint RequestedType = FinderSort.Code("rtyp");
    private static readonly uint ErrorNumber = FinderSort.Code("errn");
    private static readonly uint Wildcard = FinderSort.Code("****");
    private static readonly uint TypeNull = FinderSort.Code("null");
    private static readonly uint TypeType = FinderSort.Code("type");
    private static readonly uint TypeLong = FinderSort.Code("long");
    private static readonly uint TypeList = FinderSort.Code("list");
    private static readonly uint TypeAlias = FinderSort.Code("alis");
    private static readonly uint TypeFileUrl = FinderSort.Code("furl");
    private static readonly uint TypeOrdinal = FinderSort.Code("abso");
    private static readonly uint Every = FinderSort.Code("all ");
    private static readonly uint ByIndex = FinderSort.Code("indx");
    private static readonly uint ByProperty = FinderSort.Code("prop");

    // Finder's terms
    private static readonly uint FinderWindowClass = FinderSort.Code("brow");
    private static readonly uint Target = FinderSort.Code("fvtg");
    private static readonly uint CurrentView = FinderSort.Code("pvew");
    private static readonly uint ListViewOptions = FinderSort.Code("lvop");
    private static readonly uint SortColumn = FinderSort.Code("srtc");
    private static readonly uint ColumnName = FinderSort.Code("pnam");
    private static readonly uint SortDirection = FinderSort.Code("sord");
    private static readonly uint IconViewOptions = FinderSort.Code("icop");
    private static readonly uint IconArrangement = FinderSort.Code("iarr");

    private const int Ok = 0;
    private const int NotPermitted = -1743;             // errAEEventNotPermitted: the user refused
    private const int WaitReply = 0x3;                  // kAEWaitReply
    private const int NeverInteract = 0x10;             // kAENeverInteract
    private const int NeverAskConsent = 0x20000;        // kAEDoNotPromptForUserConsent
    private const int TimedOut = -1712;                 // errAETimeout
    private const short AnyReturnId = -1;               // kAutoGenerateReturnID
    private const int AnyTransaction = 0;               // kAnyTransactionID
    private const int TicksASecond = 60;                // an Apple Event's timeout is in ticks
    private const int MostWindows = 64;
    private const int MostSavedBytes = 4 * 1024 * 1024; // a longer .DS_Store is not read
    private const string SavedViewFile = ".DS_Store";

    // An AEDesc. The Apple Event headers pack their structures to two bytes, so the pointer follows the type at once
    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    private struct Descriptor
    {
        public uint Type;
        public nint Data;
    }

    [LibraryImport(CoreServices)]
    private static partial short AECreateDesc(uint type, void* data, nint size, Descriptor* result);

    [LibraryImport(CoreServices)]
    private static partial short AEDisposeDesc(Descriptor* descriptor);

    [LibraryImport(CoreServices)]
    private static partial short AECoerceDesc(Descriptor* descriptor, uint toType, Descriptor* result);

    [LibraryImport(CoreServices)]
    private static partial nint AEGetDescDataSize(Descriptor* descriptor);

    [LibraryImport(CoreServices)]
    private static partial short AEGetDescData(Descriptor* descriptor, void* data, nint maximumSize);

    [LibraryImport(CoreServices)]
    private static partial short AECountItems(Descriptor* list, nint* count);

    [LibraryImport(CoreServices)]
    private static partial short AEGetNthDesc(Descriptor* list, nint index, uint desiredType, uint* keyword,
        Descriptor* result);

    [LibraryImport(CoreServices)]
    private static partial short CreateObjSpecifier(uint desiredClass, Descriptor* container, uint keyForm,
        Descriptor* keyData, byte disposeInputs, Descriptor* result);

    [LibraryImport(CoreServices)]
    private static partial short AECreateAppleEvent(uint eventClass, uint eventId, Descriptor* target, short returnId,
        int transactionId, Descriptor* result);

    [LibraryImport(CoreServices)]
    private static partial short AEPutParamDesc(Descriptor* appleEvent, uint keyword, Descriptor* parameter);

    [LibraryImport(CoreServices)]
    private static partial short AEGetParamDesc(Descriptor* appleEvent, uint keyword, uint desiredType,
        Descriptor* result);

    [LibraryImport(CoreServices)]
    private static partial short AEGetParamPtr(Descriptor* appleEvent, uint keyword, uint desiredType,
        uint* actualType, void* data, nint maximumSize, nint* actualSize);

    [LibraryImport(CoreServices)]
    private static partial int AESendMessage(Descriptor* appleEvent, Descriptor* reply, int sendMode,
        nint timeOutInTicks);

    [LibraryImport(CoreServices)]
    private static partial int AEDeterminePermissionToAutomateTarget(Descriptor* target, uint eventClass,
        uint eventId, byte askUserIfNeeded);

    [LibraryImport("libc", StringMarshalling = StringMarshalling.Utf8)]
    private static partial nint realpath(string path, nint resolved);

    [LibraryImport("libc")]
    private static partial void free(nint memory);

    /// <summary>
    /// The order of the Finder window furthest in front that shows <paramref name="directory"/>, or null: when none
    /// shows it, when that one is in a view, sort, arrangement or group no File Order gives, or when Finder could not
    /// be asked.
    /// </summary>
    public FileOrder? OrderOf(string directory)
    {
        if (!OperatingSystem.IsMacOS())
        {
            return null;
        }

        try
        {
            var asked = Resolved(directory);
            // For every event of the ask together, so a slow Finder with many windows holds the thread no longer
            var deadline = Stopwatch.GetTimestamp() + Stopwatch.Frequency / 2;
            var windows = Windows(asked, deadline);
            if (windows is null)
            {
                return null;
            }

            FinderSavedView? saved = null;
            if (Stopwatch.GetTimestamp() < deadline && ShownBy(windows, asked))
            {
                saved = SavedView(asked);
            }

            return FinderSort.Followed(windows, asked, saved);
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(FinderFolderOrder), nameof(OrderOf), e);
            return null;
        }
    }

    /// <summary>
    /// The <c>.DS_Store</c> that holds the saved view of <paramref name="directory"/>, which is its parent's, and the
    /// name the folder has in it; null for a folder with no parent.
    /// </summary>
    internal static (string File, string Name)? SavedViewLocation(string directory)
    {
        var trimmed = Path.TrimEndingDirectorySeparator(directory);
        var parent = Path.GetDirectoryName(trimmed);
        var name = Path.GetFileName(trimmed);
        return string.IsNullOrEmpty(parent) || name.Length == 0
            ? null
            : (Path.Combine(parent, SavedViewFile), name);
    }

    /// <summary>
    /// What Finder saved for <paramref name="directory"/> in its parent's <c>.DS_Store</c>. Null when the file is
    /// missing, cannot be read, is longer than 4 MiB, or holds nothing for the folder. Never throws: Finder may be
    /// writing the file as it is read.
    /// </summary>
    internal static FinderSavedView? SavedView(string directory)
    {
        try
        {
            if (SavedViewLocation(directory) is not var (file, name))
            {
                return null;
            }

            using var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var length = stream.Length;
            if (length is <= 0 or > MostSavedBytes)
            {
                return null;
            }

            var bytes = new byte[length];
            stream.ReadExactly(bytes);
            return DsStore.ViewOf(bytes, name);
        }
        catch (Exception e) when (e is FileNotFoundException or DirectoryNotFoundException)
        {
            // No file: the usual case, and no answer
            return null;
        }
        catch (Exception e)
        {
            // A file that is there but cannot be read, or is in the middle of a write: no answer
            DebugHelper.LogDebug(nameof(FinderFolderOrder), nameof(SavedView), e);
            return null;
        }
    }

    private static bool ShownBy(FinderWindow[] windows, string directory)
    {
        foreach (var window in windows)
        {
            if (FinderSort.Shows(window.Folder, directory))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Has macOS ask the user to let PicView control Finder, where they have not answered yet, and waits for the
    /// answer. False when they refuse, or refused before; true otherwise, as when Finder is not running.
    /// </summary>
    public bool RequestAccess()
    {
        if (!OperatingSystem.IsMacOS())
        {
            return true;
        }

        try
        {
            var finder = Finder();
            var status = AEDeterminePermissionToAutomateTarget(&finder, Wildcard, Wildcard, 1);
            AEDisposeDesc(&finder);
            return status != NotPermitted;
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(FinderFolderOrder), nameof(RequestAccess), e);
            return true;
        }
    }

    // Finder's windows, front to back, with the sort read only of the first that shows the folder, as no other is
    // followed. Null when Finder did not tell its windows
    private static FinderWindow[]? Windows(string asked, long deadline)
    {
        if (Codes(Property(CurrentView, EveryWindow()), deadline) is not { Length: > 0 } views)
        {
            return null;
        }

        var count = Math.Min(views.Length, MostWindows);
        var folders = Folders(count, asked, deadline);
        var windows = new FinderWindow[count];
        var found = false;
        for (var i = 0; i < count; i++)
        {
            uint column = 0, direction = 0, arrangement = 0;
            if (!found && FinderSort.Shows(folders[i], asked))
            {
                found = true;
                if (views[i] == FinderSort.ListView)
                {
                    column = Code(Property(ColumnName,
                        Property(SortColumn, Property(ListViewOptions, Window(i + 1)))), deadline);
                    direction = Code(Property(SortDirection,
                        Property(SortColumn, Property(ListViewOptions, Window(i + 1)))), deadline);
                }
                else if (views[i] == FinderSort.IconView)
                {
                    arrangement = Code(Property(IconArrangement,
                        Property(IconViewOptions, Window(i + 1))), deadline);
                }
            }

            windows[i] = new FinderWindow(folders[i], views[i], column, direction, arrangement);
        }

        return windows;
    }

    // The folder each window shows. All are asked for at once. Where Finder refuses that, as it does when a window
    // shows what is no folder on a disk, each is asked for its own, front to back and no further than the first that
    // shows the folder asked for, as no window behind it is followed
    private static string?[] Folders(int count, string asked, long deadline)
    {
        var folders = new string?[count];
        if (Get(Property(Target, EveryWindow()), TypeAlias, deadline, out var all) == Ok)
        {
            var told = false;
            try
            {
                if (all.Type == TypeList && Count(&all) >= count)
                {
                    for (var i = 0; i < count; i++)
                    {
                        Descriptor item;
                        uint keyword;
                        if (AEGetNthDesc(&all, i + 1, Wildcard, &keyword, &item) == Ok)
                        {
                            folders[i] = PathOf(&item);
                            AEDisposeDesc(&item);
                        }
                    }

                    told = true;
                }
            }
            finally
            {
                AEDisposeDesc(&all);
            }

            if (told)
            {
                return folders;
            }
        }

        for (var i = 0; i < count; i++)
        {
            var status = Get(Property(Target, Window(i + 1)), TypeAlias, deadline, out var one);
            if (status == TimedOut)
            {
                break;
            }

            if (status != Ok)
            {
                continue;
            }

            folders[i] = PathOf(&one);
            AEDisposeDesc(&one);
            if (FinderSort.Shows(folders[i], asked))
            {
                break;
            }
        }

        return folders;
    }

    // The path an alias or file URL names
    private static string? PathOf(Descriptor* file)
    {
        Descriptor url;
        if (AECoerceDesc(file, TypeFileUrl, &url) != Ok)
        {
            return null;
        }

        try
        {
            var size = (int)AEGetDescDataSize(&url);
            if (size is <= 0 or > 16384)
            {
                return null;
            }

            var bytes = stackalloc byte[size];
            if (AEGetDescData(&url, bytes, size) != Ok)
            {
                return null;
            }

            return Uri.TryCreate(Encoding.UTF8.GetString(bytes, size), UriKind.Absolute, out var uri) && uri.IsFile
                ? uri.LocalPath
                : null;
        }
        finally
        {
            AEDisposeDesc(&url);
        }
    }

    // The codes Finder answers a request for one of every window with; null when it gives none
    private static uint[]? Codes(Descriptor what, long deadline)
    {
        if (Get(what, 0, deadline, out var answer) != Ok)
        {
            return null;
        }

        try
        {
            if (answer.Type != TypeList)
            {
                return [CodeOf(&answer)];
            }

            var codes = new uint[Count(&answer)];
            for (var i = 0; i < codes.Length; i++)
            {
                Descriptor item;
                uint keyword;
                if (AEGetNthDesc(&answer, i + 1, Wildcard, &keyword, &item) == Ok)
                {
                    codes[i] = CodeOf(&item);
                    AEDisposeDesc(&item);
                }
            }

            return codes;
        }
        finally
        {
            AEDisposeDesc(&answer);
        }
    }

    // The code Finder answers a request with; 0 when it gives none
    private static uint Code(Descriptor what, long deadline)
    {
        if (Get(what, 0, deadline, out var answer) != Ok)
        {
            return 0;
        }

        var code = CodeOf(&answer);
        AEDisposeDesc(&answer);
        return code;
    }

    private static uint CodeOf(Descriptor* descriptor)
    {
        uint code = 0;
        return AEGetDescDataSize(descriptor) == sizeof(uint) && AEGetDescData(descriptor, &code, sizeof(uint)) == Ok
            ? code
            : 0;
    }

    private static int Count(Descriptor* list)
    {
        nint count = 0;
        return AECountItems(list, &count) == Ok ? (int)count : 0;
    }

    // Sends Finder a request for what, which it disposes of, and waits for the answer until the deadline, which is a
    // Stopwatch timestamp; past it nothing is sent. The answer is the caller's to dispose of when the result is Ok
    private static int Get(Descriptor what, uint requested, long deadline, out Descriptor answer)
    {
        answer = default;
        Descriptor finder = Application(), request = Application(), reply = Application();
        try
        {
            var ticksLeft = (deadline - Stopwatch.GetTimestamp()) * TicksASecond / Stopwatch.Frequency;
            if (ticksLeft <= 0)
            {
                return TimedOut;
            }

            finder = Finder();
            int status = AECreateAppleEvent(CoreSuite, GetData, &finder, AnyReturnId, AnyTransaction, &request);
            if (status != Ok)
            {
                return status;
            }

            status = AEPutParamDesc(&request, Direct, &what);
            if (status == Ok && requested != 0)
            {
                Descriptor type;
                status = AECreateDesc(TypeType, &requested, sizeof(uint), &type);
                if (status == Ok)
                {
                    status = AEPutParamDesc(&request, RequestedType, &type);
                    AEDisposeDesc(&type);
                }
            }

            if (status != Ok)
            {
                return status;
            }

            status = AESendMessage(&request, &reply, WaitReply | NeverInteract | NeverAskConsent, (nint)ticksLeft);
            if (status != Ok)
            {
                return status;
            }

            // Finder's own refusal comes in the reply, as for a window that is gone
            int error = 0;
            uint actualType;
            nint actualSize;
            if (AEGetParamPtr(&reply, ErrorNumber, TypeLong, &actualType, &error, sizeof(int), &actualSize) == Ok &&
                error != Ok)
            {
                return error;
            }

            fixed (Descriptor* result = &answer)
            {
                return AEGetParamDesc(&reply, Direct, Wildcard, result);
            }
        }
        finally
        {
            AEDisposeDesc(&what);
            AEDisposeDesc(&finder);
            AEDisposeDesc(&request);
            AEDisposeDesc(&reply);
        }
    }

    // Finder, addressed by its bundle identifier
    private static Descriptor Finder()
    {
        var bundle = FinderBundle;
        var bytes = stackalloc byte[bundle.Length];
        var length = Encoding.ASCII.GetBytes(bundle, new Span<byte>(bytes, bundle.Length));
        Descriptor finder;
        Check(AECreateDesc(BundleId, bytes, length, &finder));
        return finder;
    }

    // "every Finder window"
    private static Descriptor EveryWindow()
    {
        var every = Every;
        Descriptor all;
        Check(AECreateDesc(TypeOrdinal, &every, sizeof(uint), &all));
        return Specifier(FinderWindowClass, ByIndex, all, Application());
    }

    // "Finder window index", the first being the one in front
    private static Descriptor Window(int index)
    {
        Descriptor number;
        Check(AECreateDesc(TypeLong, &index, sizeof(int), &number));
        return Specifier(FinderWindowClass, ByIndex, number, Application());
    }

    // "property of container", which it disposes of
    private static Descriptor Property(uint property, Descriptor of)
    {
        Descriptor name;
        var created = AECreateDesc(TypeType, &property, sizeof(uint), &name);
        if (created != Ok)
        {
            AEDisposeDesc(&of);
            Check(created);
        }

        return Specifier(ByProperty, ByProperty, name, of);
    }

    // The null descriptor: the application itself as a container, and a descriptor that holds nothing yet
    private static Descriptor Application() => new() { Type = TypeNull };

    // Disposes of the key and the container, whether it succeeds or not
    private static Descriptor Specifier(uint wanted, uint form, Descriptor key, Descriptor container)
    {
        Descriptor specifier;
        Check(CreateObjSpecifier(wanted, &container, form, &key, 1, &specifier));
        return specifier;
    }

    private static void Check(int status)
    {
        if (status != Ok)
        {
            throw new InvalidOperationException($"Apple Event error {status}");
        }
    }

    // The path with its symbolic links followed, as Finder names a folder; the path as given when it cannot be
    private static string Resolved(string directory)
    {
        var resolved = realpath(directory, 0);
        if (resolved == 0)
        {
            return directory;
        }

        try
        {
            return Marshal.PtrToStringUTF8(resolved) ?? directory;
        }
        finally
        {
            free(resolved);
        }
    }
}
