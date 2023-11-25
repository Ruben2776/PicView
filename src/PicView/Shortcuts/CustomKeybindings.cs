using PicView.FileHandling;
using PicView.UILogic;
using System.IO;
using System.Windows.Input;

namespace PicView.Shortcuts
{
    internal static class CustomKeybindings
    {
        internal static Dictionary<Key, Func<Task>>? CustomShortcuts;

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
                // Handle file not found exception, create a default keybindings file, or take appropriate action
            }
            catch (Exception ex)
            {
                // Handle other exceptions as needed
                Tooltip.ShowTooltipMessage($"Error loading keybindings: {ex.Message}");
            }
        }

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

                _ => null
            });
        }
    }
}