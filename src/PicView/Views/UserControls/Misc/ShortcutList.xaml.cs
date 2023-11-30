using PicView.Shortcuts;
using System.Windows.Controls;
using System.Windows.Input;

namespace PicView.Views.UserControls.Misc;

public partial class ShortcutList
{
    public ShortcutList()
    {
        InitializeComponent();

        // NextBox
        NextBox1.Loaded += (s, _) => UpdateTextBoxes(s, "Next", false);
        NextBox2.Loaded += (s, _) => UpdateTextBoxes(s, "Next", true);

        NextBox1.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "Next", false);
        NextBox2.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "Next", true);

        // PrevBox
        PrevBox1.Loaded += (s, _) => UpdateTextBoxes(s, "Prev", false);
        PrevBox2.Loaded += (s, _) => UpdateTextBoxes(s, "Prev", true);

        PrevBox1.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "Prev", false);
        PrevBox2.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "Prev", true);

        // UpBox || Rotate right
        UpBox1.Loaded += (s, _) => UpdateTextBoxes(s, "Up", false);
        UpBox2.Loaded += (s, _) => UpdateTextBoxes(s, "Up", true);

        UpBox1.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "Up", false);
        UpBox2.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "Up", true);

        // DownBox || Rotate left
        DownBox1.Loaded += (s, _) => UpdateTextBoxes(s, "Down", false);
        DownBox2.Loaded += (s, _) => UpdateTextBoxes(s, "Down", true);

        DownBox1.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "Down", false);
        DownBox2.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "Down", true);

        // PgUpBox
        PgUpBox1.Loaded += (s, _) => UpdateTextBoxes(s, "ScrollUp", false);
        PgUpBox2.Loaded += (s, _) => UpdateTextBoxes(s, "ScrollUp", true);

        PgUpBox1.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "ScrollUp", false);
        PgUpBox2.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "ScrollUp", true);

        // ScrlTopBox
        ScrlTopBox1.Loaded += (s, _) => UpdateTextBoxes(s, "ScrollToTop", false);
        ScrlTopBox2.Loaded += (s, _) => UpdateTextBoxes(s, "ScrollToTop", true);

        ScrlTopBox1.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "ScrollToTop", false);
        ScrlTopBox2.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "ScrollToTop", true);

        // PgDownBox
        PgDownBox1.Loaded += (s, _) => UpdateTextBoxes(s, "ScrollDown", false);
        PgDownBox2.Loaded += (s, _) => UpdateTextBoxes(s, "ScrollDown", true);

        PgUpBox1.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "ScrollDown", false);
        PgUpBox2.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "ScrollDown", true);

        // ScrlBottomBox
        ScrlBottomBox1.Loaded += (s, _) => UpdateTextBoxes(s, "ScrollToBottom", false);
        ScrlBottomBox2.Loaded += (s, _) => UpdateTextBoxes(s, "ScrollToBottom", true);

        ScrlBottomBox1.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "ScrollToBottom", false);
        ScrlBottomBox2.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "ScrollToBottom", true);

        // Toggle Scroll
        ToggleScrlBox1.Loaded += (s, _) => UpdateTextBoxes(s, "ToggleScroll", false);
        ToggleScrlBox2.Loaded += (s, _) => UpdateTextBoxes(s, "ToggleScroll", true);

        ToggleScrlBox1.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "ToggleScroll", false);
        ToggleScrlBox2.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "ToggleScroll", true);

        // Zoom In
        ZoomInBox.Loaded += (s, _) => UpdateTextBoxes(s, "ZoomIn", false);

        ZoomInBox.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "ZoomIn", false);

        // Zoom Out
        ZoomOutBox.Loaded += (s, _) => UpdateTextBoxes(s, "ZoomOut", false);

        ZoomOutBox.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "ZoomOut", false);

        // Reset zoom
        ResetZoomBox.Loaded += (s, _) => UpdateTextBoxes(s, "ResetZoom", false);

        ResetZoomBox.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "ResetZoom", false);

        // Flip
        FlipBox1.Loaded += (s, _) => UpdateTextBoxes(s, "Flip", false);
        FlipBox2.Loaded += (s, _) => UpdateTextBoxes(s, "Flip", true);

        FlipBox1.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "Flip", false);
        FlipBox2.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "Flip", true);

        // Crop
        CropBox1.Loaded += (s, _) => UpdateTextBoxes(s, "Crop", false);
        CropBox2.Loaded += (s, _) => UpdateTextBoxes(s, "Crop", true);

        CropBox1.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "Crop", false);
        CropBox2.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "Crop", true);

        // Change Background
        CropBox1.Loaded += (s, _) => UpdateTextBoxes(s, "ToggleBackground", false);
        CropBox2.Loaded += (s, _) => UpdateTextBoxes(s, "ToggleBackground", true);

        CropBox1.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "ToggleBackground", false);
        CropBox2.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "ToggleBackground", true);

        // Color Picker
        ColorPickBox1.Loaded += (s, _) => UpdateTextBoxes(s, "ColorPicker", false);
        ColorPickBox2.Loaded += (s, _) => UpdateTextBoxes(s, "ColorPicker", true);

        ColorPickBox1.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "ColorPicker", false);
        ColorPickBox2.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "ColorPicker", true);

        // Toggle UI
        ToggleUIBox.Loaded += (s, _) => UpdateTextBoxes(s, "ToggleInterface", false);

        ToggleUIBox.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "ToggleInterface", false);

        // Toggle Fullscreen
        SlideshowBox1.Loaded += (s, _) => UpdateTextBoxes(s, "Fullscreen", false);

        ToggleFullscreenBox.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "Fullscreen", false);

        // Slideshow
        SlideshowBox1.Loaded += (s, _) => UpdateTextBoxes(s, "Slideshow", false);
        SlideshowBox2.Loaded += (s, _) => UpdateTextBoxes(s, "Slideshow", true);

        SlideshowBox1.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "Slideshow", false);
        SlideshowBox2.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "Slideshow", true);

        // Show Image Gallery
        ShowImageGalleryBox1.Loaded += (s, _) => UpdateTextBoxes(s, "ToggleGallery", false);
        ShowImageGalleryBox2.Loaded += (s, _) => UpdateTextBoxes(s, "ToggleGallery", true);

        ShowImageGalleryBox1.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "ToggleGallery", false);
        ShowImageGalleryBox2.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "ToggleGallery", true);

        // Open
        OpenBox.Loaded += (s, _) => UpdateTextBoxes(s, "Open", false);

        OpenBox.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "Open", false);

        // Save
        SaveBox.Loaded += (s, _) => UpdateTextBoxes(s, "Save", false);

        SaveBox.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "Save", false);

        // Print
        PrintBox.Loaded += (s, _) => UpdateTextBoxes(s, "Print", false);

        PrintBox.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "Print", false);

        // Delete File
        DeleteBox1.Loaded += (s, _) => UpdateTextBoxes(s, "DeleteFile", false);
        DeleteBox2.Loaded += (s, _) => UpdateTextBoxes(s, "DeleteFile", true);

        DeleteBox1.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "DeleteFile", false);
        DeleteBox2.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "DeleteFile", true);

        // Rename File
        RenameBox1.Loaded += (s, _) => UpdateTextBoxes(s, "Rename", false);
        RenameBox2.Loaded += (s, _) => UpdateTextBoxes(s, "Rename", true);

        RenameBox1.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "Rename", false);
        RenameBox2.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "Rename", true);

        // Show in folder
        ShowInFolderBox1.Loaded += (s, _) => UpdateTextBoxes(s, "OpenInExplorer", false);
        ShowInFolderBox2.Loaded += (s, _) => UpdateTextBoxes(s, "OpenInExplorer", true);

        ShowInFolderBox1.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "OpenInExplorer", false);
        ShowInFolderBox2.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "OpenInExplorer", true);

        // File properties
        FilePropertiesBox.Loaded += (s, _) => UpdateTextBoxes(s, "ShowFileProperties", false);

        FilePropertiesBox.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "ShowFileProperties", false);

        // Copy file
        CopyFileBox.Loaded += (s, _) => UpdateTextBoxes(s, "CopyFile", false);

        CopyFileBox.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "CopyFile", false);

        // Copy file path
        CopyFilePathBox.Loaded += (s, _) => UpdateTextBoxes(s, "CopyFilePath", false);

        CopyFilePathBox.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "CopyFilePath", false);

        // Copy image
        CopyImageBox.Loaded += (s, _) => UpdateTextBoxes(s, "CopyImage", false);

        CopyImageBox.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "CopyImage", false);

        // Copy base64
        CopyBase64Box1.Loaded += (s, _) => UpdateTextBoxes(s, "CopyBase64", false);
        CopyBase64Box2.Loaded += (s, _) => UpdateTextBoxes(s, "CopyBase64", true);

        CopyBase64Box1.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "CopyBase64", false);
        CopyBase64Box2.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "CopyBase64", true);

        // Cut file
        CopyImageBox.Loaded += (s, _) => UpdateTextBoxes(s, "CutFile", false);

        CopyImageBox.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "CutFile", false);

        // About window
        AboutBox1.Loaded += (s, _) => UpdateTextBoxes(s, "AboutWindow", false);
        AboutBox2.Loaded += (s, _) => UpdateTextBoxes(s, "AboutWindow", false);

        AboutBox1.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "AboutWindow", false);
        AboutBox2.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "AboutWindow", true);

        // Settings window
        SettingsBox1.Loaded += (s, _) => UpdateTextBoxes(s, "SettingsWindow", false);
        SettingsBox2.Loaded += (s, _) => UpdateTextBoxes(s, "SettingsWindow", false);

        SettingsBox1.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "SettingsWindow", false);
        SettingsBox2.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "SettingsWindow", true);

        // Image info window
        ImageInfoBox1.Loaded += (s, _) => UpdateTextBoxes(s, "ImageInfoWindow", false);
        ImageInfoBox2.Loaded += (s, _) => UpdateTextBoxes(s, "ImageInfoWindow", false);

        ImageInfoBox1.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "ImageInfoWindow", false);
        ImageInfoBox2.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "ImageInfoWindow", true);

        // Resize Window
        ResizeWindowBox1.Loaded += (s, _) => UpdateTextBoxes(s, "ImageInfoWindow", false);
        ResizeWindowBox2.Loaded += (s, _) => UpdateTextBoxes(s, "ImageInfoWindow", false);

        ImageInfoBox1.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "ImageInfoWindow", false);
        ImageInfoBox2.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "ImageInfoWindow", true);

        // Close
        CloseBox.Loaded += (s, _) => UpdateTextBoxes(s, "Close", false);

        CloseBox.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "Close", false);

        // Center window
        CenterWindowBox1.Loaded += (s, _) => UpdateTextBoxes(s, "Center", false);
        CenterWindowBox2.Loaded += (s, _) => UpdateTextBoxes(s, "Center", true);

        CenterWindowBox1.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "Center", false);
        CenterWindowBox2.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "Center", true);

        // Topmost
        TopMostBox1.Loaded += (s, _) => UpdateTextBoxes(s, "TopMost", false);
        TopMostBox2.Loaded += (s, _) => UpdateTextBoxes(s, "TopMost", true);

        TopMostBox1.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "TopMost", false);
        TopMostBox2.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "TopMost", true);

        // Auto Fit Window
        AutoFitWindowBox1.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "AutoFitWindow", false);
        AutoFitWindowBox2.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "AutoFitWindow", true);

        AutoFitWindowBox1.Loaded += (s, _) => UpdateTextBoxes(s, "AutoFitWindow", false);
        AutoFitWindowBox1.Loaded += (s, _) => UpdateTextBoxes(s, "AutoFitWindow", true);

        // Auto Fit Window fill
        AutoFitFillWindowBox1.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "AutoFitWindowAndStretch", false);
        AutoFitFillWindowBox2.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "AutoFitWindowAndStretch", true);

        AutoFitFillWindowBox1.Loaded += (s, _) => UpdateTextBoxes(s, "AutoFitWindowAndStretch", false);
        AutoFitFillWindowBox2.Loaded += (s, _) => UpdateTextBoxes(s, "AutoFitWindowAndStretch", true);

        // Normal window
        NormalWindowBox1.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "NormalWindow", false);
        NormalWindowBox2.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "NormalWindow", true);

        NormalWindowBox1.Loaded += (s, _) => UpdateTextBoxes(s, "NormalWindow", false);
        NormalWindowBox2.Loaded += (s, _) => UpdateTextBoxes(s, "NormalWindow", true);

        // Normal window fill
        NormalWindowFillBox1.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "NormalWindowAndStretch", false);
        NormalWindowFillBox2.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "NormalWindowAndStretch", true);

        NormalWindowFillBox1.Loaded += (s, _) => UpdateTextBoxes(s, "NormalWindowAndStretch", false);
        NormalWindowFillBox2.Loaded += (s, _) => UpdateTextBoxes(s, "NormalWindowAndStretch", true);

        // Open with...
        OpenWithBox.Loaded += (s, _) => UpdateTextBoxes(s, "OpenWith", false);

        OpenWithBox.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "OpenWith", false);

        // Reload
        OpenWithBox.Loaded += (s, _) => UpdateTextBoxes(s, "OpenWith", false);

        OpenWithBox.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "OpenWith", false);
    }

    /// <summary>
    /// Updates the TextBoxes based on the keys associated with the specified function.
    /// </summary>
    /// <param name="sender">The sender object.</param>
    /// <param name="functionName">The name of the function.</param>
    /// <param name="alt">A flag indicating whether it's an alternative key.</param>
    private void UpdateTextBoxes(object sender, string functionName, bool alt)
    {
        var textBox = (TextBox)sender;
        var function = CustomKeybindings.GetFunctionByName(functionName).Result;

        // Find the key associated with the specified function
        var keys = CustomKeybindings.CustomShortcuts.Where(x => x.Value == function).Select(x => x.Key).ToList();

        if (keys.Count > 0)
        {
            if (keys.Count == 1)
            {
                // Update the TextBox based on alt
                textBox.Text = alt ? string.Empty : keys.FirstOrDefault().ToString();
            }
            else
            {
                textBox.Text = alt ? keys.LastOrDefault().ToString() : keys.FirstOrDefault().ToString();
            }
        }
        else
        {
            // Handle the case where the function is not found in the CustomShortcuts dictionary
            textBox.Text = string.Empty;
        }
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
        textBox.Text = e.Key.ToString();
        await Dispatcher.InvokeAsync(Keyboard.ClearFocus);

        var function = await CustomKeybindings.GetFunctionByName(functionName).ConfigureAwait(false);

        // Handle whether it's an alternative key or not
        if (alt)
        {
            // Check if the main key is already in the dictionary
            if (CustomKeybindings.CustomShortcuts.ContainsKey(e.Key))
            {
                if (CustomKeybindings.CustomShortcuts.ContainsValue(function))
                {
                    var prevKey = CustomKeybindings.CustomShortcuts.FirstOrDefault(x => x.Value == function).Key;
                    CustomKeybindings.CustomShortcuts.Remove(prevKey);
                }
                // Add the alternative key to the dictionary
                CustomKeybindings.CustomShortcuts[e.Key] = function;
            }
            else if (CustomKeybindings.CustomShortcuts.ContainsValue(function))
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
            // Update the key and function name in the CustomShortcuts dictionary
            CustomKeybindings.CustomShortcuts[e.Key] = function;
        }

        // Update the keybindings.json file
        await CustomKeybindings.UpdateKeyBindingsFile().ConfigureAwait(false);
    }
}