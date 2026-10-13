using Avalonia;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using PicView.Avalonia;
using PicView.Avalonia.Controllers;
using PicView.Avalonia.Functions;
using PicView.Avalonia.Input;
using PicView.Avalonia.Views.Main;
using PicView.Core.Favorites;
using PicView.Core.Localization;
using PicView.Core.Models;
using PicView.Core.ViewModels;
using MouseButton = PicView.Avalonia.Input.MouseButton;

namespace PicView.Tests.Keybindings;

[Collection("Sequential")]
public class KeybindingsFavoritesTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly string _configPath;

    static KeybindingsFavoritesTests()
    {
        if (Application.Current == null)
        {
            AppBuilder.Configure<App>()
                .UseHeadless(new AvaloniaHeadlessPlatformOptions())
                .SetupWithoutStarting();
        }
    }

    public KeybindingsFavoritesTests()
    {
        SetDefaults();
        TranslationManager.Init();
        _tempRoot = Path.Combine(Path.GetTempPath(), "PicView_KeybindingsFavoritesTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempRoot);
        _configPath = Path.Combine(_tempRoot, "Favorites.json");
        FavoritesManager.Initialize(_configPath);
    }

    public void Dispose()
    {
        MainKeyboardShortcuts.Reset();
        SynchronizationContext.SetSynchronizationContext(null);
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
    }

    private static void RunWithDispatcher(Task task)
    {
        while (!task.IsCompleted)
        {
            Dispatcher.UIThread.RunJobs();
            Thread.Sleep(5);
        }
        task.GetAwaiter().GetResult();
    }

    private static void RunWithDispatcher(ValueTask task)
    {
        while (!task.IsCompleted)
        {
            Dispatcher.UIThread.RunJobs();
            Thread.Sleep(5);
        }
        task.GetAwaiter().GetResult();
    }

    private static CoreViewModel CreateCoreWithMainWindow(out MainWindowViewModel mainWindowVm)
    {
        var core = new CoreViewModel(null, _ => ValueTask.FromResult(new ImageModel()));
        core.Keybindings = new KeybindingsViewModel();
        mainWindowVm = new MainWindowViewModel(
            core,
            core.Translation,
            null!,
            core.GlobalSettings!,
            core.GallerySettings);
        core.MainWindows.MainWindows.Add(mainWindowVm);
        core.MainWindows.ActiveWindow.Value = mainWindowVm;
        return core;
    }

    [Fact]
    public void Initialize_WhenFavoritesEntriesIsEmpty_SkipsFavoritesCategory()
    {
        var core = CreateCoreWithMainWindow(out var mainWindowVm);
        mainWindowVm.Favorites = new FavoritesViewModel(mainWindowVm);

        var view = new KeybindingsView { DataContext = core };
        using var controller = new KeybindingsViewController(view);
        controller.Initialize();

        Assert.Null(controller.FavoritesCategory);
        Assert.False(view.FavoritesHeader.IsVisible);
        Assert.False(view.FavoritesItems.IsVisible);
        Assert.Empty(view.FavoritesItems.Items);
    }

    [Fact]
    public void Initialize_WhenFavoritesEntriesHasItems_PopulatesHeaderAndFolderNameActions()
    {
        var folderA = Path.Combine(_tempRoot, "VacationPhotos");
        var folderB = Path.Combine(_tempRoot, "DesignAssets");
        Directory.CreateDirectory(folderA);
        Directory.CreateDirectory(folderB);

        var core = CreateCoreWithMainWindow(out var mainWindowVm);
        mainWindowVm.Favorites = new FavoritesViewModel(mainWindowVm);
        RunWithDispatcher(mainWindowVm.Favorites.AddFavorite(folderA, "F7"));
        RunWithDispatcher(mainWindowVm.Favorites.AddFavorite(folderB, string.Empty));

        var view = new KeybindingsView { DataContext = core };
        using var controller = new KeybindingsViewController(view);
        controller.Initialize();

        Assert.NotNull(controller.FavoritesCategory);
        Assert.True(view.FavoritesHeader.IsVisible);
        Assert.True(view.FavoritesItems.IsVisible);
        Assert.Equal(TranslationManager.Translation.Favorites, view.FavoritesHeader.Text);

        Assert.Equal(2, controller.FavoritesCategory!.Entries.Count);
        var (boxA, pathA) = controller.FavoritesCategory.Entries[0];
        var (boxB, pathB) = controller.FavoritesCategory.Entries[1];

        Assert.Equal("VacationPhotos", boxA.ActionName);
        Assert.Equal(folderA, pathA);
        Assert.Single(boxA.Keybinds);
        Assert.Equal(new Keybind(Key.F7), boxA.Keybinds[0]);

        Assert.Equal("DesignAssets", boxB.ActionName);
        Assert.Equal(folderB, pathB);
        Assert.Empty(boxB.Keybinds);
    }

    [Fact]
    public void SaveKeybindings_WhenDuplicateHotKeyAssignedAcrossFavorites_EvictsPreviousFavoriteHotKey()
    {
        var folderA = Path.Combine(_tempRoot, "Alpha");
        var folderB = Path.Combine(_tempRoot, "Beta");
        Directory.CreateDirectory(folderA);
        Directory.CreateDirectory(folderB);

        var core = CreateCoreWithMainWindow(out var mainWindowVm);
        mainWindowVm.Favorites = new FavoritesViewModel(mainWindowVm);
        RunWithDispatcher(mainWindowVm.Favorites.AddFavorite(folderA, string.Empty));
        RunWithDispatcher(mainWindowVm.Favorites.AddFavorite(folderB, "F8"));

        var view = new KeybindingsView { DataContext = core };
        using var controller = new KeybindingsViewController(view);
        controller.Initialize();

        Assert.NotNull(controller.FavoritesCategory);
        var (boxA, _) = controller.FavoritesCategory!.Entries[0];
        var (boxB, _) = controller.FavoritesCategory.Entries[1];

        // Assign F8 to the first favorite without manually clearing the second favorite
        boxA.Keybinds.Add(new Keybind(Key.F8));

        RunWithDispatcher(controller.SaveKeybindings());

        // First favorite (newly edited) keeps F8; second favorite has its duplicate F8 evicted
        Assert.Single(boxA.Keybinds);
        Assert.Equal(new Keybind(Key.F8), boxA.Keybinds[0]);
        Assert.Empty(boxB.Keybinds);

        Assert.Equal("F8", FavoritesManager.AllEntries[0].HotKey);
        Assert.Equal(string.Empty, FavoritesManager.AllEntries[1].HotKey);

        Assert.Equal("F8", mainWindowVm.Favorites.Entries[0].HotKey.CurrentValue);
        Assert.Equal(string.Empty, mainWindowVm.Favorites.Entries[1].HotKey.CurrentValue);
    }

    [Fact]
    public void ShortcutHandling_WhenFavoriteKeyboardOrMouseHotKeyMatches_DispatchesOpenPathAsync()
    {
        var folderA = Path.Combine(_tempRoot, "KeyboardFolder");
        var folderB = Path.Combine(_tempRoot, "MouseFolder");
        Directory.CreateDirectory(folderA);
        Directory.CreateDirectory(folderB);

        RunWithDispatcher(FavoritesManager.AddAsync(folderA, new Keybind(Key.D1, KeyModifiers.Control).GetFormattedForJSON()));
        RunWithDispatcher(FavoritesManager.AddAsync(folderB, new Keybind(MouseButton.XButton1, KeyModifiers.Alt).GetFormattedForJSON()));

        _ = CreateCoreWithMainWindow(out var mainWindowVm);
        var spyMapper = new SpyFunctionsMapper(mainWindowVm);
        mainWindowVm.Mapper = spyMapper;

        // Dispatch keyboard shortcut
        MainKeyboardShortcuts.CurrentKeys = new Keybind(Key.D1, KeyModifiers.Control);
        RunWithDispatcher(MainKeyboardShortcuts.ExecuteShortcutIfRegistered(mainWindowVm));

        Assert.Equal(folderA, spyMapper.LastOpenedPath);

        // Dispatch mouse shortcut
        RunWithDispatcher(MouseShortcuts.ExecuteMouseShortcutIfRegistered(
            new Keybind(MouseButton.XButton1, KeyModifiers.Alt),
            mainWindowVm));

        Assert.Equal(folderB, spyMapper.LastOpenedPath);
    }

    private sealed class SpyFunctionsMapper(MainWindowViewModel vm) : FunctionsMapper(vm, null!)
    {
        public string? LastOpenedPath { get; private set; }

        public override ValueTask OpenPathAsync(string path)
        {
            LastOpenedPath = path;
            return ValueTask.CompletedTask;
        }
    }
}
