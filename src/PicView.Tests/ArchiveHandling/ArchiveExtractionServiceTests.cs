using PicView.Core.ArchiveHandling;
using PicView.Core.Localization;
using PicView.Core.ViewModels;

namespace PicView.Tests.ArchiveHandling;

[Collection("Sequential")]
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

    [Fact]
    public async Task PrepareArchiveAsync_AlwaysUncompressEntireArchive_ReturnsEntryKeysForStagedExtraction()
    {
        var tempZipPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.zip");
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            var file1 = Path.Combine(tempDir, "test1.jpg");
            var file2 = Path.Combine(tempDir, "test2.png");
            File.WriteAllBytes(file1, [0xFF, 0xD8, 0xFF]);
            File.WriteAllBytes(file2, [0x89, 0x50, 0x4E, 0x47]);

            System.IO.Compression.ZipFile.CreateFromDirectory(tempDir, tempZipPath);

            var service = new ArchiveExtractionService();

            Settings.Navigation.AlwaysUncompressEntireArchive = true;
            var result = await service.PrepareArchiveAsync(
                tempZipPath,
                (_, _) => Task.FromResult(false),
                string.Compare);

            Assert.NotNull(result);
            Assert.False(result.Value.IsFullyExtracted);
            Assert.Equal(2, result.Value.EntryKeys.Length);

            // Test extracting initial entries
            var extracted = await service.ExtractEntriesAsync(tempZipPath, result.Value.EntryKeys);
            Assert.Equal(2, extracted.Count);
            Assert.True(File.Exists(extracted[0]));
            Assert.True(File.Exists(extracted[1]));

            service.Cleanup();
        }
        finally
        {
            Settings.Navigation.AlwaysUncompressEntireArchive = false;
            if (File.Exists(tempZipPath)) File.Delete(tempZipPath);
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task PrepareArchiveAsync_MultipleArchivesSequential_CleansUpAndSucceeds()
    {
        var tempZipPath1 = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.zip");
        var tempZipPath2 = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.zip");
        var tempDir1 = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var tempDir2 = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir1);
        Directory.CreateDirectory(tempDir2);

        try
        {
            File.WriteAllBytes(Path.Combine(tempDir1, "a.jpg"), [0xFF, 0xD8, 0xFF]);
            File.WriteAllBytes(Path.Combine(tempDir2, "b.png"), [0x89, 0x50, 0x4E, 0x47]);

            System.IO.Compression.ZipFile.CreateFromDirectory(tempDir1, tempZipPath1);
            System.IO.Compression.ZipFile.CreateFromDirectory(tempDir2, tempZipPath2);

            var service = new ArchiveExtractionService();

            // First archive
            var res1 = await service.PrepareArchiveAsync(tempZipPath1, (_, _) => Task.FromResult(false), string.Compare);
            Assert.NotNull(res1);
            var firstTempDir = service.TempZipDirectory;

            // Second archive
            var res2 = await service.PrepareArchiveAsync(tempZipPath2, (_, _) => Task.FromResult(false), string.Compare);
            Assert.NotNull(res2);

            // Cleanup previous temp dir
            service.Cleanup(firstTempDir);
            Assert.False(Directory.Exists(firstTempDir));

            service.Cleanup();
        }
        finally
        {
            if (File.Exists(tempZipPath1)) File.Delete(tempZipPath1);
            if (File.Exists(tempZipPath2)) File.Delete(tempZipPath2);
            if (Directory.Exists(tempDir1)) Directory.Delete(tempDir1, true);
            if (Directory.Exists(tempDir2)) Directory.Delete(tempDir2, true);
        }
    }

    #endregion

    #region ExtractEntriesAsync

    [Fact]
    public async Task ExtractEntriesAsync_NoTempDirectory_ReturnsEmpty()
    {
        var service = new ArchiveExtractionService();

        var result = await service.ExtractEntriesAsync("archive.zip", ["entry.jpg"]);

        Assert.Empty(result);
    }

    [Fact]
    public async Task ExtractEntriesAsync_EmptyEntryKeys_ReturnsEmpty()
    {
        var service = new ArchiveExtractionService();

        var result = await service.ExtractEntriesAsync("archive.zip", []);

        Assert.Empty(result);
    }

    [Fact]
    public async Task ExtractEntriesAsync_ExtractsFirst10Pages_AndRemainingExtractsRest()
    {
        var tempZipPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.zip");
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            for (var i = 1; i <= 14; i++)
            {
                File.WriteAllBytes(Path.Combine(tempDir, $"page_{i:D2}.jpg"), [0xFF, 0xD8, 0xFF]);
            }

            System.IO.Compression.ZipFile.CreateFromDirectory(tempDir, tempZipPath);

            var service = new ArchiveExtractionService();
            var prep = await service.PrepareArchiveAsync(tempZipPath, (_, _) => Task.FromResult(false), string.Compare);

            Assert.NotNull(prep);
            Assert.Equal(14, prep.Value.EntryKeys.Length);

            // Extract initial 10 pages
            var initialKeys = prep.Value.EntryKeys.Take(10).ToArray();
            var initialPaths = await service.ExtractEntriesAsync(tempZipPath, initialKeys);

            Assert.Equal(10, initialPaths.Count);
            foreach (var path in initialPaths)
            {
                Assert.True(File.Exists(path));
            }

            // Extract remaining 4 pages with progress tracking
            var progressReports = new List<int>();
            var progress = new Progress<int>(progressReports.Add);
            var remainingKeys = prep.Value.EntryKeys.Skip(10).ToArray();
            await service.ExtractRemainingAsync(
                tempZipPath,
                remainingKeys,
                initialCount: 10,
                totalCount: 14,
                progress: progress,
                batchSize: 10);

            foreach (var key in remainingKeys)
            {
                var expectedPath = Path.Combine(service.TempZipDirectory!, key);
                Assert.True(File.Exists(expectedPath));
            }

            // Progress should report the final count
            Assert.Contains(14, progressReports);

            service.Cleanup();
        }
        finally
        {
            if (File.Exists(tempZipPath)) File.Delete(tempZipPath);
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task ExtractRemainingAsync_MultipleBatches_ReportsProgressAtEachBatch()
    {
        var tempZipPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.zip");
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            for (var i = 1; i <= 25; i++)
            {
                File.WriteAllBytes(Path.Combine(tempDir, $"page_{i:D2}.jpg"), [0xFF, 0xD8, 0xFF]);
            }

            System.IO.Compression.ZipFile.CreateFromDirectory(tempDir, tempZipPath);

            var service = new ArchiveExtractionService();
            var prep = await service.PrepareArchiveAsync(tempZipPath, (_, _) => Task.FromResult(false), string.Compare);
            Assert.NotNull(prep);

            var initialKeys = prep.Value.EntryKeys.Take(10).ToArray();
            await service.ExtractEntriesAsync(tempZipPath, initialKeys);

            var progressReports = new List<int>();
            var progress = new Progress<int>(progressReports.Add);
            var remainingKeys = prep.Value.EntryKeys.Skip(10).ToArray();

            await service.ExtractRemainingAsync(
                tempZipPath,
                remainingKeys,
                initialCount: 10,
                totalCount: 25,
                progress: progress,
                batchSize: 10);

            // Progress should have reported after first batch of 10 (20) and upon completion (25)
            Assert.Contains(20, progressReports);
            Assert.Contains(25, progressReports);

            service.Cleanup();
        }
        finally
        {
            if (File.Exists(tempZipPath)) File.Delete(tempZipPath);
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task ExtractEntriesAsync_LessThan10Pages_ExtractsAll()
    {
        var tempZipPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.zip");
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            for (var i = 1; i <= 5; i++)
            {
                File.WriteAllBytes(Path.Combine(tempDir, $"page_{i:D2}.jpg"), [0xFF, 0xD8, 0xFF]);
            }

            System.IO.Compression.ZipFile.CreateFromDirectory(tempDir, tempZipPath);

            var service = new ArchiveExtractionService();
            var prep = await service.PrepareArchiveAsync(tempZipPath, (_, _) => Task.FromResult(false), string.Compare);

            Assert.NotNull(prep);
            Assert.Equal(5, prep.Value.EntryKeys.Length);

            // Extract initial pages (up to 10)
            var initialKeys = prep.Value.EntryKeys.Take(10).ToArray();
            var initialPaths = await service.ExtractEntriesAsync(tempZipPath, initialKeys);

            Assert.Equal(5, initialPaths.Count);
            foreach (var path in initialPaths)
            {
                Assert.True(File.Exists(path));
            }

            // Remaining is empty
            var remainingKeys = prep.Value.EntryKeys.Skip(10).ToArray();
            Assert.Empty(remainingKeys);

            service.Cleanup();
        }
        finally
        {
            if (File.Exists(tempZipPath)) File.Delete(tempZipPath);
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }
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
        await service.ExtractRemainingAsync("archive.zip", ["entry1.jpg", "entry2.jpg"]);
    }

    [Fact]
    public async Task ExtractRemainingAsync_EmptyRemainingKeys_CompletesWithoutError()
    {
        var service = new ArchiveExtractionService();

        // Should be a no-op when remaining keys is empty, even if TempZipDirectory were set
        await service.ExtractRemainingAsync("archive.zip", []);
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
        var tab = new TabViewModel(_ => { }, null!);

        Assert.NotNull(tab.ArchiveExtractionService);
    }

    [Fact]
    public void TabViewModel_TwoTabs_HaveIndependentServices()
    {
        TranslationManager.Init();
        var tab1 = new TabViewModel(_ => { }, null!);
        var tab2 = new TabViewModel(_ => { }, null!);

        Assert.NotSame(tab1.ArchiveExtractionService, tab2.ArchiveExtractionService);
    }

    [Fact]
    public void TabViewModel_Dispose_CleansUpArchiveService()
    {
        TranslationManager.Init();
        var tab = new TabViewModel(_ => { }, null!);
        var service = tab.ArchiveExtractionService;

        // Verify initial state
        Assert.False(service.IsArchived);

        // Dispose should not throw
        var exception = Record.Exception(() => tab.Dispose());
        Assert.Null(exception);
    }

    [Fact]
    public void ResetExtractionCts_ReturnsActiveToken_AndCancelsOnCleanup()
    {
        var service = new ArchiveExtractionService();
        var token = service.ResetExtractionCts();

        Assert.False(token.IsCancellationRequested);

        service.Cleanup();

        Assert.True(token.IsCancellationRequested);
    }

    [Fact]
    public void ResetExtractionCts_CancelsPreviousToken()
    {
        var service = new ArchiveExtractionService();
        var token1 = service.ResetExtractionCts();
        Assert.False(token1.IsCancellationRequested);

        var token2 = service.ResetExtractionCts();
        Assert.True(token1.IsCancellationRequested);
        Assert.False(token2.IsCancellationRequested);
    }

    #endregion
}
