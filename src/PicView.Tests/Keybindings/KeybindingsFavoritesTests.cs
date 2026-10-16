using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using PicView.Avalonia.Controllers;
using PicView.Avalonia.Functions;
using PicView.Avalonia.Input;
using PicView.Avalonia.Views.Main;
using PicView.Avalonia.Views.UC.PopUps;
using PicView.Core.Favorites;
using PicView.Core.Keybindings;
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

    public KeybindingsFavoritesTests()
    {
        SetDefaults();
        TranslationManager.Init();
        _tempRoot = Path.Combine(Path.GetTempPath(), "PicView_KeybindingsFavoritesTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempRoot);
        _configPath = Path.Combine(_tempRoot, "Favorites.json");
        FavoritesManager.Initialize(_configPath);
        KeybindingFunctions.UseConfigPath(Path.Combine(_tempRoot, "keybindings.json"));
    }

    public void Dispose()
    {
        MainKeyboardShortcuts.Reset();
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

    [AvaloniaFact]
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

    private string CreateFolder(string name)
    {
        var path = Path.Combine(_tempRoot, name);
        Directory.CreateDirectory(path);
        return path;
    }

    [AvaloniaFact]
    public void Initialize_WhenFavoritesHaveSlots_ShowsFolderNamesWithSlotKeybinds()
    {
        var folderA = CreateFolder("VacationPhotos");
        var folderB = CreateFolder("DesignAssets");
        RunWithDispatcher(FavoritesManager.AddAsync(folderA));
        RunWithDispatcher(FavoritesManager.AddAsync(folderB));
        Keymaps.Replace(Keymap.Empty.Assign(new Keybind(Key.F7), "OpenFavorite1"));

        using var controller = CreateController(out _);

        Assert.NotNull(controller.FavoritesCategory);
        Assert.Equal(TranslationManager.Translation.Favorites, controller.FavoritesCategory!.Header.Text);
        Assert.Equal(2, controller.FavoritesCategory.Entries.Count);
        var (boxA, ownerA) = controller.FavoritesCategory.Entries[0];
        var (boxB, ownerB) = controller.FavoritesCategory.Entries[1];

        Assert.Equal("VacationPhotos", boxA.ActionName);
        Assert.Equal("OpenFavorite1", ownerA);
        Assert.Equal([new Keybind(Key.F7)], boxA.Keybinds);

        Assert.Equal("DesignAssets", boxB.ActionName);
        Assert.Equal("OpenFavorite2", ownerB);
        Assert.Empty(boxB.Keybinds);
    }

    [AvaloniaFact]
    public void Initialize_ShowsOnlyFavoritesWithSlots()
    {
        for (var i = 0; i <= FavoritesManager.MaxSlots; i++)
        {
            RunWithDispatcher(FavoritesManager.AddAsync(CreateFolder($"Folder{i}")));
        }

        using var controller = CreateController(out _);

        Assert.Equal(FavoritesManager.MaxSlots, controller.FavoritesCategory!.Entries.Count);
    }

    [AvaloniaFact]
    public void SaveKeybindings_FavoriteTakesKeybindOfAnotherFavorite_BindsItToTheSlot()
    {
        RunWithDispatcher(FavoritesManager.AddAsync(CreateFolder("Alpha")));
        RunWithDispatcher(FavoritesManager.AddAsync(CreateFolder("Beta")));
        Keymaps.Replace(Keymap.Empty.Assign(new Keybind(Key.F8), "OpenFavorite2"));
        using var controller = CreateController(out _);
        var (boxA, _) = controller.FavoritesCategory!.Entries[0];
        var (boxB, _) = controller.FavoritesCategory.Entries[1];

        boxA.Keybinds.Add(new Keybind(Key.F8));
        RunWithDispatcher(controller.SaveKeybindings());

        Assert.Equal([new Keybind(Key.F8)], boxA.Keybinds);
        Assert.Empty(boxB.Keybinds);
        Assert.Equal(KeymapResolution.Action("OpenFavorite1"), Keymaps.Resolve(new Keybind(Key.F8)));
    }

    [AvaloniaFact]
    public void ShortcutHandling_WhenFavoriteKeyboardOrMouseKeybindMatches_OpensTheSlotsFolder()
    {
        var folderA = CreateFolder("KeyboardFolder");
        var folderB = CreateFolder("MouseFolder");
        RunWithDispatcher(FavoritesManager.AddAsync(folderA));
        RunWithDispatcher(FavoritesManager.AddAsync(folderB));
        Keymaps.Replace(Keymap.Empty
            .Assign(new Keybind(Key.D1, KeyModifiers.Control), "OpenFavorite1")
            .Assign(new Keybind(MouseButton.XButton1, KeyModifiers.Alt), "OpenFavorite2"));

        _ = CreateCoreWithMainWindow(out var mainWindowVm);
        var spyMapper = new SpyFunctionsMapper(mainWindowVm);
        mainWindowVm.Mapper = spyMapper;

        // Dispatch keyboard shortcut
        MainKeyboardShortcuts.CurrentKeys = new Keybind(Key.D1, KeyModifiers.Control);
        RunWithDispatcher(MainKeyboardShortcuts.ExecuteShortcutIfRegistered(mainWindowVm, new InputModeState(), NoContextualActions.Instance));

        Assert.Equal(folderA, spyMapper.LastOpenedPath);

        // Dispatch mouse shortcut
        RunWithDispatcher(MouseShortcuts.ExecuteMouseShortcutIfRegistered(
            new Keybind(MouseButton.XButton1, KeyModifiers.Alt),
            mainWindowVm,
            new InputModeState(),
            NoContextualActions.Instance));

        Assert.Equal(folderB, spyMapper.LastOpenedPath);
    }

    [AvaloniaTheory]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    [InlineData(false, false, true)]
    public void ShortcutHandling_InBlockingMode_RunsNoKeyboardOrMouseKeybind(
        bool isDialogOpen, bool isRenaming, bool isCropping)
    {
        var state = new InputModeState(isDialogOpen, isRenaming, isCropping);
        var folder = CreateFolder("Blocked");
        RunWithDispatcher(FavoritesManager.AddAsync(folder));
        Keymaps.Replace(Keymap.Empty
            .Assign(new Keybind(Key.F7), "OpenFavorite1")
            .Assign(new Keybind(MouseButton.XButton1), "OpenFavorite1"));
        _ = CreateCoreWithMainWindow(out var mainWindowVm);
        var spyMapper = new SpyFunctionsMapper(mainWindowVm);
        mainWindowVm.Mapper = spyMapper;

        MainKeyboardShortcuts.CurrentKeys = new Keybind(Key.F7);
        RunWithDispatcher(MainKeyboardShortcuts.ExecuteShortcutIfRegistered(
            mainWindowVm, state, NoContextualActions.Instance));
        RunWithDispatcher(MouseShortcuts.ExecuteMouseShortcutIfRegistered(
            new Keybind(MouseButton.XButton1), mainWindowVm, state, NoContextualActions.Instance));

        Assert.Null(spyMapper.LastOpenedPath);
    }

    [AvaloniaFact]
    public void ShortcutHandling_WhenSlotIsEmpty_DoesNothing()
    {
        Keymaps.Replace(Keymap.Empty.Assign(new Keybind(Key.F7), "OpenFavorite3"));
        _ = CreateCoreWithMainWindow(out var mainWindowVm);
        var spyMapper = new SpyFunctionsMapper(mainWindowVm);
        mainWindowVm.Mapper = spyMapper;

        MainKeyboardShortcuts.CurrentKeys = new Keybind(Key.F7);
        RunWithDispatcher(MainKeyboardShortcuts.ExecuteShortcutIfRegistered(mainWindowVm, new InputModeState(), NoContextualActions.Instance));

        Assert.Null(spyMapper.LastOpenedPath);
    }

    private const string ViewTestKeybindings = """{ "Right": "Next", "Left": "Prev", "F9": "Rotate90" }""";

    private KeybindingsViewController CreateController(out MainWindowViewModel mainWindowVm)
    {
        var core = CreateCoreWithMainWindow(out mainWindowVm);
        mainWindowVm.Favorites = new FavoritesViewModel(mainWindowVm);
        var view = new KeybindingsView { DataContext = core };
        var controller = new KeybindingsViewController(view);
        controller.Initialize();
        return controller;
    }

    [AvaloniaFact]
    public void EditingActionBox_WithKeybindOfAnotherAction_TakesItImmediately()
    {
        Keymaps.Replace(Keymap.FromJson(ViewTestKeybindings, "{}"));
        using var controller = CreateController(out _);

        controller.ActionBox("Prev")!.Keybinds.Add(new Keybind(Key.Right));

        Assert.Empty(controller.ActionBox("Next")!.Keybinds);
    }

    [AvaloniaFact]
    public void Undo_AfterTakingKeybind_RestoresBothActions()
    {
        Keymaps.Replace(Keymap.FromJson(ViewTestKeybindings, "{}"));
        using var controller = CreateController(out _);
        controller.ActionBox("Prev")!.Keybinds.Add(new Keybind(Key.Right));

        controller.Undo();

        Assert.Equal([new Keybind(Key.Right)], controller.ActionBox("Next")!.Keybinds);
        Assert.Equal([new Keybind(Key.Left)], controller.ActionBox("Prev")!.Keybinds);
    }

    [AvaloniaFact]
    public void SaveKeybindings_ActionTakesFavoritesKeybind_ClearsFavoriteAndResolvesToAction()
    {
        RunWithDispatcher(FavoritesManager.AddAsync(CreateFolder("Gamma")));
        Keymaps.Replace(Keymap.FromJson(ViewTestKeybindings, "{}").Assign(new Keybind(Key.F7), "OpenFavorite1"));
        using var controller = CreateController(out _);
        var (favoriteBox, _) = controller.FavoritesCategory!.Entries[0];

        controller.ActionBox("Prev")!.Keybinds.Add(new Keybind(Key.F7));
        RunWithDispatcher(controller.SaveKeybindings());

        Assert.Empty(favoriteBox.Keybinds);
        Assert.Equal(KeymapResolution.Action("Prev"), Keymaps.Resolve(new Keybind(Key.F7)));
    }

    private const string ViewTestContextualKeybindings =
        """{ "Right": "Next", "Enter": "ToggleFullscreen", "Contextual": { "Enter": "Confirm", "Space": "PlayPause" } }""";

    [AvaloniaFact]
    public void Initialize_ShowsContextualActionsInTheirOwnCategory()
    {
        Keymaps.Replace(Keymap.FromJson(ViewTestContextualKeybindings, "{}"));
        using var controller = CreateController(out _);

        Assert.NotNull(controller.ContextualCategory);
        Assert.Equal(KeybindCategoryDefinition.ContextualActionNames,
            controller.ContextualCategory!.Entries.Select(e => e.FunctionName));
        Assert.Equal([new Keybind(Key.Enter)], controller.ActionBox("Confirm")!.Keybinds);
        Assert.Equal([new Keybind(Key.Space)], controller.ActionBox("PlayPause")!.Keybinds);
        Assert.Null(controller.ActionBox("Cancel"));
    }

    [AvaloniaFact]
    public void SaveKeybindings_ContextualActionSharingOrdinaryKeybind_KeepsBoth()
    {
        Keymaps.Replace(Keymap.FromJson(ViewTestContextualKeybindings, "{}"));
        using var controller = CreateController(out _);

        controller.ActionBox("PlayPause")!.Keybinds.Clear();
        controller.ActionBox("PlayPause")!.Keybinds.Add(new Keybind(Key.Right));
        RunWithDispatcher(controller.SaveKeybindings());

        Assert.Equal([new Keybind(Key.Right)], controller.ActionBox("Next")!.Keybinds);
        Assert.Equal(KeymapResolution.Action("Next"), Keymaps.Resolve(new Keybind(Key.Right)));
        Assert.Equal("PlayPause", Keymaps.Current.ResolveContextual(new Keybind(Key.Right)));
    }

    [AvaloniaFact]
    public void EditingContextualBox_WithKeybindOfAnotherContextualAction_TakesIt()
    {
        Keymaps.Replace(Keymap.FromJson(ViewTestContextualKeybindings, "{}"));
        using var controller = CreateController(out _);

        controller.ActionBox("Confirm")!.Keybinds.Add(new Keybind(Key.Space));

        Assert.Empty(controller.ActionBox("PlayPause")!.Keybinds);
        Assert.Equal([new Keybind(Key.Enter), new Keybind(Key.Space)], controller.ActionBox("Confirm")!.Keybinds);
    }

    [AvaloniaFact]
    public void SaveKeybindings_KeepsBindingsOfActionsNotShownInCategories()
    {
        Keymaps.Replace(Keymap.FromJson(ViewTestKeybindings, "{}"));
        using var controller = CreateController(out _);

        controller.ActionBox("Prev")!.Keybinds.Add(new Keybind(Key.Up));
        RunWithDispatcher(controller.SaveKeybindings());

        Assert.Equal(KeymapResolution.Action("Rotate90"), Keymaps.Resolve(new Keybind(Key.F9)));
        Assert.Equal(KeymapResolution.Action("Prev"), Keymaps.Resolve(new Keybind(Key.Up)));
    }

    [AvaloniaFact]
    public void ResetThenCancel_LeavesLiveKeymapUnchanged()
    {
        RunWithDispatcher(FavoritesManager.AddAsync(CreateFolder("Delta")));
        var live = Keymap.FromJson(ViewTestKeybindings, "{}").Assign(new Keybind(Key.F7), "OpenFavorite1");
        Keymaps.Replace(live);
        var core = CreateCoreWithMainWindow(out var mainWindowVm);
        mainWindowVm.Favorites = new FavoritesViewModel(mainWindowVm);
        var view = new KeybindingsView { DataContext = core };
        using var controller = new KeybindingsViewController(view);
        var window = new Window { Content = view };
        window.Show();
        controller.Initialize();
        Dispatcher.UIThread.RunJobs();

        // Click through the automation peer so a bound Command runs too, as with a real click
        Click(view.DefaultButton);
        Assert.Empty(controller.ActionBox("Next")!.Keybinds);
        Assert.Equal([new Keybind(Key.F7)], controller.FavoritesCategory!.Entries[0].Box.Keybinds);
        Click(view.CancelButton);

        Assert.Equal([new Keybind(Key.Right)], controller.ActionBox("Next")!.Keybinds);
        Assert.Equal(live, Keymaps.Current);
    }

    private static void Click(Button button) =>
        ((IInvokeProvider)ControlAutomationPeer.CreatePeerForElement(button)).Invoke();

    private sealed class NoContextualActions : IContextualActionHandler
    {
        public static NoContextualActions Instance { get; } = new();

        public ValueTask<bool> TryRunAsync(InputMode mode, string contextualAction) => ValueTask.FromResult(false);
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

    private static FavoriteDialog CreateDialog(string? path)
    {
        FavoriteDialog dialog = null!;
        RunWithDispatcher(Dispatcher.UIThread.InvokeAsync(() =>
        {
            dialog = new FavoriteDialog(path);
        }).GetTask());
        return dialog;
    }

    [AvaloniaFact]
    public void FavoriteDialog_ForFavoriteWithSlot_ShowsPathAndSlotsKeybind()
    {
        var folder = CreateFolder("Preselected");
        RunWithDispatcher(FavoritesManager.AddAsync(CreateFolder("First")));
        RunWithDispatcher(FavoritesManager.AddAsync(folder));
        var keybind = new Keybind(Key.D1, KeyModifiers.Control);
        Keymaps.Replace(Keymap.Empty.Assign(keybind, "OpenFavorite2"));

        var dialog = CreateDialog(folder);

        Assert.Equal(folder, dialog.PathBox.Text);
        Assert.True(dialog.KeyBox.IsEnabled);
        Assert.Equal([keybind], dialog.KeyBox.Keybinds);
    }

    [AvaloniaFact]
    public void FavoriteDialog_ForNewFavorite_ShowsKeybindLeftOnNextFreeSlot()
    {
        Keymaps.Replace(Keymap.Empty.Assign(new Keybind(Key.F7), "OpenFavorite1"));

        var dialog = CreateDialog(null);

        Assert.True(dialog.KeyBox.IsEnabled);
        Assert.Equal([new Keybind(Key.F7)], dialog.KeyBox.Keybinds);
    }

    [AvaloniaFact]
    public void FavoriteDialog_WhenNoSlotIsFree_DisablesKeyBox()
    {
        for (var i = 0; i < FavoritesManager.MaxSlots; i++)
        {
            RunWithDispatcher(FavoritesManager.AddAsync(CreateFolder($"Folder{i}")));
        }

        var dialog = CreateDialog(null);

        Assert.False(dialog.KeyBox.IsEnabled);
    }

    [AvaloniaFact]
    public void SetFavoriteKeybindAsync_ReplacesSlotsKeybindAndTakesItFromOtherActions()
    {
        Keymaps.Replace(Keymap.FromJson(ViewTestKeybindings, "{}").Assign(new Keybind(Key.F7), "OpenFavorite1"));

        RunWithDispatcher(Keymaps.SetFavoriteKeybindAsync(1, new Keybind(Key.Right)));

        Assert.Equal(KeymapResolution.Action("OpenFavorite1"), Keymaps.Resolve(new Keybind(Key.Right)));
        Assert.Equal(KeymapResolution.None, Keymaps.Resolve(new Keybind(Key.F7)));
        Assert.Empty(Keymaps.Current.KeybindsFor("Next"));
    }

    [AvaloniaFact]
    public void SetFavoriteKeybindAsync_WithNoKeybind_UnbindsSlot()
    {
        Keymaps.Replace(Keymap.Empty.Assign(new Keybind(Key.F7), "OpenFavorite1"));

        RunWithDispatcher(Keymaps.SetFavoriteKeybindAsync(1, null));

        Assert.Empty(Keymaps.Current.KeybindsFor("OpenFavorite1"));
    }
}
