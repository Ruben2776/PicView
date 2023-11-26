using PicView.FileHandling;
using PicView.UILogic;
using System.IO;
using System.Windows.Input;

namespace PicView.Shortcuts
{
    internal static class CustomKeybindings
    {
        /// <summary>
        /// Dictionary to store custom shortcuts with keys and associated functions.
        /// </summary>
        internal static Dictionary<Key, Func<Task>>? CustomShortcuts;

        /// <summary>
        /// Asynchronously loads custom keybindings from a file.
        /// </summary>
        internal static async Task LoadKeybindings()
        {
            try
            {
                CustomShortcuts = new Dictionary<Key, Func<Task>>();
                var lines = await File.ReadAllLinesAsync("Shortcuts/keybindings.txt").ConfigureAwait(false);
                foreach (var line in lines)
                {
                    var parts = line.Split('=');

                    if (parts.Length != 2)
                    {
                        continue;
                    }

                    // Parse the key and function
                    var key = (Key)Enum.Parse(typeof(Key), parts[0]);
                    var function = GetFunctionByName(parts[1]);
                    // Add to the dictionary
                    CustomShortcuts[key] = await function;
                }
            }
            catch (FileNotFoundException)
            {
                // Create a default keybindings file
                var defaultKeybindings = @"
                    Escape=Close
                    D=Next
                    Right=Next
                    A=Prev
                    Left=Prev
                    W=Up
                    Up=Up
                    S=Down
                    Down=Down
                    PageUp=ScrollUp
                    PageDown=ScrollDown
                    Add=ZoomIn
                    OemPlus=ZoomIn
                    OemMinus=ZoomOut
                    Subtract=ZoomOut
                    Scroll=ToggleScroll
                    Home=ScrollToTop
                    End=ScrollToBottom
                    G=ToggleGallery
                    F=Flip
                    J=ResizeImage
                    C=Crop
                    E=GalleryClick
                    Enter=GalleryClick
                    Delete=DeleteFile
                    I=ImageInfoWindow
                    F6=EffectsWindow
                    F1=AboutWindow
                    F2=Rename
                    F3=OpenInExplorer
                    F5=Slideshow
                    F11=Fullscreen
                    F12=Fullscreen
                    B=ChangeBackground
                    Space=Center
                    D0=Set0Star
                    D1=Set1Star
                    D2=Set2Star
                    D3=Set3Star
                    D4=Set4Star
                    D5=Set5Star";

                // Save the default keybindings to a new file
                await File.WriteAllTextAsync("Shortcuts/keybindings.txt", defaultKeybindings);

                // Reload the keybindings from the newly created file
                await LoadKeybindings().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // Handle other exceptions as needed
                Tooltip.ShowTooltipMessage($"Error loading keybindings: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets the function associated with a given function name.
        /// </summary>
        /// <param name="functionName">Name of the function.</param>
        /// <returns>Task containing the function.</returns>
        private static Task<Func<Task>> GetFunctionByName(string functionName)
        {
            return Task.FromResult<Func<Task>>(functionName switch
            {
                // Navigation values
                "Next" => UIHelper.Next,
                "Prev" => UIHelper.Prev,
                "Up" => UIHelper.Up,
                "Down" => UIHelper.Down,

                // Scroll
                "ScrollUp" => UIHelper.ScrollUp,
                "ScrollDown" => UIHelper.ScrollDown,
                "ScrollToTop" => UIHelper.ScrollToToTop,
                "ScrollToBottom" => UIHelper.ScrollToBottom,

                // Zoom
                "ZoomIn" => UIHelper.ZoomIn,
                "ZoomOut" => UIHelper.ZoomOut,
                "ResetZoom" => UIHelper.ResetZoom,

                // Toggles
                "ToggleScroll" => UIHelper.ToggleScroll,
                "ToggleLooping" => UIHelper.ToggleLooping,
                "ToggleGallery" => UIHelper.ToggleGallery,

                // Scale Window
                "AutoFitWindow" => UIHelper.AutoFitWindow,
                "AutoFitWindowAndStretch" => UIHelper.AutoFitWindowAndStretch,
                "NormalWindow" => UIHelper.NormalWindow,
                "NormalWindowAndStretch" => UIHelper.NormalWindowAndStretch,

                // Window functions
                "Fullscreen" => UIHelper.Fullscreen,
                "TopMost" => UIHelper.SetTopMost,
                "Close" => UIHelper.Close,
                "ToggleInterface" => UIHelper.ToggleInterface,

                // Windows
                "AboutWindow" => UIHelper.AboutWindow,
                "EffectsWindow" => UIHelper.EffectsWindow,
                "ImageInfoWindow" => UIHelper.ImageInfoWindow,
                "ResizeWindow" => UIHelper.ResizeWindow,
                "SettingsWindow" => UIHelper.SettingsWindow,

                // Open functions
                "Open" => OpenSave.OpenAsync,
                "OpenWith" => UIHelper.OpenWith,
                "OpenInExplorer" => UIHelper.OpenInExplorer,
                "Save" => OpenSave.SaveFilesAsync,
                "Reload" => UIHelper.Reload,

                // File functions
                "DeleteFile" => UIHelper.DeleteFile,
                "DuplicateFile" => UIHelper.DuplicateFile,
                "Rename" => UIHelper.Rename,
                "ShowFileProperties" => UIHelper.ShowFileProperties,

                // Image functions
                "ResizeImage" => UIHelper.ResizeImage,
                "Crop" => UIHelper.Crop,
                "Flip" => UIHelper.Flip,
                "OptimizeImage" => UIHelper.OptimizeImage,

                // Set stars
                "Set0Star" => UIHelper.Set0Star,
                "Set1Star" => UIHelper.Set1Star,
                "Set2Star" => UIHelper.Set2Star,
                "Set3Star" => UIHelper.Set3Star,
                "Set4Star" => UIHelper.Set4Star,
                "Set5Star" => UIHelper.Set5Star,

                // Misc
                "ChangeBackground" => UIHelper.ToggleBackground,
                "GalleryClick" => UIHelper.GalleryClick,
                "Center" => UIHelper.Center,
                "Slideshow" => UIHelper.Slideshow,
                "ColorPicker" => UIHelper.ColorPicker,

                _ => null
            });
        }
    }
}