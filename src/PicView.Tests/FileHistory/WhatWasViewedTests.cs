using PicView.Core.FileHistory;

namespace PicView.Tests.FileHistory;

/// <summary>
/// What FileHistory.json keeps besides the entries: the Last File, the start-up directory and the Last Session.
/// </summary>
[Collection("Sequential")]
public class WhatWasViewedTests : IDisposable
{
    private readonly FileHistoryFileBackup _fileHistoryBackup = new();
    private readonly string _tempRoot;
    private readonly string _configPath;

    public WhatWasViewedTests()
    {
        SetDefaults();
        Settings.Navigation.IsFileHistoryEnabled = true;
        Settings.StartUp.LastFile = null;
        Settings.StartUp.StartUpDirectory = null;
        _tempRoot = Path.Combine(Path.GetTempPath(), "PicView_WhatWasViewedTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempRoot);
        _configPath = Path.Combine(_tempRoot, "FileHistory.json");
    }

    public void Dispose()
    {
        FileHistoryManager.Unload();
        if (Directory.Exists(_tempRoot))
        {
            try
            {
                Directory.Delete(_tempRoot, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        _fileHistoryBackup.Dispose();
    }

    [Fact]
    public void Load_OldFileWithoutNewFields_LoadsEntriesAndHasNoSession()
    {
        File.WriteAllText(_configPath, "{\"IsSortingDescending\":true,\"Entries\":[{\"Path\":\"C:\\\\a.jpg\"}]}");

        FileHistoryManager.Initialize(_configPath);

        Assert.Equal(1, FileHistoryManager.Count);
        Assert.Null(FileHistoryManager.LastSession);
        Assert.Null(FileHistoryManager.LastFile);
        Assert.Null(FileHistoryManager.StartUpDirectory);
    }

    [Fact]
    public void Load_MigratesLastFileAndStartUpDirectoryFromRecord_AndNullsRecord()
    {
        Settings.StartUp.LastFile = "C:\\legacy\\last.jpg";
        Settings.StartUp.StartUpDirectory = "C:\\legacy";

        FileHistoryManager.Initialize(_configPath);

        Assert.Equal("C:\\legacy\\last.jpg", FileHistoryManager.LastFile);
        Assert.Equal("C:\\legacy", FileHistoryManager.StartUpDirectory);
        Assert.Null(Settings.StartUp.LastFile);
        Assert.Null(Settings.StartUp.StartUpDirectory);
    }

    [Fact]
    public void Load_SecondRunWithNullRecord_KeepsFileValues()
    {
        Settings.StartUp.LastFile = "C:\\legacy\\last.jpg";
        Settings.StartUp.StartUpDirectory = "C:\\legacy";
        FileHistoryManager.Initialize(_configPath);
        FileHistoryManager.SaveToFileAsync().GetAwaiter().GetResult();

        FileHistoryManager.Unload();
        FileHistoryManager.Initialize(_configPath);

        Assert.Equal("C:\\legacy\\last.jpg", FileHistoryManager.LastFile);
        Assert.Equal("C:\\legacy", FileHistoryManager.StartUpDirectory);
    }

    [Fact]
    public void Load_FileValueWinsOverRecord()
    {
        File.WriteAllText(_configPath, "{\"LastFile\":\"C:\\\\file.jpg\",\"StartUpDirectory\":\"C:\\\\dir\"}");
        Settings.StartUp.LastFile = "C:\\legacy\\last.jpg";
        Settings.StartUp.StartUpDirectory = "C:\\legacy";

        FileHistoryManager.Initialize(_configPath);

        Assert.Equal("C:\\file.jpg", FileHistoryManager.LastFile);
        Assert.Equal("C:\\dir", FileHistoryManager.StartUpDirectory);
        Assert.Null(Settings.StartUp.LastFile);
        Assert.Null(Settings.StartUp.StartUpDirectory);
    }

    [Fact]
    public void Load_AfterLastFileSetEarly_KeepsEarlyValue()
    {
        File.WriteAllText(_configPath, "{\"LastFile\":\"C:\\\\older.jpg\",\"StartUpDirectory\":\"C:\\\\olderdir\"}");

        // App Start shows the launch file before the history file is read
        FileHistoryManager.LastFile = "C:\\launch.jpg";
        FileHistoryManager.StartUpDirectory = "C:\\launchdir";
        FileHistoryManager.Initialize(_configPath);

        Assert.Equal("C:\\launch.jpg", FileHistoryManager.LastFile);
        Assert.Equal("C:\\launchdir", FileHistoryManager.StartUpDirectory);
    }

    [Fact]
    public void EnsureInitialized_CalledTwice_LoadsOnce()
    {
        FileHistoryManager.Initialize(_configPath);
        FileHistoryManager.Add("C:\\a.jpg");

        FileHistoryManager.EnsureInitialized();
        FileHistoryManager.EnsureInitialized();

        Assert.Equal(1, FileHistoryManager.Count);
    }

    [Fact]
    public async Task SaveThenLoad_RoundTripsLastFileStartUpDirectoryAndSession()
    {
        FileHistoryManager.Initialize(_configPath);
        FileHistoryManager.LastFile = "C:\\b.zip";
        FileHistoryManager.StartUpDirectory = "C:\\root";
        FileHistoryManager.RecordSession(new LastSession
            { Sources = ["C:\\a.jpg", "https://x/y.png", "C:\\b.zip"], ActiveIndex = 1 });

        await FileHistoryManager.SaveToFileAsync();
        FileHistoryManager.Unload();
        FileHistoryManager.Initialize(_configPath);

        Assert.Equal("C:\\b.zip", FileHistoryManager.LastFile);
        Assert.Equal("C:\\root", FileHistoryManager.StartUpDirectory);
        var session = FileHistoryManager.LastSession;
        Assert.NotNull(session);
        Assert.Equal(["C:\\a.jpg", "https://x/y.png", "C:\\b.zip"], session.Sources);
        Assert.Equal(1, session.ActiveIndex);
    }

    [Fact]
    public void HistoryOff_SettersAndRecordSessionDoNothing()
    {
        FileHistoryManager.Initialize(_configPath);
        Settings.Navigation.IsFileHistoryEnabled = false;

        FileHistoryManager.LastFile = "C:\\a.jpg";
        FileHistoryManager.StartUpDirectory = "C:\\root";
        FileHistoryManager.RecordSession(new LastSession { Sources = ["C:\\a.jpg"] });

        Assert.Null(FileHistoryManager.LastFile);
        Assert.Null(FileHistoryManager.StartUpDirectory);
        Assert.Null(FileHistoryManager.LastSession);
    }

    [Fact]
    public async Task HistoryTurnedOffBeforeSave_SavesNoLastFileNoSession()
    {
        FileHistoryManager.Initialize(_configPath);
        FileHistoryManager.LastFile = "C:\\a.jpg";
        FileHistoryManager.StartUpDirectory = "C:\\root";
        FileHistoryManager.RecordSession(new LastSession { Sources = ["C:\\a.jpg"] });
        Settings.Navigation.IsFileHistoryEnabled = false;

        await FileHistoryManager.SaveToFileAsync();

        Settings.Navigation.IsFileHistoryEnabled = true;
        FileHistoryManager.Unload();
        FileHistoryManager.Initialize(_configPath);
        Assert.Null(FileHistoryManager.LastFile);
        Assert.Null(FileHistoryManager.StartUpDirectory);
        Assert.Null(FileHistoryManager.LastSession);
    }

    [Fact]
    public void Clear_ClearsLastFileStartUpDirectoryAndSession()
    {
        FileHistoryManager.Initialize(_configPath);
        FileHistoryManager.LastFile = "C:\\a.jpg";
        FileHistoryManager.StartUpDirectory = "C:\\root";
        FileHistoryManager.RecordSession(new LastSession { Sources = ["C:\\a.jpg"] });

        FileHistoryManager.Clear();

        Assert.Null(FileHistoryManager.LastFile);
        Assert.Null(FileHistoryManager.StartUpDirectory);
        Assert.Null(FileHistoryManager.LastSession);
    }

    [Fact]
    public void RenameFolder_RenamesSessionSourcesAndLastFile()
    {
        var oldFolder = Path.Combine(_tempRoot, "old");
        var newFolder = Path.Combine(_tempRoot, "new");
        FileHistoryManager.Initialize(_configPath);
        FileHistoryManager.LastFile = Path.Combine(oldFolder, "a.jpg");
        FileHistoryManager.RecordSession(new LastSession
        {
            Sources = [Path.Combine(oldFolder, "a.jpg"), "https://x/y.png", Path.Combine(_tempRoot, "older", "b.jpg")],
            ActiveIndex = 0
        });

        FileHistoryManager.RenameFolder(oldFolder, newFolder);

        Assert.Equal(Path.Combine(newFolder, "a.jpg"), FileHistoryManager.LastFile);
        Assert.Equal([Path.Combine(newFolder, "a.jpg"), "https://x/y.png", Path.Combine(_tempRoot, "older", "b.jpg")],
            FileHistoryManager.LastSession!.Sources);
    }

    [Fact]
    public void RenameFolder_StartUpDirectoryIsTheFolder_FollowsIt()
    {
        var oldFolder = Path.Combine(_tempRoot, "old");
        var newFolder = Path.Combine(_tempRoot, "new");
        FileHistoryManager.Initialize(_configPath);
        FileHistoryManager.StartUpDirectory = oldFolder.ToUpperInvariant();

        FileHistoryManager.RenameFolder(oldFolder, newFolder);

        Assert.Equal(newFolder, FileHistoryManager.StartUpDirectory);
    }

    [Fact]
    public void RecordSession_StoresACopy()
    {
        FileHistoryManager.Initialize(_configPath);
        var session = new LastSession { Sources = ["C:\a.jpg"] };

        FileHistoryManager.RecordSession(session);
        session.Sources.Add("C:\b.jpg");

        Assert.Single(FileHistoryManager.LastSession!.Sources);
    }

    [Fact]
    public void Rename_RenamesSessionSourcesAndLastFile()
    {
        FileHistoryManager.Initialize(_configPath);
        FileHistoryManager.LastFile = "C:\\a.jpg";
        FileHistoryManager.RecordSession(new LastSession { Sources = ["C:\\a.jpg", "C:\\b.jpg"] });

        FileHistoryManager.Rename("C:\\a.jpg", "C:\\c.jpg");

        Assert.Equal("C:\\c.jpg", FileHistoryManager.LastFile);
        Assert.Equal(["C:\\c.jpg", "C:\\b.jpg"], FileHistoryManager.LastSession!.Sources);
    }

    [Fact]
    public void RecordSession_Empty_StoresNull()
    {
        FileHistoryManager.Initialize(_configPath);
        FileHistoryManager.RecordSession(new LastSession { Sources = ["C:\\a.jpg"] });

        FileHistoryManager.RecordSession(new LastSession());

        Assert.Null(FileHistoryManager.LastSession);
    }
}
