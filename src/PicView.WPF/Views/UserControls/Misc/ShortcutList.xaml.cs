using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using PicView.WPF.Animations;
using PicView.Core.Config;
using PicView.Core.Localization;
using PicView.WPF.Shortcuts;
using PicView.WPF.UILogic;

namespace PicView.WPF.Views.UserControls.Misc;

public partial class ShortcutList
{
    public ShortcutList()
    {
        InitializeComponent();

        var color = SettingsHelper.Settings.Theme.Dark ? Colors.White : (Color)Application.Current.Resources["MainColor"];
        SetDefaultButton.MouseEnter += delegate
        {
            AnimationHelper.MouseOverColorEvent(color.A, color.R, color.G, color.B, SetToDefaultText);
        };
        SetDefaultButton.MouseEnter += delegate { AnimationHelper.MouseEnterBgTexColor(SetToDefaultBrush); };
        SetDefaultButton.MouseLeave += delegate
        {
            AnimationHelper.MouseLeaveColorEvent(color.A, color.R, color.G, color.B, SetToDefaultText);
        };
        SetDefaultButton.MouseLeave += delegate { AnimationHelper.MouseLeaveBgTexColor(SetToDefaultBrush); };
        SetDefaultButton.MouseLeftButtonDown += async delegate
        {
            await CustomKeybindings.SetDefaultKeybindings().ConfigureAwait(false);
            await Dispatcher.InvokeAsync(() =>
            {
                ConfigureWindows.GetAboutWindow.Container.Children.Remove(this);
                ConfigureWindows.GetAboutWindow.Container.Children.RemoveAt(ConfigureWindows.GetAboutWindow.Container.Children.Count - 1);
                var shortcutList = new ShortcutList { HorizontalAlignment = HorizontalAlignment.Center };
                ConfigureWindows.GetAboutWindow.Container.Children.Add(shortcutList);
                var credits = new Credits
                {
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                ConfigureWindows.GetAboutWindow.Container.Children.Add(credits);
            });
        };

        #region Loaded and key events

        // NextBox
        NextBox1.Loaded += async (s, _) => await UpdateTextBoxes(s, "Next", false).ConfigureAwait(false);
        NextBox2.Loaded += async (s, _) => await UpdateTextBoxes(s, "Next", true).ConfigureAwait(false);

        NextBox1.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "Next", false).ConfigureAwait(false);
        NextBox2.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "Next", true).ConfigureAwait(false);

        // PrevBox
        PrevBox1.Loaded += async (s, _) => await UpdateTextBoxes(s, "Prev", false).ConfigureAwait(false);
        PrevBox2.Loaded += async (s, _) => await UpdateTextBoxes(s, "Prev", true).ConfigureAwait(false);

        PrevBox1.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "Prev", false).ConfigureAwait(false);
        PrevBox2.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "Prev", true).ConfigureAwait(false);

        // ToggleLoopingBox
        ToggleLoopingBox1.Loaded += async (s, _) => await UpdateTextBoxes(s, "ToggleLooping", false).ConfigureAwait(false);
        ToggleLoopingBox2.Loaded += async (s, _) => await UpdateTextBoxes(s, "ToggleLooping", true).ConfigureAwait(false);

        ToggleLoopingBox1.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "ToggleLooping", false).ConfigureAwait(false);
        ToggleLoopingBox2.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "ToggleLooping", true).ConfigureAwait(false);

        // GalleryBox
        GalleryBox1.Loaded += async (s, _) => await UpdateTextBoxes(s, "GalleryClick", false).ConfigureAwait(false);
        GalleryBox2.Loaded += async (s, _) => await UpdateTextBoxes(s, "GalleryClick", true).ConfigureAwait(false);

        GalleryBox1.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "GalleryClick", false).ConfigureAwait(false);
        GalleryBox2.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "GalleryClick", true).ConfigureAwait(false);

        // UpBox || Rotate right
        UpBox1.Loaded += async (s, _) => await UpdateTextBoxes(s, "Up", false).ConfigureAwait(false);
        UpBox2.Loaded += async (s, _) => await UpdateTextBoxes(s, "Up", true).ConfigureAwait(false);

        UpBox1.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "Up", false).ConfigureAwait(false);
        UpBox2.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "Up", true).ConfigureAwait(false);

        // DownBox || Rotate left
        DownBox1.Loaded += async (s, _) => await UpdateTextBoxes(s, "Down", false).ConfigureAwait(false);
        DownBox2.Loaded += async (s, _) => await UpdateTextBoxes(s, "Down", true).ConfigureAwait(false);

        DownBox1.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "Down", false).ConfigureAwait(false);
        DownBox2.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "Down", true).ConfigureAwait(false);

        // ScrollToTopBox
        ScrollToTopBox1.Loaded += async (s, _) => await UpdateTextBoxes(s, "ScrollToTop", false).ConfigureAwait(false);
        ScrollToTopBox2.Loaded += async (s, _) => await UpdateTextBoxes(s, "ScrollToTop", true).ConfigureAwait(false);

        ScrollToTopBox1.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "ScrollToTop", false).ConfigureAwait(false);
        ScrollToTopBox2.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "ScrollToTop", true).ConfigureAwait(false);

        // ScrlBottomBox
        ScrlBottomBox1.Loaded += async (s, _) => await UpdateTextBoxes(s, "ScrollToBottom", false).ConfigureAwait(false);
        ScrlBottomBox2.Loaded += async (s, _) => await UpdateTextBoxes(s, "ScrollToBottom", true).ConfigureAwait(false);

        ScrlBottomBox1.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "ScrollToBottom", false).ConfigureAwait(false);
        ScrlBottomBox2.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "ScrollToBottom", true).ConfigureAwait(false);

        // Toggle Scroll
        ToggleScrlBox1.Loaded += async (s, _) => await UpdateTextBoxes(s, "ToggleScroll", false).ConfigureAwait(false);
        ToggleScrlBox2.Loaded += async (s, _) => await UpdateTextBoxes(s, "ToggleScroll", true).ConfigureAwait(false);

        ToggleScrlBox1.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "ToggleScroll", false).ConfigureAwait(false);
        ToggleScrlBox2.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "ToggleScroll", true).ConfigureAwait(false);

        // Zoom In
        ZoomInBox1.Loaded += async (s, _) => await UpdateTextBoxes(s, "ZoomIn", false).ConfigureAwait(false);

        ZoomInBox1.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "ZoomIn", false).ConfigureAwait(false);

        // Zoom Out
        ZoomOutBox1.Loaded += async (s, _) => await UpdateTextBoxes(s, "ZoomOut", false).ConfigureAwait(false);

        ZoomOutBox1.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "ZoomOut", false).ConfigureAwait(false);

        // Reset zoom
        ResetZoomBox1.Loaded += async (s, _) => await UpdateTextBoxes(s, "ResetZoom", false).ConfigureAwait(false);

        ResetZoomBox1.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "ResetZoom", false).ConfigureAwait(false);

        // Stretch
        StretchBox1.Loaded += async (s, _) => await UpdateTextBoxes(s, "Stretch", false).ConfigureAwait(false);
        StretchBox2.Loaded += async (s, _) => await UpdateTextBoxes(s, "Stretch", true).ConfigureAwait(false);

        StretchBox1.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "Stretch", false).ConfigureAwait(false);
        StretchBox2.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "Stretch", true).ConfigureAwait(false);

        // Flip
        FlipBox1.Loaded += async (s, _) => await UpdateTextBoxes(s, "Flip", false).ConfigureAwait(false);
        FlipBox2.Loaded += async (s, _) => await UpdateTextBoxes(s, "Flip", true).ConfigureAwait(false);

        FlipBox1.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "Flip", false).ConfigureAwait(false);
        FlipBox2.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "Flip", true).ConfigureAwait(false);

        // Crop
        CropBox1.Loaded += async (s, _) => await UpdateTextBoxes(s, "Crop", false).ConfigureAwait(false);
        CropBox2.Loaded += async (s, _) => await UpdateTextBoxes(s, "Crop", true).ConfigureAwait(false);

        CropBox1.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "Crop", false).ConfigureAwait(false);
        CropBox2.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "Crop", true).ConfigureAwait(false);

        // Change Background
        ChangeBgBox1.Loaded += async (s, _) => await UpdateTextBoxes(s, "ChangeBackground", false).ConfigureAwait(false);
        ChangeBgBox2.Loaded += async (s, _) => await UpdateTextBoxes(s, "ChangeBackground", true).ConfigureAwait(false);

        ChangeBgBox1.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "ChangeBackground", false).ConfigureAwait(false);
        ChangeBgBox2.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "ChangeBackground", true).ConfigureAwait(false);

        // Change Background
        QuickResizeBox1.Loaded += async (s, _) => await UpdateTextBoxes(s, "ResizeImage", false).ConfigureAwait(false);
        QuickResizeBox2.Loaded += async (s, _) => await UpdateTextBoxes(s, "ResizeImage", true).ConfigureAwait(false);

        QuickResizeBox1.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "ResizeImage", false).ConfigureAwait(false);
        QuickResizeBox2.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "ResizeImage", true).ConfigureAwait(false);

        // Color Picker
        ColorPickBox1.Loaded += async (s, _) => await UpdateTextBoxes(s, "ColorPicker", false).ConfigureAwait(false);
        ColorPickBox2.Loaded += async (s, _) => await UpdateTextBoxes(s, "ColorPicker", true).ConfigureAwait(false);

        ColorPickBox1.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "ColorPicker", false).ConfigureAwait(false);
        ColorPickBox2.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "ColorPicker", true).ConfigureAwait(false);

        // Optimize image
        OptimizeBox1.Loaded += async (s, _) => await UpdateTextBoxes(s, "OptimizeImage", false).ConfigureAwait(false);
        OptimizeBox2.Loaded += async (s, _) => await UpdateTextBoxes(s, "OptimizeImage", true).ConfigureAwait(false);

        OptimizeBox1.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "OptimizeImage", false).ConfigureAwait(false);
        OptimizeBox2.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "OptimizeImage", true).ConfigureAwait(false);

        // Toggle UI
        ToggleUIBox.Loaded += async (s, _) => await UpdateTextBoxes(s, "ToggleInterface", false).ConfigureAwait(false);

        ToggleUIBox.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "ToggleInterface", false).ConfigureAwait(false);

        // Toggle Fullscreen
        ToggleFullscreenBox1.Loaded += async (s, _) => await UpdateTextBoxes(s, "Fullscreen", false).ConfigureAwait(false);
        ToggleFullscreenBox2.Loaded += async (s, _) => await UpdateTextBoxes(s, "Fullscreen", true).ConfigureAwait(false);

        ToggleFullscreenBox1.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "Fullscreen", false).ConfigureAwait(false);
        ToggleFullscreenBox2.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "Fullscreen", true).ConfigureAwait(false);

        // Slideshow
        SlideshowBox1.Loaded += async (s, _) => await UpdateTextBoxes(s, "Slideshow", false).ConfigureAwait(false);
        SlideshowBox2.Loaded += async (s, _) => await UpdateTextBoxes(s, "Slideshow", true).ConfigureAwait(false);

        SlideshowBox1.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "Slideshow", false).ConfigureAwait(false);
        SlideshowBox2.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "Slideshow", true).ConfigureAwait(false);

        // Show Image Gallery
        ShowImageGalleryBox1.Loaded += async (s, _) => await UpdateTextBoxes(s, "ToggleGallery", false).ConfigureAwait(false);
        ShowImageGalleryBox2.Loaded += async (s, _) => await UpdateTextBoxes(s, "ToggleGallery", true).ConfigureAwait(false);

        ShowImageGalleryBox1.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "ToggleGallery", false).ConfigureAwait(false);
        ShowImageGalleryBox2.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "ToggleGallery", true).ConfigureAwait(false);

        // Open
        OpenBox.Loaded += async (s, _) => await UpdateTextBoxes(s, "Open", false).ConfigureAwait(false);

        OpenBox.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "Open", false).ConfigureAwait(false);

        // Save
        SaveBox.Loaded += async (s, _) => await UpdateTextBoxes(s, "Save", false).ConfigureAwait(false);

        SaveBox.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "Save", false).ConfigureAwait(false);

        // Print
        PrintBox.Loaded += async (s, _) => await UpdateTextBoxes(s, "Print", false).ConfigureAwait(false);

        PrintBox.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "Print", false).ConfigureAwait(false);

        // Delete File
        DeleteBox1.Loaded += async (s, _) => await UpdateTextBoxes(s, "DeleteFile", false).ConfigureAwait(false);
        DeleteBox2.Loaded += async (s, _) => await UpdateTextBoxes(s, "DeleteFile", true).ConfigureAwait(false);

        DeleteBox1.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "DeleteFile", false).ConfigureAwait(false);
        DeleteBox2.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "DeleteFile", true).ConfigureAwait(false);

        // Rename File
        RenameBox1.Loaded += async (s, _) => await UpdateTextBoxes(s, "Rename", false).ConfigureAwait(false);
        RenameBox2.Loaded += async (s, _) => await UpdateTextBoxes(s, "Rename", true).ConfigureAwait(false);

        RenameBox1.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "Rename", false).ConfigureAwait(false);
        RenameBox2.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "Rename", true).ConfigureAwait(false);

        // Show in folder
        ShowInFolderBox1.Loaded += async (s, _) => await UpdateTextBoxes(s, "OpenInExplorer", false).ConfigureAwait(false);
        ShowInFolderBox2.Loaded += async (s, _) => await UpdateTextBoxes(s, "OpenInExplorer", true).ConfigureAwait(false);

        ShowInFolderBox1.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "OpenInExplorer", false).ConfigureAwait(false);
        ShowInFolderBox2.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "OpenInExplorer", true).ConfigureAwait(false);

        // File properties
        FilePropertiesBox.Loaded += async (s, _) => await UpdateTextBoxes(s, "ShowFileProperties", false).ConfigureAwait(false);

        FilePropertiesBox.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "ShowFileProperties", false).ConfigureAwait(false);

        // Copy file
        CopyFileBox.Loaded += async (s, _) => await UpdateTextBoxes(s, "CopyFile", false).ConfigureAwait(false);

        CopyFileBox.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "CopyFile", false).ConfigureAwait(false);

        // Copy file path
        CopyFilePathBox.Loaded += async (s, _) => await UpdateTextBoxes(s, "CopyFilePath", false).ConfigureAwait(false);

        CopyFilePathBox.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "CopyFilePath", false).ConfigureAwait(false);

        // Copy image
        CopyImageBox1.Loaded += async (s, _) => await UpdateTextBoxes(s, "CopyImage", false).ConfigureAwait(false);

        CopyImageBox1.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "CopyImage", false).ConfigureAwait(false);

        // Copy base64
        CopyBase64Box1.Loaded += async (s, _) => await UpdateTextBoxes(s, "CopyBase64", false).ConfigureAwait(false);
        CopyBase64Box2.Loaded += async (s, _) => await UpdateTextBoxes(s, "CopyBase64", true).ConfigureAwait(false);

        CopyBase64Box1.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "CopyBase64", false).ConfigureAwait(false);
        CopyBase64Box2.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "CopyBase64", true).ConfigureAwait(false);

        // Paste
        PasteBox.Loaded += async (s, _) => await UpdateTextBoxes(s, "Paste", false).ConfigureAwait(false);

        PasteBox.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "Paste", false).ConfigureAwait(false);

        // Duplicate
        DuplicateBox1.Loaded += async (s, _) => await UpdateTextBoxes(s, "DuplicateFile", false).ConfigureAwait(false);
        DuplicateBox2.Loaded += async (s, _) => await UpdateTextBoxes(s, "DuplicateFile", true).ConfigureAwait(false);

        DuplicateBox1.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "DuplicateFile", false).ConfigureAwait(false);
        DuplicateBox2.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "DuplicateFile", true).ConfigureAwait(false);

        // Cut file
        CutBox.Loaded += async (s, _) => await UpdateTextBoxes(s, "CutFile", false).ConfigureAwait(false);
        CutBox.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "CutFile", false).ConfigureAwait(false);

        // About window
        AboutBox1.Loaded += async (s, _) => await UpdateTextBoxes(s, "AboutWindow", false).ConfigureAwait(false);
        AboutBox2.Loaded += async (s, _) => await UpdateTextBoxes(s, "AboutWindow", true).ConfigureAwait(false);

        AboutBox1.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "AboutWindow", false).ConfigureAwait(false);
        AboutBox2.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "AboutWindow", true).ConfigureAwait(false);

        // Settings window
        SettingsBox1.Loaded += async (s, _) => await UpdateTextBoxes(s, "SettingsWindow", false).ConfigureAwait(false);
        SettingsBox2.Loaded += async (s, _) => await UpdateTextBoxes(s, "SettingsWindow", true).ConfigureAwait(false);

        SettingsBox1.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "SettingsWindow", false).ConfigureAwait(false);
        SettingsBox2.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "SettingsWindow", true).ConfigureAwait(false);

        // Image info window
        ImageInfoBox1.Loaded += async (s, _) => await UpdateTextBoxes(s, "ImageInfoWindow", false).ConfigureAwait(false);
        ImageInfoBox2.Loaded += async (s, _) => await UpdateTextBoxes(s, "ImageInfoWindow", true).ConfigureAwait(false);

        ImageInfoBox1.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "ImageInfoWindow", false).ConfigureAwait(false);
        ImageInfoBox2.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "ImageInfoWindow", true).ConfigureAwait(false);

        // Resize Window
        ResizeWindowBox1.Loaded += async (s, _) => await UpdateTextBoxes(s, "ResizeWindow", false).ConfigureAwait(false);
        ResizeWindowBox2.Loaded += async (s, _) => await UpdateTextBoxes(s, "ResizeWindow", true).ConfigureAwait(false);

        ResizeWindowBox1.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "ResizeWindow", false).ConfigureAwait(false);
        ResizeWindowBox2.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "ResizeWindow", true).ConfigureAwait(false);

        // Close
        CloseBox1.Loaded += async (s, _) => await UpdateTextBoxes(s, "Close", false).ConfigureAwait(false);
        CloseBox2.Loaded += async (s, _) => await UpdateTextBoxes(s, "Close", true).ConfigureAwait(false);

        CloseBox1.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "Close", false).ConfigureAwait(false);
        CloseBox2.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "Close", true).ConfigureAwait(false);

        // New window
        NewWindowBox.Loaded += async (s, _) => await UpdateTextBoxes(s, "OpenWith", false).ConfigureAwait(false);

        NewWindowBox.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "OpenWith", false).ConfigureAwait(false);

        // Center window
        CenterWindowBox1.Loaded += async (s, _) => await UpdateTextBoxes(s, "Center", false).ConfigureAwait(false);
        CenterWindowBox2.Loaded += async (s, _) => await UpdateTextBoxes(s, "Center", true).ConfigureAwait(false);

        CenterWindowBox1.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "Center", false).ConfigureAwait(false);
        CenterWindowBox2.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "Center", true).ConfigureAwait(false);

        // Topmost
        TopMostBox1.Loaded += async (s, _) => await UpdateTextBoxes(s, "SetTopMost", false).ConfigureAwait(false);
        TopMostBox2.Loaded += async (s, _) => await UpdateTextBoxes(s, "SetTopMost", true).ConfigureAwait(false);

        TopMostBox1.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "SetTopMost", false).ConfigureAwait(false);
        TopMostBox2.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "SetTopMost", true).ConfigureAwait(false);

        // Auto Fit Window
        AutoFitWindowBox1.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "AutoFitWindow", false).ConfigureAwait(false);
        AutoFitWindowBox2.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "AutoFitWindow", true).ConfigureAwait(false);

        AutoFitWindowBox1.Loaded += async (s, _) => await UpdateTextBoxes(s, "AutoFitWindow", false).ConfigureAwait(false);
        AutoFitWindowBox2.Loaded += async (s, _) => await UpdateTextBoxes(s, "AutoFitWindow", true).ConfigureAwait(false);

        // Auto Fit Window fill
        AutoFitFillWindowBox1.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "AutoFitWindowAndStretch", false).ConfigureAwait(false);
        AutoFitFillWindowBox2.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "AutoFitWindowAndStretch", true).ConfigureAwait(false);

        AutoFitFillWindowBox1.Loaded += async (s, _) => await UpdateTextBoxes(s, "AutoFitWindowAndStretch", false).ConfigureAwait(false);
        AutoFitFillWindowBox2.Loaded += async (s, _) => await UpdateTextBoxes(s, "AutoFitWindowAndStretch", true).ConfigureAwait(false);

        // Normal window
        NormalWindowBox1.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "NormalWindow", false).ConfigureAwait(false);
        NormalWindowBox2.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "NormalWindow", true).ConfigureAwait(false);

        NormalWindowBox1.Loaded += async (s, _) => await UpdateTextBoxes(s, "NormalWindow", false).ConfigureAwait(false);
        NormalWindowBox2.Loaded += async (s, _) => await UpdateTextBoxes(s, "NormalWindow", true).ConfigureAwait(false);

        // Normal window fill
        NormalWindowFillBox1.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "NormalWindowAndStretch", false).ConfigureAwait(false);
        NormalWindowFillBox2.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "NormalWindowAndStretch", true).ConfigureAwait(false);

        NormalWindowFillBox1.Loaded += async (s, _) => await UpdateTextBoxes(s, "NormalWindowAndStretch", false).ConfigureAwait(false);
        NormalWindowFillBox2.Loaded += async (s, _) => await UpdateTextBoxes(s, "NormalWindowAndStretch", true).ConfigureAwait(false);

        // Open with...
        OpenWithBox.Loaded += async (s, _) => await UpdateTextBoxes(s, "OpenWith", false).ConfigureAwait(false);

        OpenWithBox.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "OpenWith", false).ConfigureAwait(false);

        // Reload
        OpenWithBox.Loaded += async (s, _) => await UpdateTextBoxes(s, "OpenWith", false).ConfigureAwait(false);

        OpenWithBox.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "OpenWith", false).ConfigureAwait(false);

        // Set 1 star
        SetStar1Box1.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "Set1Star", false).ConfigureAwait(false);
        SetStar1Box2.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "Set1Star", true).ConfigureAwait(false);

        SetStar1Box1.Loaded += async (s, _) => await UpdateTextBoxes(s, "Set1Star", false).ConfigureAwait(false);
        SetStar1Box2.Loaded += async (s, _) => await UpdateTextBoxes(s, "Set1Star", true).ConfigureAwait(false);

        // Set 2 star
        SetStar2Box1.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "Set2Star", false).ConfigureAwait(false);
        SetStar2Box2.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "Set2Star", true).ConfigureAwait(false);

        SetStar2Box1.Loaded += async (s, _) => await UpdateTextBoxes(s, "Set2Star", false).ConfigureAwait(false);
        SetStar2Box2.Loaded += async (s, _) => await UpdateTextBoxes(s, "Set2Star", true).ConfigureAwait(false);

        // Set 3 star
        SetStar3Box1.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "Set3Star", false).ConfigureAwait(false);
        SetStar3Box2.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "Set3Star", true).ConfigureAwait(false);

        SetStar3Box1.Loaded += async (s, _) => await UpdateTextBoxes(s, "Set3Star", false).ConfigureAwait(false);
        SetStar3Box2.Loaded += async (s, _) => await UpdateTextBoxes(s, "Set3Star", true).ConfigureAwait(false);

        // Set 4 star
        SetStar4Box1.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "Set4Star", false).ConfigureAwait(false);
        SetStar4Box2.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "Set4Star", true).ConfigureAwait(false);

        SetStar4Box1.Loaded += async (s, _) => await UpdateTextBoxes(s, "Set4Star", false).ConfigureAwait(false);
        SetStar4Box2.Loaded += async (s, _) => await UpdateTextBoxes(s, "Set4Star", true).ConfigureAwait(false);

        // Set 5 star
        SetStar5Box1.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "Set5Star", false).ConfigureAwait(false);
        SetStar5Box2.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "Set5Star", true).ConfigureAwait(false);

        SetStar5Box1.Loaded += async (s, _) => await UpdateTextBoxes(s, "Set5Star", false).ConfigureAwait(false);
        SetStar5Box2.Loaded += async (s, _) => await UpdateTextBoxes(s, "Set5Star", true).ConfigureAwait(false);

        // Reset stars
        ResetStarsBox1.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "Set0Star", false).ConfigureAwait(false);
        ResetStarsBox2.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "Set0Star", true).ConfigureAwait(false);

        ResetStarsBox1.Loaded += async (s, _) => await UpdateTextBoxes(s, "Set0Star", false).ConfigureAwait(false);
        ResetStarsBox2.Loaded += async (s, _) => await UpdateTextBoxes(s, "Set0Star", true).ConfigureAwait(false);

        #endregion Loaded and key events

        UpdateLanguage();
    }

    /// <summary>
    /// Updates the TextBoxes based on the keys associated with the specified function.
    /// </summary>
    /// <param name="sender">The sender object.</param>
    /// <param name="functionName">The name of the function.</param>
    /// <param name="alt">A flag indicating whether it's an alternative key.</param>
    private async Task UpdateTextBoxes(object sender, string functionName, bool alt)
    {
        if (CustomKeybindings.CustomShortcuts is null)
        {
            return;
        }

        if (CustomKeybindings.CustomShortcuts.Count <= 0)
        {
            return;
        }
        var key = await Task.FromResult(GetFunctionKey()).ConfigureAwait(false);

        await Dispatcher.InvokeAsync(() =>
        {
            var textBox = (TextBox)sender;
            textBox.Text = key;
            textBox.GotKeyboardFocus += delegate
            {
                try
                {
                    textBox.Foreground = (SolidColorBrush)Application.Current.Resources["MainColorFadedBrush"];
                    textBox.Text = TranslationHelper.GetTranslation("PressKey");
                }
                catch (Exception)
                {
                    textBox.Text = string.Empty;
                }
            };
            textBox.LostKeyboardFocus += delegate
            {
                try
                {
                    if (textBox.Text.Equals(TranslationHelper.GetTranslation("PressKey")))
                    {
                        textBox.Text = GetFunctionKey();
                    }
                    textBox.Foreground = (SolidColorBrush)Application.Current.Resources["MainColorBrush"];
                }
                catch (Exception)
                {
                    textBox.Text = string.Empty;
                }
            };
            UpdateModifierTextBoxes(functionName, key, alt);
        }, DispatcherPriority.Normal);
        return;

        string GetFunctionKey()
        {
            var function = UIHelper.GetFunctionByName(functionName).Result;

            // Find the key associated with the specified function
            var keys = CustomKeybindings.CustomShortcuts.Where(x => x.Value == function).Select(x => x.Key).ToList();

            return keys.Count switch
            {
                <= 0 => string.Empty,
                1 => alt ? string.Empty : keys.FirstOrDefault().ToString(),
                _ => alt ? keys.LastOrDefault().ToString() : keys.FirstOrDefault().ToString()
            };
        }
    }

    /// <summary>
    /// Updates modifier TextBoxes based on the function, key, and alternative key flag.
    /// </summary>
    /// <param name="functionName">The name of the function.</param>
    /// <param name="key">The key associated with the function.</param>
    /// <param name="alt">A flag indicating whether it's an alternative key.</param>
    private void UpdateModifierTextBoxes(string functionName, string key, bool alt)
    {
        switch (functionName)
        {
            case "Next":
                if (!alt)
                {
                    LastImageBox1.Text = $"{TranslationHelper.GetTranslation("Ctrl")} + {key}";
                    NextFolderBox1.Text = $"{TranslationHelper.GetTranslation("Shift")} + {key}";
                }
                else
                {
                    LastImageBox2.Text = $"{TranslationHelper.GetTranslation("Ctrl")} + {key}";
                    NextFolderBox2.Text = $"{TranslationHelper.GetTranslation("Shift")} + {key}";
                }

                break;

            case "Prev":
                if (!alt)
                {
                    FirstImageBox1.Text = $"{TranslationHelper.GetTranslation("Ctrl")} + {key}";
                    PrevFolderBox1.Text = $"{TranslationHelper.GetTranslation("Shift")} + {key}";
                }
                else
                {
                    FirstImageBox2.Text = $"{TranslationHelper.GetTranslation("Ctrl")} + {key}";
                    PrevFolderBox2.Text = $"{TranslationHelper.GetTranslation("Shift")} + {key}";
                }
                break;

            case "Up":
                if (!alt)
                {
                    ScrollUpBox1.Text = key;
                }
                else
                {
                    ScrollUpBox2.Text = key;
                }
                break;

            case "Down":
                if (!alt)
                {
                    ScrollDownBox1.Text = key;
                }
                else
                {
                    ScrollDownBox2.Text = key;
                }

                break;
        }
    }

    internal void UpdateLanguage()
    {
        #region Translations

        ApplicationShortcutsLabel.Content = TranslationHelper.GetTranslation("ApplicationShortcuts");
        ChangeKeybindingText.Text = TranslationHelper.GetTranslation("ChangeKeybindingText");
        SetDefaultLabel.Content = TranslationHelper.GetTranslation("ResetButtonText");

        NavigationLabel.Content = TranslationHelper.GetTranslation("Navigation");

        NextImageTextBlock.Text = TranslationHelper.GetTranslation("NextImage");
        NextBox1.ToolTip = TranslationHelper.GetTranslation("ChangeKeybindingTooltip");
        NextBox2.ToolTip = TranslationHelper.GetTranslation("ChangeKeybindingTooltip");

        LastImageTextBlock.Text = TranslationHelper.GetTranslation("LastImage");
        LastImageBox1.ToolTip = TranslationHelper.GetTranslation("ChangeKeybindingTooltip");
        LastImageBox2.ToolTip = TranslationHelper.GetTranslation("ChangeKeybindingTooltip");

        LastImageTextBlock.Text = TranslationHelper.GetTranslation("LastImage");
        LastImageBox1.ToolTip = TranslationHelper.GetTranslation("ChangeKeybindingTooltip");
        LastImageBox2.ToolTip = TranslationHelper.GetTranslation("ChangeKeybindingTooltip");

        NextFolderTextBlock.Text = TranslationHelper.GetTranslation("NextFolder");
        NextFolderBox1.ToolTip = TranslationHelper.GetTranslation("ChangeKeybindingTooltip");
        NextFolderBox2.ToolTip = TranslationHelper.GetTranslation("ChangeKeybindingTooltip");

        PrevImageTextBlock.Text = TranslationHelper.GetTranslation("PrevImage");
        PrevBox1.ToolTip = TranslationHelper.GetTranslation("ChangeKeybindingTooltip");
        PrevBox2.ToolTip = TranslationHelper.GetTranslation("ChangeKeybindingTooltip");

        FirstImageTextBlock.Text = TranslationHelper.GetTranslation("FirstImage");
        FirstImageBox1.ToolTip = TranslationHelper.GetTranslation("ChangeKeybindingTooltip");
        FirstImageBox2.ToolTip = TranslationHelper.GetTranslation("ChangeKeybindingTooltip");

        PrevFolderTextBlock.Text = TranslationHelper.GetTranslation("PrevFolder");
        PrevFolderBox1.ToolTip = TranslationHelper.GetTranslation("ChangeKeybindingTooltip");
        PrevFolderBox2.ToolTip = TranslationHelper.GetTranslation("ChangeKeybindingTooltip");

        ToggleLoopingTextBlock.Text = TranslationHelper.GetTranslation("ToggleLooping");
        ToggleLoopingBox1.ToolTip = TranslationHelper.GetTranslation("ChangeKeybindingTooltip");
        ToggleLoopingBox2.ToolTip = TranslationHelper.GetTranslation("ChangeKeybindingTooltip");

        SelectGalleryThumbTextBlock.Text = TranslationHelper.GetTranslation("SelectGalleryThumb");
        GalleryBox1.ToolTip = TranslationHelper.GetTranslation("ChangeKeybindingTooltip");
        GalleryBox2.ToolTip = TranslationHelper.GetTranslation("ChangeKeybindingTooltip");

        ScrollAndRotateLabel.Content = TranslationHelper.GetTranslation("ScrollAndRotate");

        RotateRightTextBlock.Text = TranslationHelper.GetTranslation("RotateRight");
        UpBox1.ToolTip = TranslationHelper.GetTranslation("ChangeKeybindingTooltip");
        UpBox2.ToolTip = TranslationHelper.GetTranslation("ChangeKeybindingTooltip");

        ScrollUpTextBlock.Text = TranslationHelper.GetTranslation("ScrollUp");

        RotateLeftTextBlock.Text = TranslationHelper.GetTranslation("RotateLeft");
        DownBox1.ToolTip = TranslationHelper.GetTranslation("ChangeKeybindingTooltip");
        DownBox2.ToolTip = TranslationHelper.GetTranslation("ChangeKeybindingTooltip");

        ScrollDownTextBlock.Text = TranslationHelper.GetTranslation("ScrollDown");

        ScrollToTopBlock.Text = TranslationHelper.GetTranslation("ScrollToTop");
        ScrollToTopBox1.ToolTip = TranslationHelper.GetTranslation("ChangeKeybindingTooltip");
        ScrollToTopBox2.ToolTip = TranslationHelper.GetTranslation("ChangeKeybindingTooltip");

        ScrollToBottomTextBlock.Text = TranslationHelper.GetTranslation("ScrollToBottom");
        ScrlBottomBox1.ToolTip = TranslationHelper.GetTranslation("ChangeKeybindingTooltip");
        ScrlBottomBox2.ToolTip = TranslationHelper.GetTranslation("ChangeKeybindingTooltip");

        ToggleScrollBlock.Text = TranslationHelper.GetTranslation("ToggleScroll");
        ToggleScrlBox1.ToolTip = TranslationHelper.GetTranslation("ChangeKeybindingTooltip");
        ToggleScrlBox2.ToolTip = TranslationHelper.GetTranslation("ChangeKeybindingTooltip");

        ZoomLabel.Content = TranslationHelper.GetTranslation("Zoom");

        ZoomInTextBlock.Text = TranslationHelper.GetTranslation("ZoomIn");
        ZoomInBox1.ToolTip = TranslationHelper.GetTranslation("ChangeKeybindingTooltip");
        ZoomInBox2.Text = TranslationHelper.GetTranslation("MouseWheel");

        ZoomOutTextBlock.Text = TranslationHelper.GetTranslation("ZoomOut");
        ZoomOutBox1.ToolTip = TranslationHelper.GetTranslation("ChangeKeybindingTooltip");
        ZoomOutBox2.Text = TranslationHelper.GetTranslation("MouseWheel");

        PanTextBlock.Text = TranslationHelper.GetTranslation("Pan");
        PanBox1.Text = TranslationHelper.GetTranslation("MouseDrag");

        ResetZoomTextBlock.Text = TranslationHelper.GetTranslation("ResetZoom");
        ResetZoomBox1.ToolTip = TranslationHelper.GetTranslation("ChangeKeybindingTooltip");
        ResetZoomBox2.Text = TranslationHelper.GetTranslation("DoubleClick");

        ImageControlLabel.Content = TranslationHelper.GetTranslation("ImageControl");

        StretchImageTextBlock.Text = TranslationHelper.GetTranslation("StretchImage");
        StretchBox1.ToolTip = TranslationHelper.GetTranslation("ChangeKeybindingTooltip");
        StretchBox2.ToolTip = TranslationHelper.GetTranslation("ChangeKeybindingTooltip");

        FlipTextBlock.Text = TranslationHelper.GetTranslation("Flip");
        FlipBox1.ToolTip = TranslationHelper.GetTranslation("ChangeKeybindingTooltip");
        FlipBox2.ToolTip = TranslationHelper.GetTranslation("ChangeKeybindingTooltip");

        CropTextBlock.Text = TranslationHelper.GetTranslation("Crop");
        CropBox1.ToolTip = TranslationHelper.GetTranslation("ChangeKeybindingTooltip");
        CropBox2.ToolTip = TranslationHelper.GetTranslation("ChangeKeybindingTooltip");

        ChangeBackgroundTextBlock.Text = TranslationHelper.GetTranslation("ChangeBackground");
        ChangeBgBox1.ToolTip = TranslationHelper.GetTranslation("ChangeKeybindingTooltip");
        ChangeBgBox2.ToolTip = TranslationHelper.GetTranslation("ChangeKeybindingTooltip");

        ResizeImageTextBlock.Text = TranslationHelper.GetTranslation("ResizeImage");
        QuickResizeBox1.ToolTip = TranslationHelper.GetTranslation("ChangeKeybindingTooltip");
        QuickResizeBox2.ToolTip = TranslationHelper.GetTranslation("ChangeKeybindingTooltip");

        ColorPickerToolTextBlock.Text = TranslationHelper.GetTranslation("ColorPickerTool");
        ColorPickBox1.ToolTip = TranslationHelper.GetTranslation("ChangeKeybindingTooltip");
        ColorPickBox2.ToolTip = TranslationHelper.GetTranslation("ChangeKeybindingTooltip");

        OptimizeImageTextBlock.Text = TranslationHelper.GetTranslation("OptimizeImage");
        OptimizeBox1.ToolTip = TranslationHelper.GetTranslation("ChangeKeybindingTooltip");
        OptimizeBox2.ToolTip = TranslationHelper.GetTranslation("ChangeKeybindingTooltip");

        InterfaceConfigurationLabel.Content = TranslationHelper.GetTranslation("InterfaceConfiguration");

        ShowHideUiTextBlock.Text = TranslationHelper.GetTranslation("ShowHideUI");
        ToggleUIBox.ToolTip = TranslationHelper.GetTranslation("ChangeKeybindingTooltip");
        ToggleUIBox2.Text =
            $"{TranslationHelper.GetTranslation("Alt")} + z";

        ToggleFullscreenTextBlock1.Text = ToggleFullscreenTextBlock2.Text =
            TranslationHelper.GetTranslation("ToggleFullscreen");
        ToggleFullscreenTextBlock3.Text =
            $"{TranslationHelper.GetTranslation("Alt")} + {TranslationHelper.GetTranslation("Enter")}";
        ToggleFullscreenTextBlock4.Text =
            $"{TranslationHelper.GetTranslation("Shift")} + {TranslationHelper.GetTranslation("DoubleClick")}";

        ToggleFullscreenBox1.ToolTip = TranslationHelper.GetTranslation("ChangeKeybindingTooltip");
        ToggleFullscreenBox2.ToolTip = TranslationHelper.GetTranslation("ChangeKeybindingTooltip");

        SlideShowTextBlock.Text = TranslationHelper.GetTranslation("Slideshow");
        SlideshowBox1.ToolTip = TranslationHelper.GetTranslation("ChangeKeybindingTooltip");
        SlideshowBox2.ToolTip = TranslationHelper.GetTranslation("ChangeKeybindingTooltip");

        ShowImageGalleryTextBlock.Text = TranslationHelper.GetTranslation("ShowImageGallery");
        ShowImageGalleryBox1.ToolTip = TranslationHelper.GetTranslation("ChangeKeybindingTooltip");
        ShowImageGalleryBox2.ToolTip = TranslationHelper.GetTranslation("ChangeKeybindingTooltip");

        FileManagementLabel.Content = TranslationHelper.GetTranslation("FileManagement");

        OpenTextBlock.Text = TranslationHelper.GetTranslation("Open");
        OpenBox.ToolTip = TranslationHelper.GetTranslation("ChangeKeybindingTooltip");
        OpenBox2.Text = $"{TranslationHelper.GetTranslation("Ctrl")} + O";

        OpenWithTextBlock.Text = TranslationHelper.GetTranslation("OpenWith");
        OpenWithBox.ToolTip = TranslationHelper.GetTranslation("ChangeKeybindingTooltip");
        OpenWithBox2.Text = $"{TranslationHelper.GetTranslation("Ctrl")} + E";

        ReloadTextBlock.Text = TranslationHelper.GetTranslation("Reload");
        ReloadBox.ToolTip = TranslationHelper.GetTranslation("ChangeKeybindingTooltip");
        ReloadBox2.Text = $"{TranslationHelper.GetTranslation("Ctrl")} + R";

        SaveTextBlock.Text = TranslationHelper.GetTranslation("Save");
        SaveBox.ToolTip = TranslationHelper.GetTranslation("ChangeKeybindingTooltip");
        SaveBox2.Text = $"{TranslationHelper.GetTranslation("Ctrl")} + S";

        PrintTextBlock.Text = TranslationHelper.GetTranslation("Print");
        PrintBox.ToolTip = TranslationHelper.GetTranslation("ChangeKeybindingTooltip");
        PrintBox2.Text = $"{TranslationHelper.GetTranslation("Ctrl")} + P";

        DeleteFileTextBlock1.Text = DeleteFileTextBlock2.Text =
            TranslationHelper.GetTranslation("DeleteFile");
        DeleteBox1.ToolTip = TranslationHelper.GetTranslation("ChangeKeybindingTooltip");
        DeleteBox2.ToolTip = TranslationHelper.GetTranslation("ChangeKeybindingTooltip");

        RenameFileTextBlock.Text = TranslationHelper.GetTranslation("RenameFile");
        RenameBox1.ToolTip = TranslationHelper.GetTranslation("ChangeKeybindingTooltip");
        RenameBox2.ToolTip = TranslationHelper.GetTranslation("ChangeKeybindingTooltip");

        ShowInFolderTextBlock.Text = TranslationHelper.GetTranslation("ShowInFolder");
        ShowInFolderBox1.ToolTip = TranslationHelper.GetTranslation("ChangeKeybindingTooltip");
        ShowInFolderBox2.ToolTip = TranslationHelper.GetTranslation("ChangeKeybindingTooltip");

        FilePropertiesTextBlock.Text = TranslationHelper.GetTranslation("FileProperties");
        FilePropertiesBox.ToolTip = TranslationHelper.GetTranslation("ChangeKeybindingTooltip");
        FilePropertiesBox2.Text = $"{TranslationHelper.GetTranslation("Ctrl")} + I";

        CopyLabel.Content = TranslationHelper.GetTranslation("Copy");

        CopyFileTextBlock.Text = TranslationHelper.GetTranslation("CopyFile");
        CopyFileBox.ToolTip = TranslationHelper.GetTranslation("ChangeKeybindingTooltip");
        CopyFileBox2.Text = $"{TranslationHelper.GetTranslation("Ctrl")} + C";

        FileCopyPathTextBlock.Text = TranslationHelper.GetTranslation("FileCopyPath");
        CopyFilePathBox.ToolTip = TranslationHelper.GetTranslation("ChangeKeybindingTooltip");
        CopyFilePathBox2.Text =
            $"{TranslationHelper.GetTranslation("Ctrl")} + {TranslationHelper.GetTranslation("Alt")} C";

        CopyImageTextBlock.Text = TranslationHelper.GetTranslation("CopyImage");
        CopyImageBox1.ToolTip = TranslationHelper.GetTranslation("ChangeKeybindingTooltip");
        CopyImageBox2.ToolTip = TranslationHelper.GetTranslation("ChangeKeybindingTooltip");

        Base64TextBlock.Text = $"{TranslationHelper.GetTranslation("Copy")} + Base64";
        CopyBase64Box1.ToolTip = TranslationHelper.GetTranslation("ChangeKeybindingTooltip");
        CopyBase64Box2.ToolTip = TranslationHelper.GetTranslation("ChangeKeybindingTooltip");

        FilePasteTextBlock.Text = TranslationHelper.GetTranslation("FilePaste");
        PasteBox.ToolTip = TranslationHelper.GetTranslation("ChangeKeybindingTooltip");
        CopyBase64Box2.Text = $"{TranslationHelper.GetTranslation("Ctrl")} + V";

        DuplicateFileTextBlock.Text = TranslationHelper.GetTranslation("DuplicateFile");
        DuplicateBox1.ToolTip = TranslationHelper.GetTranslation("ChangeKeybindingTooltip");
        DuplicateBox2.ToolTip = TranslationHelper.GetTranslation("ChangeKeybindingTooltip");

        FileCutTextBlock.Text = TranslationHelper.GetTranslation("FileCut");
        CutBox.ToolTip = TranslationHelper.GetTranslation("ChangeKeybindingTooltip");
        CutBox2.Text = $"{TranslationHelper.GetTranslation("Ctrl")} + X";

        WindowManagementLabel.Content = TranslationHelper.GetTranslation("WindowManagement");

        AboutTextBlock.Text = TranslationHelper.GetTranslation("About");
        AboutBox1.ToolTip = TranslationHelper.GetTranslation("ChangeKeybindingTooltip");
        AboutBox2.ToolTip = TranslationHelper.GetTranslation("ChangeKeybindingTooltip");

        SettingsTextBlock.Text = TranslationHelper.GetTranslation("Settings");
        SettingsBox1.ToolTip = TranslationHelper.GetTranslation("ChangeKeybindingTooltip");
        SettingsBox2.ToolTip = TranslationHelper.GetTranslation("ChangeKeybindingTooltip");

        ImageInfoTextBlock.Text = TranslationHelper.GetTranslation("ImageInfo");
        ImageInfoBox1.ToolTip = TranslationHelper.GetTranslation("ChangeKeybindingTooltip");
        ImageInfoBox2.ToolTip = TranslationHelper.GetTranslation("ChangeKeybindingTooltip");

        ShowResizeWindowTextBlock.Text = TranslationHelper.GetTranslation("ShowResizeWindow");
        ResizeWindowBox1.ToolTip = TranslationHelper.GetTranslation("ChangeKeybindingTooltip");
        ResizeWindowBox2.ToolTip = TranslationHelper.GetTranslation("ChangeKeybindingTooltip");

        CloseTextBlock1.Text = CloseTextBlock2.Text = TranslationHelper.GetTranslation("Close");
        CloseBox1.ToolTip = TranslationHelper.GetTranslation("ChangeKeybindingTooltip");
        CloseBox2.ToolTip = TranslationHelper.GetTranslation("ChangeKeybindingTooltip");
        CloseBox3.Text = TranslationHelper.GetTranslation("Esc");
        CloseBox4.Text = $"{TranslationHelper.GetTranslation("Ctrl")} + Q";

        NewWindowTextBlock.Text = TranslationHelper.GetTranslation("NewWindow");
        NewWindowBox.ToolTip = TranslationHelper.GetTranslation("ChangeKeybindingTooltip");
        NewWindowBox2.Text = $"{TranslationHelper.GetTranslation("Ctrl")} + N";

        CenterWindowTextBlock.Text = TranslationHelper.GetTranslation("CenterWindow");
        CenterWindowBox1.ToolTip = TranslationHelper.GetTranslation("ChangeKeybindingTooltip");
        CenterWindowBox2.ToolTip = TranslationHelper.GetTranslation("ChangeKeybindingTooltip");

        StayTopMostTextBlock.Text = TranslationHelper.GetTranslation("StayTopMost");
        TopMostBox1.ToolTip = TranslationHelper.GetTranslation("ChangeKeybindingTooltip");
        TopMostBox2.ToolTip = TranslationHelper.GetTranslation("ChangeKeybindingTooltip");

        WindowScalingLabel.Content = TranslationHelper.GetTranslation("WindowScaling");

        AutoFitWindowTextBlock.Text = TranslationHelper.GetTranslation("AutoFitWindow");
        AutoFitWindowBox1.ToolTip = TranslationHelper.GetTranslation("ChangeKeybindingTooltip");
        AutoFitWindowBox2.ToolTip = TranslationHelper.GetTranslation("ChangeKeybindingTooltip");

        FillHeightTextBlock.Text = TranslationHelper.GetTranslation("FillHeight");
        AutoFitFillWindowBox1.ToolTip = TranslationHelper.GetTranslation("ChangeKeybindingTooltip");
        AutoFitFillWindowBox2.ToolTip = TranslationHelper.GetTranslation("ChangeKeybindingTooltip");

        NormalWindowTextBlock.Text = TranslationHelper.GetTranslation("NormalWindow");
        NormalWindowBox1.ToolTip = TranslationHelper.GetTranslation("ChangeKeybindingTooltip");
        NormalWindowBox2.ToolTip = TranslationHelper.GetTranslation("ChangeKeybindingTooltip");

        NormalFillTextBlock.Text = TranslationHelper.GetTranslation("FillHeight");
        NormalWindowFillBox1.ToolTip = TranslationHelper.GetTranslation("ChangeKeybindingTooltip");
        NormalWindowFillBox2.ToolTip = TranslationHelper.GetTranslation("ChangeKeybindingTooltip");

        SetStarRatingLabel.Content = TranslationHelper.GetTranslation("SetStarRating");

        SetStar1TextBlock.Text = TranslationHelper.GetTranslation("_1Star");
        SetStar1Box1.ToolTip = TranslationHelper.GetTranslation("ChangeKeybindingTooltip");
        SetStar1Box2.ToolTip = TranslationHelper.GetTranslation("ChangeKeybindingTooltip");

        SetStar2TextBlock.Text = TranslationHelper.GetTranslation("_2Star");
        SetStar2Box1.ToolTip = TranslationHelper.GetTranslation("ChangeKeybindingTooltip");
        SetStar2Box2.ToolTip = TranslationHelper.GetTranslation("ChangeKeybindingTooltip");

        SetStar3TextBlock.Text = TranslationHelper.GetTranslation("_3Star");
        SetStar3Box1.ToolTip = TranslationHelper.GetTranslation("ChangeKeybindingTooltip");
        SetStar3Box2.ToolTip = TranslationHelper.GetTranslation("ChangeKeybindingTooltip");

        SetStar4TextBlock.Text = TranslationHelper.GetTranslation("_4Star");
        SetStar4Box1.ToolTip = TranslationHelper.GetTranslation("ChangeKeybindingTooltip");
        SetStar4Box2.ToolTip = TranslationHelper.GetTranslation("ChangeKeybindingTooltip");

        SetStar5TextBlock.Text = TranslationHelper.GetTranslation("_5Star");
        SetStar5Box1.ToolTip = TranslationHelper.GetTranslation("ChangeKeybindingTooltip");
        SetStar5Box2.ToolTip = TranslationHelper.GetTranslation("ChangeKeybindingTooltip");

        RemoveStarRatingTextBlock.Text = TranslationHelper.GetTranslation("RemoveStarRating");
        ResetStarsBox1.ToolTip = TranslationHelper.GetTranslation("ChangeKeybindingTooltip");
        ResetStarsBox2.ToolTip = TranslationHelper.GetTranslation("ChangeKeybindingTooltip");

        #endregion Translations
    }

    /// <summary>
    /// Associates a key with a function and updates the CustomShortcuts dictionary and keybindings.json file.
    /// </summary>
    /// <param name="sender">The sender object.</param>
    /// <param name="e">The KeyEventArgs containing information about the key event.</param>
    /// <param name="functionName">The name of the function.</param>
    /// <param name="alt">A flag indicating whether it's an alternative key.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task AssociateKey(object sender, KeyEventArgs e, string functionName, bool alt)
    {
        e.Handled = true;

        // Update the text box content
        var textBox = (TextBox)sender;
        var key = e.Key.ToString();
        textBox.Text = key;
        UpdateModifierTextBoxes(functionName, key, alt);
        await Dispatcher.InvokeAsync(Keyboard.ClearFocus);

        var function = await UIHelper.GetFunctionByName(functionName).ConfigureAwait(false);

        // Don't have same key for more than one function
        CustomKeybindings.CustomShortcuts.Remove(e.Key);

        if (e.Key == Key.Escape)
        {
            await Dispatcher.InvokeAsync(() =>
            {
                textBox.Text = string.Empty;
            });

            return;
        }

        // Handle whether it's an alternative key or not
        if (alt)
        {
            if (CustomKeybindings.CustomShortcuts.ContainsValue(function))
            {
                // If the main key is not present, add a new entry with the alternative key
                var altKey = (Key)Enum.Parse(typeof(Key), textBox.Text);
                CustomKeybindings.CustomShortcuts[altKey] = function;
            }
            else
            {
                // Update the key and function name in the CustomShortcuts dictionary
                CustomKeybindings.CustomShortcuts[e.Key] = function;
            }
        }
        else
        {
            // Remove if it already contains
            if (CustomKeybindings.CustomShortcuts.ContainsValue(function))
            {
                Remove();
            }
            CustomKeybindings.CustomShortcuts[e.Key] = function;
        }
        return;

        void Remove()
        {
            var prevKey = CustomKeybindings.CustomShortcuts.FirstOrDefault(x => x.Value == function).Key;
            CustomKeybindings.CustomShortcuts.Remove(prevKey);
        }
    }
}