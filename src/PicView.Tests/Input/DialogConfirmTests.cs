using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Input;
using PicView.Avalonia.Views.UC.PopUps;
using PicView.Core.Favorites;
using PicView.Core.Keybindings;
using PicView.Core.Localization;
using PicView.Core.ViewModels;

namespace PicView.Tests.Input;

[Collection("Sequential")]
public class DialogConfirmTests : IDisposable
{
    private readonly string _tempRoot;

    public DialogConfirmTests()
    {
        SetDefaults();
        TranslationManager.Init();
        _tempRoot = Path.Combine(Path.GetTempPath(), "PicView_DialogConfirmTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempRoot);
        FavoritesManager.Initialize(Path.Combine(_tempRoot, "Favorites.json"));
        KeybindingFunctions.UseConfigPath(Path.Combine(_tempRoot, "keybindings.json"));
    }

    public void Dispose()
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

    private static T RunWithDispatcher<T>(ValueTask<T> task)
    {
        while (!task.IsCompleted)
        {
            Dispatcher.UIThread.RunJobs();
            Thread.Sleep(5);
        }

        return task.GetAwaiter().GetResult();
    }

    private static void WaitUntil(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (!condition() && DateTime.UtcNow < deadline)
        {
            Dispatcher.UIThread.RunJobs();
            Thread.Sleep(5);
        }
    }

    private sealed class PlainPopUp : AnimatedPopUp;

    private sealed class ConfirmingPopUp : AnimatedPopUp
    {
        public override ValueTask<bool> ConfirmAsync() => ValueTask.FromResult(true);
    }

    [AvaloniaFact]
    public void ConfirmAsync_ByDefault_IsNotClaimed()
    {
        Assert.False(RunWithDispatcher(new PlainPopUp().ConfirmAsync()));
    }

    [AvaloniaFact]
    public void TopPopUp_ReturnsTheVisiblePopUpAddedLast()
    {
        var hidden = new ConfirmingPopUp { IsVisible = false };
        var lower = new PlainPopUp();
        var top = new ConfirmingPopUp();
        var panel = new Panel { Children = { lower, top, hidden } };

        Assert.Same(top, ContextualActionHandler.TopPopUp(panel));
    }

    [AvaloniaFact]
    public void TopPopUp_WithoutVisiblePopUp_IsNull()
    {
        var panel = new Panel { Children = { new PlainPopUp { IsVisible = false }, new Border() } };

        Assert.Null(ContextualActionHandler.TopPopUp(panel));
        Assert.Null(ContextualActionHandler.TopPopUp(null));
    }

    [AvaloniaFact]
    public void FavoriteDialog_Confirm_SavesFavoriteWithItsKeybind()
    {
        var folder = Path.Combine(_tempRoot, "Folder");
        Directory.CreateDirectory(folder);
        Keymaps.Replace(Keymap.Empty);
        var dialog = new FavoriteDialog(folder);
        dialog.KeyBox.Keybinds.Add(new Keybind(Key.F7));

        Assert.True(RunWithDispatcher(dialog.ConfirmAsync()));

        Assert.True(FavoritesManager.Contains(folder));
        var slot = FavoritesManager.SlotOf(folder);
        Assert.NotNull(slot);
        Assert.Equal([new Keybind(Key.F7)],
            Keymaps.Current.KeybindsFor(KeybindCategoryDefinition.FavoriteAction(slot.Value)));
    }

    [AvaloniaFact]
    public void RenameDialog_Confirm_RenamesActiveFile()
    {
        var oldPath = Path.Combine(_tempRoot, "old.txt");
        File.WriteAllText(oldPath, "x");
        var newPath = Path.Combine(_tempRoot, "new.txt");
        var vm = new MainWindowViewModel(null!, null!, null!, null!, null!);
        var tab = new TabViewModel(null!, vm);
        tab.FileInfo.Value = new FileInfo(oldPath);
        vm.WindowTabs.ActiveTab.Value = tab;
        var dialog = new RenameDialog { DataContext = vm };
        dialog.RenameBox.Text = "new.txt";

        Assert.True(RunWithDispatcher(dialog.ConfirmAsync()));

        WaitUntil(() => File.Exists(newPath));
        Assert.True(File.Exists(newPath));
        Assert.False(File.Exists(oldPath));
    }

    [AvaloniaFact]
    public void CloseDialog_Confirm_ClosesItsWindow()
    {
        var dialog = new CloseDialog();
        var window = new Window { Content = dialog };
        var closed = false;
        window.Closed += (_, _) => closed = true;
        window.Show();
        Dispatcher.UIThread.RunJobs();

        Assert.True(RunWithDispatcher(dialog.ConfirmAsync()));

        Assert.True(closed);
    }

    [AvaloniaTheory]
    [InlineData(Key.E, KeyModifiers.None)]
    [InlineData(Key.E, KeyModifiers.Shift)]
    [InlineData(Key.Space, KeyModifiers.None)]
    [InlineData(Key.Left, KeyModifiers.None)]
    public void FocusedTextBox_OwnsKeysThatEditText(Key key, KeyModifiers modifiers)
    {
        Assert.True(FocusedInput.OwnsKeybind(new TextBox(), new Keybind(key, modifiers)));
    }

    [AvaloniaTheory]
    [InlineData(Key.Enter, KeyModifiers.None)]
    [InlineData(Key.Escape, KeyModifiers.None)]
    [InlineData(Key.S, KeyModifiers.Control)]
    [InlineData(Key.S, KeyModifiers.Meta)]
    public void FocusedTextBox_LeavesConfirmCancelAndChordsToTheWindow(Key key, KeyModifiers modifiers)
    {
        Assert.False(FocusedInput.OwnsKeybind(new TextBox(), new Keybind(key, modifiers)));
    }

    [AvaloniaFact]
    public void FocusedMultilineTextBox_OwnsEnter()
    {
        Assert.True(FocusedInput.OwnsKeybind(new TextBox { AcceptsReturn = true }, new Keybind(Key.Enter)));
    }

    [AvaloniaFact]
    public void NothingFocused_OwnsNoKeys()
    {
        Assert.False(FocusedInput.OwnsKeybind(null, new Keybind(Key.E)));
        Assert.False(FocusedInput.OwnsKeybind(new Button(), new Keybind(Key.E)));
    }

    [AvaloniaFact]
    public void RecordingKeybindBox_OwnsEnterAndEscape()
    {
        var box = new KeybindBox { MaxTags = 1 };
        var window = new Window { Content = box };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        box.TagBox!.Focus();
        Dispatcher.UIThread.RunJobs();

        Assert.True(FocusedInput.OwnsKeybind(box.TagBox, new Keybind(Key.Enter)));
        Assert.True(FocusedInput.OwnsKeybind(box.TagBox, new Keybind(Key.Escape)));
    }

    [AvaloniaFact]
    public void FullKeybindBox_LeavesEnterToTheWindow()
    {
        // A full box records nothing, so Enter confirms the dialog instead
        var box = new KeybindBox { MaxTags = 1 };
        box.Keybinds.Add(new Keybind(Key.F7));
        var window = new Window { Content = box };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        box.TagBox!.Focus();
        Dispatcher.UIThread.RunJobs();

        Assert.False(FocusedInput.OwnsKeybind(box.TagBox, new Keybind(Key.Enter)));
    }
}
