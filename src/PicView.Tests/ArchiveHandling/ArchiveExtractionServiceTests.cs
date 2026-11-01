using System.IO.Compression;
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
            var extracted = await service.ExtractEntriesAsync(tempZipPath, result.Value.EntryKeys, TestContext.Current.CancellationToken);
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

    [Fact]
    public async Task PrepareArchiveAsync_ArchiveThatFailsToOpen_LeavesTheOpenArchiveAsItWas()
    {
        var folder = Path.Combine(Path.GetTempPath(), "PicViewEntries_" + Guid.NewGuid().ToString("N"));
        var zip = await CreateZipAsync(folder, "open.zip", "1.jpg");
        // Local software opens a .7z, and fails as when it is not installed
        var broken = Path.Combine(folder, "broken.7z");
        await File.WriteAllBytesAsync(broken, [0x37, 0x7A], TestContext.Current.CancellationToken);
        var service = new ArchiveExtractionService();

        try
        {
            await service.PrepareArchiveAsync(zip, (_, _) => Task.FromResult(false), string.CompareOrdinal);
            var openFolder = service.TempZipDirectory;
            var page = await service.ExtractEntryAsync(zip, "1.jpg", TestContext.Current.CancellationToken);
            var extracting = service.ResetExtractionCts();
            string? brokenFolder = null;

            var preparation = await service.PrepareArchiveAsync(broken, (_, created) =>
            {
                brokenFolder = created;
                return Task.FromResult(false);
            }, string.CompareOrdinal);

            Assert.Null(preparation);
            Assert.Equal(openFolder, service.TempZipDirectory);
            Assert.Equal(zip, service.LastOpenedArchive);
            Assert.Equal("1.jpg", service.EntryOf(page!));
            Assert.False(extracting.IsCancellationRequested, "The open archive stopped extracting");
            Assert.False(Directory.Exists(brokenFolder), "The folder of the archive that failed to open was left behind");
        }
        finally
        {
            service.Cleanup();
            Directory.Delete(folder, true);
        }
    }

    [Fact]
    public async Task EntryOf_FileOfAnArchiveOpenedBeforeTheOpenOne_IsItsEntryUntilItsFolderIsCleanedUp()
    {
        var folder = Path.Combine(Path.GetTempPath(), "PicViewEntries_" + Guid.NewGuid().ToString("N"));
        var first = await CreateZipAsync(folder, "first.zip", "1.jpg");
        var second = await CreateZipAsync(folder, "second.zip", "2.jpg");
        var service = new ArchiveExtractionService();

        try
        {
            await service.PrepareArchiveAsync(first, (_, _) => Task.FromResult(false), string.CompareOrdinal);
            var firstFolder = service.TempZipDirectory;
            var page = await service.ExtractEntryAsync(first, "1.jpg", TestContext.Current.CancellationToken);
            // The second opens before the first one's page shows
            await service.PrepareArchiveAsync(second, (_, _) => Task.FromResult(false), string.CompareOrdinal);
            var entryWhileBothAreOpen = service.EntryOf(page!);

            service.Cleanup(firstFolder);

            Assert.Equal("1.jpg", entryWhileBothAreOpen);
            Assert.Null(service.EntryOf(page!));
            Assert.Equal(second, service.LastOpenedArchive);
        }
        finally
        {
            service.Cleanup();
            Directory.Delete(folder, true);
        }
    }

    [Fact]
    public async Task EntryOf_FileOfAFullyExtractedArchive_IsItsPathInTheFolder()
    {
        var archive = Path.Combine(Path.GetTempPath(), "PicViewEntries_" + Guid.NewGuid().ToString("N") + ".7z");
        await File.WriteAllBytesAsync(archive, [0x37, 0x7A], TestContext.Current.CancellationToken);
        var service = new ArchiveExtractionService();

        try
        {
            // Local software extracts a .7z whole, keeping its folders
            var files = (await service.PrepareArchiveAsync(archive, (_, folder) =>
            {
                var sub = Directory.CreateDirectory(Path.Combine(folder, "sub")).FullName;
                File.WriteAllBytes(Path.Combine(sub, "1.jpg"), [0xFF, 0xD8, 0xFF]);
                return Task.FromResult(true);
            }, string.CompareOrdinal))!.Value.EntryKeys;

            var entry = service.EntryOf(files[0]);

            Assert.Equal(Path.Combine("sub", "1.jpg"), entry);
            Assert.Equal(files[0], service.ExtractedPathOf(entry!));
            Assert.Null(service.EntryOf(archive));
        }
        finally
        {
            service.Cleanup();
            File.Delete(archive);
        }
    }

    #endregion

    #region ExtractEntriesAsync

    [Fact]
    public async Task ExtractEntriesAsync_NoTempDirectory_ReturnsEmpty()
    {
        var service = new ArchiveExtractionService();

        var result = await service.ExtractEntriesAsync("archive.zip", ["entry.jpg"], TestContext.Current.CancellationToken);

        Assert.Empty(result);
    }

    [Fact]
    public async Task ExtractEntriesAsync_EmptyEntryKeys_ReturnsEmpty()
    {
        var service = new ArchiveExtractionService();

        var result = await service.ExtractEntriesAsync("archive.zip", [], TestContext.Current.CancellationToken);

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
            var initialPaths = await service.ExtractEntriesAsync(tempZipPath, initialKeys, TestContext.Current.CancellationToken);

            Assert.Equal(10, initialPaths.Count);
            foreach (var path in initialPaths)
            {
                Assert.True(File.Exists(path));
            }

            // Extract remaining 4 pages with progress tracking
            var progressReports = new List<int>();
            var progress = new Progress<int>(progressReports.Add);
            var remainingKeys = prep.Value.EntryKeys.Skip(10).ToArray();
            await service.ExtractRemainingAsync(tempZipPath, remainingKeys, initialCount: 10, totalCount: 14, progress: progress, batchSize: 10, ct: TestContext.Current.CancellationToken);

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
            await service.ExtractEntriesAsync(tempZipPath, initialKeys, TestContext.Current.CancellationToken);

            var progressReports = new List<int>();
            var progress = new Progress<int>(progressReports.Add);
            var remainingKeys = prep.Value.EntryKeys.Skip(10).ToArray();

            await service.ExtractRemainingAsync(tempZipPath, remainingKeys, initialCount: 10, totalCount: 25, progress: progress, batchSize: 10, ct: TestContext.Current.CancellationToken);

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
            var initialPaths = await service.ExtractEntriesAsync(tempZipPath, initialKeys, TestContext.Current.CancellationToken);

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

    [Fact]
    public async Task ExtractEntriesAsync_SameFileNameInTwoFolders_RemembersEachFilesEntry()
    {
        var folder = Path.Combine(Path.GetTempPath(), "PicViewEntries_" + Guid.NewGuid().ToString("N"));
        var source = Path.Combine(folder, "source");
        Directory.CreateDirectory(Path.Combine(source, "a"));
        Directory.CreateDirectory(Path.Combine(source, "b"));
        await File.WriteAllBytesAsync(Path.Combine(source, "a", "1.jpg"), [0xFF, 0xD8, 0xFF], TestContext.Current.CancellationToken);
        await File.WriteAllBytesAsync(Path.Combine(source, "b", "1.jpg"), [0xFF, 0xD8, 0xFF], TestContext.Current.CancellationToken);
        var zip = Path.Combine(folder, "nested.zip");
        ZipFile.CreateFromDirectory(source, zip);
        var service = new ArchiveExtractionService();

        try
        {
            var keys = (await service.PrepareArchiveAsync(zip, (_, _) => Task.FromResult(false), string.CompareOrdinal))!
                .Value.EntryKeys;
            var files = await service.ExtractEntriesAsync(zip, keys, TestContext.Current.CancellationToken);

            Assert.Equal(2, files.Count);
            Assert.NotEqual(files[0], files[1]);
            Assert.Equal(keys[0], service.EntryOf(files[0]));
            Assert.Equal(keys[1], service.EntryOf(files[1]));
            Assert.Equal(files[1], service.ExtractedPathOf(keys[1]));
        }
        finally
        {
            service.Cleanup();
            Directory.Delete(folder, true);
        }
    }

    #endregion

    #region ExtractEntryAsync

    [Fact]
    public async Task ExtractEntryAsync_NoTempDirectory_ReturnsNull()
    {
        var service = new ArchiveExtractionService();

        // TempZipDirectory is null because PrepareArchiveAsync was never called
        var result = await service.ExtractEntryAsync("archive.zip", "entry.jpg", TestContext.Current.CancellationToken);

        Assert.Null(result);
    }

    [Fact]
    public async Task ExtractEntryAsync_NullEntryKey_ReturnsNull()
    {
        var service = new ArchiveExtractionService();

        var result = await service.ExtractEntryAsync("archive.zip", null!, TestContext.Current.CancellationToken);

        Assert.Null(result);
    }

    [Fact]
    public async Task ExtractEntryAsync_EmptyEntryKey_ReturnsNull()
    {
        var service = new ArchiveExtractionService();

        var result = await service.ExtractEntryAsync("archive.zip", string.Empty, TestContext.Current.CancellationToken);

        Assert.Null(result);
    }

    #endregion

    #region ExtractRemainingAsync

    [Fact]
    public async Task ExtractRemainingAsync_NoTempDirectory_CompletesWithoutError()
    {
        var service = new ArchiveExtractionService();

        // Should be a no-op when TempZipDirectory is null
        await service.ExtractRemainingAsync("archive.zip", ["entry1.jpg", "entry2.jpg"], ct: TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task ExtractRemainingAsync_EmptyRemainingKeys_CompletesWithoutError()
    {
        var service = new ArchiveExtractionService();

        // Should be a no-op when remaining keys is empty, even if TempZipDirectory were set
        await service.ExtractRemainingAsync("archive.zip", [], ct: TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task ExtractRemaining_ArchiveThatIsNotSolid_ExtractsTheKeysInTheOrderGiven()
    {
        var folder = Path.Combine(Path.GetTempPath(), "PicViewEntries_" + Guid.NewGuid().ToString("N"));
        var source = Directory.CreateDirectory(Path.Combine(folder, "source")).FullName;
        for (var i = 1; i <= 5; i++)
        {
            await File.WriteAllBytesAsync(Path.Combine(source, $"img_{i:D2}.jpg"), [0xFF, 0xD8, 0xFF],
                TestContext.Current.CancellationToken);
        }
        var zip = Path.Combine(folder, "pages.zip");
        ZipFile.CreateFromDirectory(source, zip);
        var service = new ArchiveExtractionService();
        await service.PrepareArchiveAsync(zip, (_, _) => Task.FromResult(false), string.CompareOrdinal);
        var order = new ExtractionOrder(service);

        try
        {
            // One file to a batch, so each report follows one file
            service.ExtractRemaining(zip, ["img_04.jpg", "img_05.jpg", "img_02.jpg", "img_01.jpg"], progress: order,
                batchSize: 1, ct: TestContext.Current.CancellationToken);

            Assert.Equal(["img_04.jpg", "img_05.jpg", "img_02.jpg", "img_01.jpg"], order.Files);
        }
        finally
        {
            service.Cleanup();
            Directory.Delete(folder, true);
        }
    }

    [Theory]
    // Nearest first both ways from a page an archive opened again at
    [InlineData(new[] { "img_04.jpg", "img_05.jpg" }, new[] { "img_02.jpg", "img_01.jpg" },
        new[] { "img_04.jpg", "img_02.jpg", "img_05.jpg", "img_01.jpg" })]
    // The longer set finishes in its order
    [InlineData(new[] { "img_03.jpg", "img_04.jpg", "img_05.jpg" }, new[] { "img_01.jpg" },
        new[] { "img_03.jpg", "img_01.jpg", "img_04.jpg", "img_05.jpg" })]
    [InlineData(new[] { "img_05.jpg" }, new[] { "img_03.jpg", "img_02.jpg", "img_01.jpg" },
        new[] { "img_05.jpg", "img_03.jpg", "img_02.jpg", "img_01.jpg" })]
    public async Task ExtractRemaining_ArchiveThatIsNotSolid_TakesTurnsBetweenTheTwoSetsOfKeys(string[] remainingKeys,
        string[] laterKeys, string[] expected)
    {
        var folder = Path.Combine(Path.GetTempPath(), "PicViewEntries_" + Guid.NewGuid().ToString("N"));
        var zip = await CreateZipAsync(folder, "pages.zip",
            "img_01.jpg", "img_02.jpg", "img_03.jpg", "img_04.jpg", "img_05.jpg");
        var service = new ArchiveExtractionService();
        await service.PrepareArchiveAsync(zip, (_, _) => Task.FromResult(false), string.CompareOrdinal);
        var order = new ExtractionOrder(service);

        try
        {
            // One file to a batch, so each report follows one file
            service.ExtractRemaining(zip, remainingKeys, laterKeys, progress: order, batchSize: 1,
                ct: TestContext.Current.CancellationToken);

            Assert.Equal(expected, order.Files);
        }
        finally
        {
            service.Cleanup();
            Directory.Delete(folder, true);
        }
    }

    [Fact]
    public async Task ExtractRemaining_SolidArchive_WalksItInItsOwnOrderForEachSetOfKeys()
    {
        // A comic book name, so SharpCompress opens it rather than local software as for a .7z
        var archive = Path.Combine(Path.GetTempPath(), "PicViewEntries_" + Guid.NewGuid().ToString("N") + ".cbz");
        await File.WriteAllBytesAsync(archive, Convert.FromBase64String(SolidSevenZip), TestContext.Current.CancellationToken);
        var service = new ArchiveExtractionService();
        await service.PrepareArchiveAsync(archive, (_, _) => Task.FromResult(false), string.CompareOrdinal);
        var order = new ExtractionOrder(service);

        try
        {
            service.ExtractRemaining(archive, ["img_04.jpg", "img_05.jpg"], ["img_02.jpg", "img_01.jpg"], progress: order,
                batchSize: 1, ct: TestContext.Current.CancellationToken);

            Assert.Equal(["img_04.jpg", "img_05.jpg", "img_01.jpg", "img_02.jpg"], order.Files);
        }
        finally
        {
            service.Cleanup();
            File.Delete(archive);
        }
    }

    [Fact]
    public async Task ExtractRemaining_PageFileBeingWritten_ItsEntryIsKnownButItIsNotFoundForTheEntryYet()
    {
        // The watcher lists a page once its file is there, and a tab that waits for it shows and records it at once. A
        // step Back to a page looks for it in that list, so a file still being written must not be found: the archive
        // opens again at the page instead
        var folder = Path.Combine(Path.GetTempPath(), "PicViewEntries_" + Guid.NewGuid().ToString("N"));
        var source = Directory.CreateDirectory(Path.Combine(folder, "source")).FullName;
        await File.WriteAllBytesAsync(Path.Combine(source, "page.jpg"), [0xFF, 0xD8, 0xFF],
            TestContext.Current.CancellationToken);
        var zip = Path.Combine(folder, "pages.zip");
        ZipFile.CreateFromDirectory(source, zip);
        var service = new HeldWriteExtraction("page.jpg");
        var preparation = await service.PrepareArchiveAsync(zip, (_, _) => Task.FromResult(false), string.CompareOrdinal);
        var page = Path.Combine(preparation!.Value.TempDirectory, "page.jpg");

        try
        {
            var extraction = Task.Run(() => service.ExtractRemaining(zip, ["page.jpg"],
                ct: TestContext.Current.CancellationToken), TestContext.Current.CancellationToken);
            await service.Holding.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
            var isThereWhileWritten = File.Exists(page);
            var entryWhileWritten = service.EntryOf(page);
            var fileWhileWritten = service.ExtractedPathOf("page.jpg");
            service.Release();
            await extraction.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

            Assert.True(isThereWhileWritten);
            Assert.Equal("page.jpg", entryWhileWritten);
            Assert.Null(fileWhileWritten);
            Assert.Equal(page, service.ExtractedPathOf("page.jpg"));
        }
        finally
        {
            service.Release();
            service.Cleanup();
            Directory.Delete(folder, true);
        }
    }

    // A solid 7z, one block, of img_01.jpg to img_05.jpg, made with 7-Zip: 7z a -t7z -ms=on -mx=9
    private const string SolidSevenZip =
        "N3q8ryccAARTbgTkngAAAAAAAAAiAAAAAAAAAFMK2JrgACcAFV0Af7Yb5ssI8BnpyTc9vFejmhDnqmt2AAAAgTMHrg/P2W+8D+vqnL82Pf6J" +
        "90X3LepWaMPhXOQo+F9u31WyWYsBYd3T3j3fKmoECsObR6BFn38aufG05Zuta+IsyzmssVSkob+AX2DH9615JCmwnvYfH8s6oZKZL4Hiwd" +
        "ebUxC7S5Bsq5ohsUUnZTbYXyMUKYS0BJN4iRDUABcGHQEJgIEABwsBAAEjAwEBBV0AEAAADID6CgEwtof7AAA=";

    #endregion

    #region Archive folder structure

    private static readonly string[] TwoVolumeEntries =
    [
        "Volume 01/001.jpg", "Volume 01/002.jpg", "Volume 01/003.jpg",
        "Volume 02/001.jpg", "Volume 02/002.jpg", "Volume 02/003.jpg"
    ];

    private static readonly string[] TwoVolumeFileNames =
    [
        "Volume 01 - 001.jpg", "Volume 01 - 002.jpg", "Volume 01 - 003.jpg",
        "Volume 02 - 001.jpg", "Volume 02 - 002.jpg", "Volume 02 - 003.jpg"
    ];

    /// <summary>
    ///     Creates a zip whose files sit at the given relative paths, the way a comic archive with
    ///     one folder per volume is built.
    /// </summary>
    private static string CreateZipWithEntries(params string[] relativePaths)
    {
        var tempZipPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.zip");
        var sourceDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        try
        {
            foreach (var relativePath in relativePaths)
            {
                var path = Path.Combine(sourceDir, relativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllBytes(path, [0xFF, 0xD8, 0xFF]);
            }

            System.IO.Compression.ZipFile.CreateFromDirectory(sourceDir, tempZipPath);
            return tempZipPath;
        }
        finally
        {
            if (Directory.Exists(sourceDir)) Directory.Delete(sourceDir, true);
        }
    }

    /// <summary>
    ///     A natural string comparer like the one the platforms pass in, so <c>2</c> sorts before <c>10</c>.
    /// </summary>
    private static int NaturalCompare(string? x, string? y)
    {
        x ??= string.Empty;
        y ??= string.Empty;
        int i = 0, j = 0;
        while (i < x.Length && j < y.Length)
        {
            if (char.IsDigit(x[i]) && char.IsDigit(y[j]))
            {
                var startX = i;
                var startY = j;
                while (i < x.Length && char.IsDigit(x[i])) i++;
                while (j < y.Length && char.IsDigit(y[j])) j++;
                var result = long.Parse(x[startX..i]).CompareTo(long.Parse(y[startY..j]));
                if (result != 0)
                {
                    return result;
                }
            }
            else
            {
                var result = char.ToUpperInvariant(x[i]).CompareTo(char.ToUpperInvariant(y[j]));
                if (result != 0)
                {
                    return result;
                }
                i++;
                j++;
            }
        }
        return (x.Length - i).CompareTo(y.Length - j);
    }

    private static string[] Normalize(IEnumerable<string> keys) => keys.Select(k => k.Replace('\\', '/')).ToArray();

    private static async Task<(string[] Keys, string[] FileNames)> PrepareAndExtractAllAsync(
        ArchiveExtractionService service, string zipPath, Func<string, string, int> comparer)
    {
        var prep = await service.PrepareArchiveAsync(zipPath, (_, _) => Task.FromResult(false), comparer);
        Assert.NotNull(prep);
        var extracted = await service.ExtractEntriesAsync(zipPath, prep.Value.EntryKeys, TestContext.Current.CancellationToken);
        return (Normalize(prep.Value.EntryKeys), extracted.Select(Path.GetFileName).ToArray()!);
    }

    [Fact]
    public async Task PrepareArchiveAsync_RespectingStructure_SortsKeysByFolderThenFile()
    {
        var zipPath = CreateZipWithEntries(TwoVolumeEntries);
        var service = new ArchiveExtractionService();
        try
        {
            var (keys, _) = await PrepareAndExtractAllAsync(service, zipPath, NaturalCompare);

            Assert.Equal(TwoVolumeEntries, keys);
        }
        finally
        {
            service.Cleanup();
            File.Delete(zipPath);
        }
    }

    [Fact]
    public async Task ExtractEntriesAsync_RespectingStructure_NamesFilesAfterTheirFolders()
    {
        var zipPath = CreateZipWithEntries(TwoVolumeEntries);
        var service = new ArchiveExtractionService();
        try
        {
            var (_, fileNames) = await PrepareAndExtractAllAsync(service, zipPath, NaturalCompare);

            Assert.Equal(TwoVolumeFileNames, fileNames);
        }
        finally
        {
            service.Cleanup();
            File.Delete(zipPath);
        }
    }

    [Fact]
    public async Task ExtractedFileNames_RespectingStructure_SortByNameIntoArchiveOrder()
    {
        var zipPath = CreateZipWithEntries(TwoVolumeEntries);
        var service = new ArchiveExtractionService();
        try
        {
            var (_, fileNames) = await PrepareAndExtractAllAsync(service, zipPath, NaturalCompare);

            // The iterator and file watcher order the temp folder by file name, so the names alone must give the archive order.
            var sorted = fileNames.ToList();
            sorted.Sort(NaturalCompare);
            Assert.Equal(TwoVolumeFileNames, sorted);
        }
        finally
        {
            service.Cleanup();
            File.Delete(zipPath);
        }
    }

    [Fact]
    public async Task ExtractRemainingAsync_RespectingStructure_NamesFilesAfterTheirFolders()
    {
        var zipPath = CreateZipWithEntries(TwoVolumeEntries);
        var service = new ArchiveExtractionService();
        try
        {
            var prep = await service.PrepareArchiveAsync(zipPath, (_, _) => Task.FromResult(false), NaturalCompare);
            Assert.NotNull(prep);

            await service.ExtractEntriesAsync(zipPath, prep.Value.EntryKeys.Take(2).ToArray(), TestContext.Current.CancellationToken);
            await service.ExtractRemainingAsync(zipPath, prep.Value.EntryKeys.Skip(2).ToArray(), ct: TestContext.Current.CancellationToken);

            var fileNames = Directory.GetFiles(service.TempZipDirectory!).Select(Path.GetFileName).Order(StringComparer.Ordinal);
            Assert.Equal(TwoVolumeFileNames, fileNames);
        }
        finally
        {
            service.Cleanup();
            File.Delete(zipPath);
        }
    }

    [Fact]
    public async Task PrepareArchiveAsync_RespectingStructure_SortsFolderNumbersNaturally()
    {
        var zipPath = CreateZipWithEntries("Volume 10/001.jpg", "Volume 2/001.jpg");
        var service = new ArchiveExtractionService();
        try
        {
            var (keys, fileNames) = await PrepareAndExtractAllAsync(service, zipPath, NaturalCompare);

            Assert.Equal(["Volume 2/001.jpg", "Volume 10/001.jpg"], keys);
            Assert.Equal(["Volume 2 - 001.jpg", "Volume 10 - 001.jpg"], fileNames);
        }
        finally
        {
            service.Cleanup();
            File.Delete(zipPath);
        }
    }

    [Fact]
    public async Task ExtractEntriesAsync_RespectingStructure_DropsTheFolderAllEntriesShare()
    {
        var zipPath = CreateZipWithEntries(TwoVolumeEntries.Select(e => "Comic/" + e).ToArray());
        var service = new ArchiveExtractionService();
        try
        {
            var (_, fileNames) = await PrepareAndExtractAllAsync(service, zipPath, NaturalCompare);

            Assert.Equal(TwoVolumeFileNames, fileNames);
        }
        finally
        {
            service.Cleanup();
            File.Delete(zipPath);
        }
    }

    [Fact]
    public async Task ExtractEntriesAsync_RespectingStructure_SingleSharedFolderGivesPlainNames()
    {
        var zipPath = CreateZipWithEntries("Comic/001.jpg", "Comic/002.jpg");
        var service = new ArchiveExtractionService();
        try
        {
            var (_, fileNames) = await PrepareAndExtractAllAsync(service, zipPath, NaturalCompare);

            Assert.Equal(["001.jpg", "002.jpg"], fileNames);
        }
        finally
        {
            service.Cleanup();
            File.Delete(zipPath);
        }
    }

    [Fact]
    public async Task ExtractEntriesAsync_RespectingStructure_RootFilesKeepTheirName()
    {
        var zipPath = CreateZipWithEntries("cover.jpg", "Volume 01/001.jpg");
        var service = new ArchiveExtractionService();
        try
        {
            var (keys, fileNames) = await PrepareAndExtractAllAsync(service, zipPath, NaturalCompare);

            Assert.Equal(["cover.jpg", "Volume 01/001.jpg"], keys);
            Assert.Equal(["cover.jpg", "Volume 01 - 001.jpg"], fileNames);
        }
        finally
        {
            service.Cleanup();
            File.Delete(zipPath);
        }
    }

    [Fact]
    public async Task PrepareArchiveAsync_RespectingStructureDescending_ReversesTheWholeOrder()
    {
        Settings.Sorting.Ascending = false;
        var zipPath = CreateZipWithEntries(TwoVolumeEntries);
        var service = new ArchiveExtractionService();
        try
        {
            var (keys, fileNames) = await PrepareAndExtractAllAsync(service, zipPath, NaturalCompare);

            Assert.Equal(Enumerable.Reverse(TwoVolumeEntries), keys);
            Assert.Equal(Enumerable.Reverse(TwoVolumeFileNames), fileNames);
        }
        finally
        {
            Settings.Sorting.Ascending = true;
            service.Cleanup();
            File.Delete(zipPath);
        }
    }

    [Fact]
    public async Task PrepareArchiveAsync_NotRespectingStructure_KeepsFileNameOrderAndFlatNames()
    {
        Settings.Navigation.IsRespectingArchiveStructure = false;
        var zipPath = CreateZipWithEntries(TwoVolumeEntries);
        var service = new ArchiveExtractionService();
        try
        {
            var (keys, fileNames) = await PrepareAndExtractAllAsync(service, zipPath, NaturalCompare);

            Assert.Equal(["001.jpg", "001.jpg", "002.jpg", "002.jpg", "003.jpg", "003.jpg"], keys.Select(Path.GetFileName));
            Assert.Equal(["001.jpg", "001_1.jpg", "002.jpg", "002_1.jpg", "003.jpg", "003_1.jpg"], fileNames);
        }
        finally
        {
            Settings.Navigation.IsRespectingArchiveStructure = true;
            service.Cleanup();
            File.Delete(zipPath);
        }
    }

    [Fact]
    public async Task PrepareArchiveAsync_RespectingStructureNotSortedByName_KeepsFlatNames()
    {
        Settings.Sorting.SortPreference = 1;
        var zipPath = CreateZipWithEntries(TwoVolumeEntries);
        var service = new ArchiveExtractionService();
        try
        {
            var (_, fileNames) = await PrepareAndExtractAllAsync(service, zipPath, NaturalCompare);

            Assert.Equal(["001.jpg", "001_1.jpg", "002.jpg", "002_1.jpg", "003.jpg", "003_1.jpg"], fileNames.Order(StringComparer.Ordinal));
        }
        finally
        {
            Settings.Sorting.SortPreference = 0;
            service.Cleanup();
            File.Delete(zipPath);
        }
    }

    [Fact]
    public async Task PrepareArchiveAsync_RespectingStructureFullyExtracted_SortsByRelativePath()
    {
        var archivePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.7z");
        await File.WriteAllBytesAsync(archivePath, [], TestContext.Current.CancellationToken);
        var service = new ArchiveExtractionService();
        try
        {
            // Stands in for the local software, which extracts the archive into real nested folders.
            var prep = await service.PrepareArchiveAsync(archivePath, (_, destination) =>
            {
                foreach (var entry in Enumerable.Reverse(TwoVolumeEntries))
                {
                    var path = Path.Combine(destination, entry);
                    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                    File.WriteAllBytes(path, [0xFF, 0xD8, 0xFF]);
                }
                return Task.FromResult(true);
            }, NaturalCompare);

            Assert.NotNull(prep);
            Assert.True(prep.Value.IsFullyExtracted);
            var relative = prep.Value.EntryKeys.Select(k => Path.GetRelativePath(prep.Value.TempDirectory, k));
            Assert.Equal(TwoVolumeEntries, Normalize(relative));
        }
        finally
        {
            service.Cleanup();
            File.Delete(archivePath);
        }
    }

    [Fact]
    public async Task ExtractEntriesAsync_ArchiveThatAnotherOpenedAfter_NamesItsFilesAsItsOwnPreparationDid()
    {
        // Every entry of the first shares its Comic folder, which its names leave out. A second archive can open before
        // the first one's pages are extracted, as two quick Backs do, and must not change how they are named
        var first = CreateZipWithEntries(TwoVolumeEntries.Select(e => "Comic/" + e).ToArray());
        var second = CreateZipWithEntries("001.jpg");
        var service = new ArchiveExtractionService();
        try
        {
            var prep = await service.PrepareArchiveAsync(first, (_, _) => Task.FromResult(false), NaturalCompare);
            var firstFolder = service.TempZipDirectory;
            await service.PrepareArchiveAsync(second, (_, _) => Task.FromResult(false), NaturalCompare);

            var extracted = await service.ExtractEntriesAsync(first, prep!.Value.EntryKeys,
                TestContext.Current.CancellationToken, firstFolder);

            Assert.Equal(TwoVolumeFileNames, extracted.Select(Path.GetFileName));
        }
        finally
        {
            service.Cleanup();
            File.Delete(first);
            File.Delete(second);
        }
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

    [Fact]
    public async Task Cleanup_ForgetsWhichEntryEachFileCameFrom()
    {
        var folder = Path.Combine(Path.GetTempPath(), "PicViewEntries_" + Guid.NewGuid().ToString("N"));
        var source = Directory.CreateDirectory(Path.Combine(folder, "source")).FullName;
        await File.WriteAllBytesAsync(Path.Combine(source, "1.jpg"), [0xFF, 0xD8, 0xFF], TestContext.Current.CancellationToken);
        var zip = Path.Combine(folder, "flat.zip");
        ZipFile.CreateFromDirectory(source, zip);
        var service = new ArchiveExtractionService();
        await service.PrepareArchiveAsync(zip, (_, _) => Task.FromResult(false), string.CompareOrdinal);
        var file = await service.ExtractEntryAsync(zip, "1.jpg", TestContext.Current.CancellationToken);

        service.Cleanup();

        Assert.Null(service.EntryOf(file!));
        Assert.Null(service.ExtractedPathOf("1.jpg"));
        Directory.Delete(folder, true);
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
        var tab = new TabViewModel(null!);

        Assert.NotNull(tab.ArchiveExtractionService);
    }

    [Fact]
    public void TabViewModel_TwoTabs_HaveIndependentServices()
    {
        TranslationManager.Init();
        var tab1 = new TabViewModel(null!);
        var tab2 = new TabViewModel(null!);

        Assert.NotSame(tab1.ArchiveExtractionService, tab2.ArchiveExtractionService);
    }

    [Fact]
    public void TabViewModel_Dispose_CleansUpArchiveService()
    {
        TranslationManager.Init();
        var tab = new TabViewModel(null!);
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

    [Fact]
    public void Cleanup_PreviousTempDir_DoesNotCancelCurrentExtractionToken()
    {
        var service = new ArchiveExtractionService();
        var token = service.ResetExtractionCts();

        var previousDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(previousDir);

        try
        {
            service.Cleanup(previousDir);

            // Token for current extraction should NOT be cancelled
            Assert.False(token.IsCancellationRequested);
            Assert.False(Directory.Exists(previousDir));
        }
        finally
        {
            if (Directory.Exists(previousDir))
            {
                Directory.Delete(previousDir, true);
            }
            service.Cleanup();
        }
    }

    #endregion

    // A zip in the folder that holds the images given, each a few bytes
    private static async Task<string> CreateZipAsync(string folder, string name, params string[] images)
    {
        var source = Directory.CreateDirectory(Path.Combine(folder, Path.GetFileNameWithoutExtension(name))).FullName;
        foreach (var image in images)
        {
            await File.WriteAllBytesAsync(Path.Combine(source, image), [0xFF, 0xD8, 0xFF],
                TestContext.Current.CancellationToken);
        }

        var zip = Path.Combine(folder, name);
        ZipFile.CreateFromDirectory(source, zip);
        return zip;
    }

    // The order files are extracted in: at each progress report, the extracted files it hasn't seen yet
    private sealed class ExtractionOrder(ArchiveExtractionService service) : IProgress<int>
    {
        public List<string> Files { get; } = [];

        public void Report(int value)
        {
            foreach (var path in Directory.GetFiles(service.TempZipDirectory!).Order(StringComparer.Ordinal))
            {
                var name = Path.GetFileName(path);
                if (!Files.Contains(name))
                {
                    Files.Add(name);
                }
            }
        }
    }
}
