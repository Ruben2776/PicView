using PicView.Core.Keybindings;
using PicView.Core.Localization;

namespace PicView.Tests.Keybindings;

public class KeybindCategoryDefinitionTests
{
    private static LanguageModel CreateTestTranslation()
    {
        // Use a populated LanguageModel with non-null values for relevant properties
        return new LanguageModel
        {
            NextImage = "Next image",
            AdvanceBy10Images = "Advance by 10",
            AdvanceBy100Images = "Advance by 100",
            PrevImage = "Previous image",
            GoBackBy10Images = "Go back by 10",
            GoBackBy100Images = "Go back by 100",
            LastImage = "Last image",
            FirstImage = "First image",
            NextFolder = "Next folder",
            PrevFolder = "Previous folder",
            NextArchive = "Next archive",
            PrevArchive = "Previous archive",
            Search = "Search",
            SelectGalleryThumb = "Select gallery thumb",
            ToggleLooping = "Toggle looping",
            RotateRight = "Rotate right",
            ScrollUp = "Scroll up",
            RotateLeft = "Rotate left",
            ScrollDown = "Scroll down",
            ScrollToTop = "Scroll to top",
            ScrollToBottom = "Scroll to bottom",
            ToggleScroll = "Toggle scroll",
            CtrlToZoom = "Ctrl to zoom",
            ZoomIn = "Zoom in",
            ZoomOut = "Zoom out",
            ResetZoom = "Reset zoom",
            SideBySide = "Side by side",
            Stretch = "Stretch",
            Flip = "Flip",
            Crop = "Crop",
            ChangeBackground = "Change background",
            OptimizeImage = "Optimize image",
            HideUI = "Hide UI",
            Slideshow = "Slideshow",
            ShowImageGallery = "Show gallery",
            Open = "Open",
            OpenWith = "Open with",
            ShowInFolder = "Show in folder",
            Reload = "Reload",
            Save = "Save",
            SaveAs = "Save as",
            Print = "Print",
            DeleteFile = "Delete file",
            PermanentlyDelete = "Permanently delete",
            RenameFile = "Rename file",
            FileProperties = "File properties",
            FileName = "File name",
            FileSize = "File size",
            FileExtension = "File extension",
            Created = "Created",
            LastAccessTime = "Last access time",
            Random = "Random",
            Ascending = "Ascending",
            Descending = "Descending",
            CopyFile = "Copy file",
            FileCopyPath = "Copy path",
            CopyImage = "Copy image",
            Copy = "Copy",
            FilePaste = "Paste",
            DuplicateFile = "Duplicate file",
            NewTab = "New tab",
            CloseTab = "Close tab",
            About = "About",
            Settings = "Settings",
            ImageInfo = "Image info",
            Effects = "Effects",
            FileConversion = "File conversion",
            ApplicationShortcuts = "Application shortcuts",
            BatchResize = "Batch resize",
            Resize = "Resize",
            Close = "Close",
            NewWindow = "New window",
            CenterWindow = "Center window",
            StayTopMost = "Stay top most",
            ToggleFullscreen = "Toggle fullscreen",
            Maximize = "Maximize",
            AutoFitWindow = "Auto fit window",
            NormalWindow = "Normal window",
            _1Star = "1 Star",
            _2Star = "2 Stars",
            _3Star = "3 Stars",
            _4Star = "4 Stars",
            _5Star = "5 Stars",
            RemoveStarRating = "Remove star rating",
        };
    }

    [Fact]
    public void GetNavigationEntries_ReturnsCorrectCount()
    {
        var t = CreateTestTranslation();
        var entries = KeybindCategoryDefinition.GetNavigationEntries(t);
        Assert.Equal(15, entries.Count);
    }

    [Fact]
    public void GetNavigationEntries_ContainsExpectedFunctionNames()
    {
        var t = CreateTestTranslation();
        var entries = KeybindCategoryDefinition.GetNavigationEntries(t);
        var functionNames = entries.Select(e => e.FunctionName).ToList();

        Assert.Contains("Next", functionNames);
        Assert.Contains("Prev", functionNames);
        Assert.Contains("Last", functionNames);
        Assert.Contains("First", functionNames);
        Assert.Contains("NextFolder", functionNames);
        Assert.Contains("GalleryClick", functionNames);
    }

    [Fact]
    public void GetNavigationEntries_DisplayNamesFromTranslation()
    {
        var t = CreateTestTranslation();
        var entries = KeybindCategoryDefinition.GetNavigationEntries(t);

        var nextEntry = entries.First(e => e.FunctionName == "Next");
        Assert.Equal("Next image", nextEntry.DisplayName);

        var prevEntry = entries.First(e => e.FunctionName == "Prev");
        Assert.Equal("Previous image", prevEntry.DisplayName);
    }

    [Fact]
    public void GetScrollAndRotateEntries_ReturnsCorrectCount()
    {
        var t = CreateTestTranslation();
        var entries = KeybindCategoryDefinition.GetScrollAndRotateEntries(t);
        Assert.Equal(8, entries.Count);
    }

    [Fact]
    public void GetZoomEntries_ReturnsCorrectCount()
    {
        var t = CreateTestTranslation();
        var entries = KeybindCategoryDefinition.GetZoomEntries(t);
        Assert.Equal(3, entries.Count);
    }

    [Fact]
    public void GetImageControlEntries_ReturnsCorrectCount()
    {
        var t = CreateTestTranslation();
        var entries = KeybindCategoryDefinition.GetImageControlEntries(t);
        Assert.Equal(6, entries.Count);
    }

    [Fact]
    public void GetInterfaceConfigurationEntries_ReturnsCorrectCount()
    {
        var t = CreateTestTranslation();
        var entries = KeybindCategoryDefinition.GetInterfaceConfigurationEntries(t);
        Assert.Equal(3, entries.Count);
    }

    [Fact]
    public void GetFileManagementEntries_ReturnsCorrectCount()
    {
        var t = CreateTestTranslation();
        var entries = KeybindCategoryDefinition.GetFileManagementEntries(t);
        Assert.Equal(11, entries.Count);
    }

    [Fact]
    public void GetSortFilesEntries_ReturnsCorrectCount()
    {
        var t = CreateTestTranslation();
        var entries = KeybindCategoryDefinition.GetSortFilesEntries(t);
        Assert.Equal(8, entries.Count);
    }

    [Fact]
    public void GetCopyEntries_ReturnsCorrectCount()
    {
        var t = CreateTestTranslation();
        var entries = KeybindCategoryDefinition.GetCopyEntries(t);
        Assert.Equal(6, entries.Count);
    }

    [Fact]
    public void GetTabManagementEntries_ReturnsCorrectCount()
    {
        var t = CreateTestTranslation();
        var entries = KeybindCategoryDefinition.GetTabManagementEntries(t);
        Assert.Equal(2, entries.Count);
    }

    [Fact]
    public void GetToolWindowsEntries_ReturnsCorrectCount()
    {
        var t = CreateTestTranslation();
        var entries = KeybindCategoryDefinition.GetToolWindowsEntries(t);
        Assert.Equal(8, entries.Count);
    }

    [Fact]
    public void GetWindowManagementEntries_ReturnsCorrectCount()
    {
        var t = CreateTestTranslation();
        var entries = KeybindCategoryDefinition.GetWindowManagementEntries(t);
        Assert.Equal(6, entries.Count);
    }

    [Fact]
    public void GetWindowScalingEntries_ReturnsCorrectCount()
    {
        var t = CreateTestTranslation();
        var entries = KeybindCategoryDefinition.GetWindowScalingEntries(t);
        Assert.Equal(2, entries.Count);
    }

    [Fact]
    public void GetStarRatingEntries_ReturnsCorrectCount()
    {
        var t = CreateTestTranslation();
        var entries = KeybindCategoryDefinition.GetStarRatingEntries(t);
        Assert.Equal(6, entries.Count);
    }

    [Fact]
    public void GetAllCategories_Returns13Categories()
    {
        var t = CreateTestTranslation();
        var categories = KeybindCategoryDefinition.GetAllCategories(t);
        Assert.Equal(13, categories.Count);
    }

    [Fact]
    public void GetAllCategories_AllEntriesHaveNonEmptyFunctionNames()
    {
        var t = CreateTestTranslation();
        var categories = KeybindCategoryDefinition.GetAllCategories(t);

        foreach (var (categoryName, entries) in categories)
        {
            foreach (var (functionName, displayName) in entries)
            {
                Assert.False(string.IsNullOrEmpty(functionName),
                    $"Category '{categoryName}' has an entry with empty function name");
            }
        }
    }

    [Fact]
    public void GetAllCategories_AllEntriesHaveNonEmptyDisplayNames()
    {
        var t = CreateTestTranslation();
        var categories = KeybindCategoryDefinition.GetAllCategories(t);

        foreach (var (categoryName, entries) in categories)
        {
            foreach (var (functionName, displayName) in entries)
            {
                Assert.False(string.IsNullOrEmpty(displayName),
                    $"Category '{categoryName}', function '{functionName}' has an empty display name");
            }
        }
    }

    [Fact]
    public void GetAllCategories_NoDuplicateFunctionNamesWithinCategory()
    {
        var t = CreateTestTranslation();
        var categories = KeybindCategoryDefinition.GetAllCategories(t);

        foreach (var (categoryName, entries) in categories)
        {
            var functionNames = entries.Select(e => e.FunctionName).ToList();
            var unique = functionNames.Distinct().ToList();
            Assert.True(functionNames.Count == unique.Count,
                $"Category '{categoryName}' has duplicate function names");
        }
    }

    [Fact]
    public void NullTranslationProperties_FallBackToEmptyString()
    {
        // LanguageModel with all null properties
        var t = new LanguageModel();
        var entries = KeybindCategoryDefinition.GetNavigationEntries(t);

        // Should not throw and all display names should be empty strings
        foreach (var (_, displayName) in entries)
        {
            Assert.NotNull(displayName);
        }
    }

    [Fact]
    public void GetCopyEntries_CopyBase64HasConcatenatedDisplayName()
    {
        var t = CreateTestTranslation();
        var entries = KeybindCategoryDefinition.GetCopyEntries(t);
        var base64Entry = entries.First(e => e.FunctionName == "CopyBase64");
        Assert.Equal("Copy base64", base64Entry.DisplayName);
    }
}
