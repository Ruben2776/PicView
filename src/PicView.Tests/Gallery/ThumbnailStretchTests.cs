using System.Globalization;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Views.Config;
using PicView.Avalonia.Views.Gallery;
using PicView.Core.Config;
using PicView.Core.Gallery;
using PicView.Core.Localization;
using PicView.Core.Models;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Tests.Gallery;

/// <summary>
/// The Thumbnail Stretch of the Docked Gallery and the Expanded Gallery: how it loads from the settings file, how the
/// Settings window picks it, and how thumbnails follow the gallery that is shown.
/// </summary>
[Collection("Sequential")]
public class ThumbnailStretchTests
{
    public ThumbnailStretchTests()
    {
        ObservableSystem.DefaultFrameProvider = new MockFrameProvider();
        TranslationManager.Init();
        SetDefaults();
    }

    private sealed class MockFrameProvider : FrameProvider
    {
        public override long GetFrameCount() => 0;
        public override void Register(IFrameRunnerWorkItem callback) => callback.MoveNext(0);
    }

    // Settings files up to 5.1.4 are version 2.0. Those from 5.0.6 and earlier can hold 2 (Fill) and 3 (None), which
    // 5.1.0 removed, and from 5.1.0 the Settings window saved 2 and 3 when Square or Fill Square was picked
    [Theory]
    [InlineData("DockedGalleryStretchMode", 2)]
    [InlineData("DockedGalleryStretchMode", 3)]
    [InlineData("ExpandedGalleryStretchMode", 2)]
    [InlineData("ExpandedGalleryStretchMode", 3)]
    public void SavedThumbnailStretch_RemovedValue_LoadsAsAnExistingStretch(string key, int saved)
    {
        var loaded = LoadGallerySetting(2.0, key, saved);

        Assert.True(Enum.IsDefined(GetStretch(loaded, key)));
    }

    [AvaloniaTheory]
    [InlineData("DockedGalleryComboBox", "DockedGallerySquareItem", GalleryStretchMode.Square)]
    [InlineData("DockedGalleryComboBox", "DockedGalleryFillSquareItem", GalleryStretchMode.FillSquare)]
    [InlineData("FullGalleryComboBox", "FullGallerySquareItem", GalleryStretchMode.Square)]
    [InlineData("FullGalleryComboBox", "FullGalleryFillSquareItem", GalleryStretchMode.FillSquare)]
    public void SettingsComboBox_PickingAnItem_SetsThatThumbnailStretch(string comboBoxName, string itemName,
        GalleryStretchMode expected)
    {
        var (core, view) = ShowGallerySettings();
        var isDocked = comboBoxName == "DockedGalleryComboBox";

        view.FindControl<ComboBox>(comboBoxName)!.SelectedItem = view.FindControl<ComboBoxItem>(itemName);

        var gallerySettings = core.GallerySettings;
        if (isDocked)
        {
            Assert.Equal(expected, gallerySettings.DockedGalleryStretchMode.Value);
            Assert.Equal(expected, Settings.Gallery.DockedGalleryStretchMode);
            Assert.Equal(expected == GalleryStretchMode.Square, gallerySettings.IsDockedStretchSquare.Value);
            Assert.Equal(expected == GalleryStretchMode.FillSquare, gallerySettings.IsDockedStretchSquareFill.Value);
        }
        else
        {
            Assert.Equal(expected, gallerySettings.ExpandedGalleryStretchMode.Value);
            Assert.Equal(expected, Settings.Gallery.ExpandedGalleryStretchMode);
            Assert.Equal(expected == GalleryStretchMode.Square, gallerySettings.IsExpandedStretchSquare.Value);
            Assert.Equal(expected == GalleryStretchMode.FillSquare, gallerySettings.IsExpandedStretchSquareFill.Value);
        }
    }

    [AvaloniaTheory]
    [InlineData("DockedGalleryComboBox", "DockedGallerySquareItem", GalleryStretchMode.Square)]
    [InlineData("DockedGalleryComboBox", "DockedGalleryFillSquareItem", GalleryStretchMode.FillSquare)]
    [InlineData("DockedGalleryComboBox", "DockedGalleryUniformItem", GalleryStretchMode.Uniform)]
    [InlineData("FullGalleryComboBox", "FullGallerySquareItem", GalleryStretchMode.Square)]
    [InlineData("FullGalleryComboBox", "FullGalleryFillSquareItem", GalleryStretchMode.FillSquare)]
    [InlineData("FullGalleryComboBox", "FullGalleryUniformToFillItem", GalleryStretchMode.UniformToFill)]
    public void SettingsComboBox_GalleryMenuSetsStretch_ShowsIt(string comboBoxName, string itemName,
        GalleryStretchMode mode)
    {
        var (core, view) = ShowGallerySettings();
        var comboBox = view.FindControl<ComboBox>(comboBoxName)!;

        // Pick something else in the combo box first, so it has to follow the menu rather than keep its selection
        comboBox.SelectedIndex = mode is GalleryStretchMode.Uniform ? 1 : 0;
        var command = comboBoxName == "DockedGalleryComboBox"
            ? core.GallerySettings.SetDockedStretchModeCommand
            : core.GallerySettings.SetExpandedStretchModeCommand;
        command.Execute(mode);

        Assert.Same(view.FindControl<ComboBoxItem>(itemName), comboBox.SelectedItem);
    }

    [AvaloniaFact]
    public void Thumbnail_ShownGalleryChangesStretch_TakesIt()
    {
        WithThumbnail((core, image) =>
        {
            core.GallerySettings.ThumbnailStretch.Value = GalleryStretchMode.UniformToFill;
            Assert.Equal(Stretch.UniformToFill, image.Stretch);

            core.GallerySettings.ThumbnailStretch.Value = GalleryStretchMode.Uniform;
            Assert.Equal(Stretch.Uniform, image.Stretch);
        });
    }

    [AvaloniaFact]
    public void Thumbnail_FillSquare_Crops()
    {
        WithThumbnail((core, image) =>
        {
            core.GallerySettings.ThumbnailStretch.Value = GalleryStretchMode.FillSquare;

            Assert.Equal(Stretch.UniformToFill, image.Stretch);
        });
    }

    private static AppSettings LoadGallerySetting(double version, string key, int saved)
    {
        var json = $$"""{ "Version": {{version.ToString(CultureInfo.InvariantCulture)}}, "Gallery": { "{{key}}": {{saved}} } }""";
        var loaded = JsonSerializer.Deserialize(json, SettingsGenerationContext.Default.AppSettings);
        return EnsureSettings(loaded!);
    }

    private static GalleryStretchMode GetStretch(AppSettings settings, string key) =>
        key == "DockedGalleryStretchMode"
            ? settings.Gallery!.DockedGalleryStretchMode
            : settings.Gallery!.ExpandedGalleryStretchMode;

    private static (CoreViewModel Core, GallerySettingsView View) ShowGallerySettings()
    {
        var core = new CoreViewModel(null, _ => ValueTask.FromResult(new ImageModel()));
        core.GallerySettings.Initialize();
        var view = new GallerySettingsView();
        var window = new Window { DataContext = core, Content = view };
        window.Show();
        return (core, view);
    }

    // A thumbnail finds the shared gallery settings through the application's DataContext
    private static void WithThumbnail(Action<CoreViewModel, ThumbImage> test)
    {
        var core = new CoreViewModel(null, _ => ValueTask.FromResult(new ImageModel()));
        var application = Application.Current!;
        var previous = application.DataContext;
        application.DataContext = core;
        try
        {
            var item = new GalleryItem();
            test(core, item.FindControl<ThumbImage>("GalleryImage")!);
        }
        finally
        {
            application.DataContext = previous;
        }
    }
}
