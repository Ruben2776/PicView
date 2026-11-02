using PicView.Core.ArchiveHandling;
using PicView.Core.ViewModels;

namespace PicView.Tests.ArchiveHandling;

/// <summary>
/// An extraction a test holds open on a tab, so the tab counts as extracting until the test ends it: an archive of
/// twelve pages opened at the first, whose eleventh is written until <see cref="EndAsync"/> lets it. The tab takes
/// <see cref="Writer"/> as its entry writer. The pages go to the opening's own folder, so a tab that lists other files
/// keeps its list.
/// </summary>
internal sealed class HeldExtraction : IDisposable
{
    private const string HeldPage = "img_11.jpg";

    // Bounds each wait, so a failing test fails instead of hanging the run
    private static readonly TimeSpan WaitLimit = TimeSpan.FromSeconds(10);

    private readonly HeldEntryWriter _writer = new(HeldPage);
    private ArchiveOpening? _opening;

    /// <summary>The entry writer to give the tab, which holds the eleventh page's write.</summary>
    public IArchiveEntryWriter Writer => _writer;

    /// <summary>The opening begun, once <see cref="BeginAsync"/> has run.</summary>
    public ArchiveOpening Opening => _opening ?? throw new InvalidOperationException("The extraction has not begun");

    /// <summary>
    /// Opens the archive on <paramref name="tab"/> and begins extracting the rest, with the archive in
    /// <paramref name="folder"/>. Returns once the eleventh page is being written, so the tab is extracting.
    /// </summary>
    public async Task BeginAsync(TabViewModel tab, string folder)
    {
        var zip = CreateZip(folder);
        _opening = await tab.Archives.OpenAsync(zip, null, (_, _) => Task.FromResult(false), string.CompareOrdinal,
            CancellationToken.None) ?? throw new InvalidOperationException("The archive did not open");
        _opening.ExtractRest();
        await _writer.Holding(HeldPage).WaitAsync(WaitLimit);
    }

    /// <summary>Lets the extraction finish, and returns once it has ended, as an archive's background extraction does.</summary>
    public async Task EndAsync()
    {
        _writer.Release(HeldPage);
        await Opening.Ended.WaitAsync(WaitLimit);
    }

    public void Dispose() => _writer.ReleaseAll();

    // A zip of twelve pages named img_01.jpg and on, in a folder of its own
    private static string CreateZip(string folder)
    {
        var own = Directory.CreateDirectory(Path.Combine(folder, Guid.NewGuid().ToString("N"))).FullName;
        var source = Directory.CreateDirectory(Path.Combine(own, "source")).FullName;
        for (var i = 1; i <= 12; i++)
        {
            File.WriteAllBytes(Path.Combine(source, $"img_{i:D2}.jpg"), [0xFF, 0xD8, 0xFF]);
        }

        var zip = Path.Combine(own, "held.zip");
        System.IO.Compression.ZipFile.CreateFromDirectory(source, zip);
        return zip;
    }
}
