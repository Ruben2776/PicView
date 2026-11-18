namespace PicView.Tests;

/// <summary>
/// Finds the source tree from the test output folder, for tests that read source files.
/// </summary>
internal static class SourceTree
{
    /// <summary>
    /// The folder that holds <c>PicView.slnx</c>.
    /// </summary>
    internal static string Src { get; } = FindSrc();

    private static string FindSrc()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir != null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "PicView.slnx")))
            {
                return dir.FullName;
            }
        }

        throw new InvalidOperationException("PicView.slnx not found above " + AppContext.BaseDirectory);
    }
}
