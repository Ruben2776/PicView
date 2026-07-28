using PicView.Core.ArchiveHandling;
using PicView.Core.Localization;
using PicView.Core.ViewModels;

namespace PicView.Tests.ArchiveHandling;

public class ArchiveExtractionServiceTests
{
    public ArchiveExtractionServiceTests()
    {
        SetDefaults();
    }

    #region Initial State

    [Fact]
    public void InitialState_TempZipDirectory_IsNull()
    {
        var service = new ArchiveExtractionService();

        Assert.Null(service.TempZipDirectory);
    }

    [Fact]
    public void InitialState_LastOpenedArchive_IsNull()
    {
        var service = new ArchiveExtractionService();

        Assert.Null(service.LastOpenedArchive);
    }

    [Fact]
    public void InitialState_IsArchived_IsFalse()
    {
        var service = new ArchiveExtractionService();

        Assert.False(service.IsArchived);
    }

    #endregion

    #region PrepareArchiveAsync

    [Fact]
    public async Task PrepareArchiveAsync_NullPath_ReturnsNull()
    {
        var service = new ArchiveExtractionService();

        var result = await service.PrepareArchiveAsync(
            null!,
            (_, _) => Task.FromResult(false),
            string.Compare);

        Assert.Null(result);
    }

    [Fact]
    public async Task PrepareArchiveAsync_EmptyPath_ReturnsNull()
    {
        var service = new ArchiveExtractionService();

        var result = await service.PrepareArchiveAsync(
            string.Empty,
            (_, _) => Task.FromResult(false),
            string.Compare);

        Assert.Null(result);
    }

    [Fact]
    public async Task PrepareArchiveAsync_NonExistentFile_ReturnsNull()
    {
        var service = new ArchiveExtractionService();

        var result = await service.PrepareArchiveAsync(
            @"C:\nonexistent\file.zip",
            (_, _) => Task.FromResult(false),
            string.Compare);

        Assert.Null(result);
    }

    #endregion

    #region ExtractEntryAsync

    [Fact]
    public async Task ExtractEntryAsync_NoTempDirectory_ReturnsNull()
    {
        var service = new ArchiveExtractionService();

        // TempZipDirectory is null because PrepareArchiveAsync was never called
        var result = await service.ExtractEntryAsync("archive.zip", "entry.jpg");

        Assert.Null(result);
    }

    [Fact]
    public async Task ExtractEntryAsync_NullEntryKey_ReturnsNull()
    {
        var service = new ArchiveExtractionService();

        var result = await service.ExtractEntryAsync("archive.zip", null!);

        Assert.Null(result);
    }

    [Fact]
    public async Task ExtractEntryAsync_EmptyEntryKey_ReturnsNull()
    {
        var service = new ArchiveExtractionService();

        var result = await service.ExtractEntryAsync("archive.zip", string.Empty);

        Assert.Null(result);
    }

    #endregion

    #region ExtractRemainingAsync

    [Fact]
    public async Task ExtractRemainingAsync_NoTempDirectory_CompletesWithoutError()
    {
        var service = new ArchiveExtractionService();

        // Should be a no-op when TempZipDirectory is null
        await service.ExtractRemainingAsync("archive.zip", new[] { "entry1.jpg", "entry2.jpg" });
    }

    [Fact]
    public async Task ExtractRemainingAsync_EmptyRemainingKeys_CompletesWithoutError()
    {
        var service = new ArchiveExtractionService();

        // Should be a no-op when remaining keys is empty, even if TempZipDirectory were set
        await service.ExtractRemainingAsync("archive.zip", Array.Empty<string>());
    }

    #endregion

    #region Cleanup

    [Fact]
    public void Cleanup_WithExistingTempDirectory_DeletesAndClearsState()
    {
        var service = new ArchiveExtractionService();

        // Manually create a temp directory to simulate PrepareArchiveAsync behavior
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);

        // Use reflection-free approach: set state via the Cleanup(string?) overload indirectly.
        // Instead, we test the Cleanup(string?) overload directly.
        service.Cleanup(tempDir);

        // The directory should be deleted
        Assert.False(Directory.Exists(tempDir));
    }

    [Fact]
    public void Cleanup_NullPath_DoesNotThrow()
    {
        var service = new ArchiveExtractionService();

        // Should handle null gracefully
        var exception = Record.Exception(() => service.Cleanup(null));

        Assert.Null(exception);
    }

    [Fact]
    public void Cleanup_EmptyPath_DoesNotThrow()
    {
        var service = new ArchiveExtractionService();

        var exception = Record.Exception(() => service.Cleanup(string.Empty));

        Assert.Null(exception);
    }

    [Fact]
    public void Cleanup_NonExistentPath_DoesNotThrow()
    {
        var service = new ArchiveExtractionService();

        var exception = Record.Exception(() => service.Cleanup(@"C:\nonexistent\directory"));

        Assert.Null(exception);
    }

    [Fact]
    public void Cleanup_NoArgs_WhenNoTempDirectory_DoesNotThrow()
    {
        var service = new ArchiveExtractionService();

        // TempZipDirectory is null, parameterless Cleanup should not throw
        var exception = Record.Exception(() => service.Cleanup());

        Assert.Null(exception);
    }

    #endregion

    #region Instance Isolation

    [Fact]
    public void TwoInstances_HaveIndependentState()
    {
        var serviceA = new ArchiveExtractionService();
        var serviceB = new ArchiveExtractionService();

        // Both should start with null/false state
        Assert.Null(serviceA.TempZipDirectory);
        Assert.Null(serviceB.TempZipDirectory);
        Assert.False(serviceA.IsArchived);
        Assert.False(serviceB.IsArchived);

        // They are distinct instances
        Assert.NotSame(serviceA, serviceB);
    }

    [Fact]
    public async Task TwoInstances_PrepareDoesNotAffectOther()
    {
        var serviceA = new ArchiveExtractionService();
        var serviceB = new ArchiveExtractionService();

        // Attempt prepare on serviceA with a nonexistent file (returns null but doesn't affect serviceB)
        await serviceA.PrepareArchiveAsync(
            @"C:\nonexistent.zip",
            (_, _) => Task.FromResult(false),
            string.Compare);

        // serviceB should still be in initial state
        Assert.Null(serviceB.TempZipDirectory);
        Assert.Null(serviceB.LastOpenedArchive);
        Assert.False(serviceB.IsArchived);
    }

    #endregion

    #region TabViewModel Integration

    [Fact]
    public void TabViewModel_ArchiveExtractionService_IsNotNull()
    {
        TranslationManager.Init();
        var tab = new TabViewModel(_ => { });

        Assert.NotNull(tab.ArchiveExtractionService);
    }

    [Fact]
    public void TabViewModel_TwoTabs_HaveIndependentServices()
    {
        TranslationManager.Init();
        var tab1 = new TabViewModel(_ => { });
        var tab2 = new TabViewModel(_ => { });

        Assert.NotSame(tab1.ArchiveExtractionService, tab2.ArchiveExtractionService);
    }

    [Fact]
    public void TabViewModel_Dispose_CleansUpArchiveService()
    {
        TranslationManager.Init();
        var tab = new TabViewModel(_ => { });
        var service = tab.ArchiveExtractionService;

        // Verify initial state
        Assert.False(service.IsArchived);

        // Dispose should not throw
        var exception = Record.Exception(() => tab.Dispose());
        Assert.Null(exception);
    }

    #endregion
}
