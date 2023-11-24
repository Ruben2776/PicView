using PicView.ConfigureSettings;
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
                "Close" => delegate
                {
                    UIHelper.Close();
                    return Task.CompletedTask;
                }
                ,

                // Navigation values
                "Next" => ChangeImage.Navigation.Next,
                "Prev" => ChangeImage.Navigation.Prev,
                "ScrollUp" => delegate
                {
                    UIHelper.ScrollUp();
                    return Task.CompletedTask;
                }
                ,
                "ScrollDown" => delegate
                {
                    UIHelper.ScrollDown();
                    return Task.CompletedTask;
                }
                ,

                // Toggles
                "ToggleScroll" => delegate
                {
                    UpdateUIValues.SetScrolling();
                    return Task.CompletedTask;
                }
                ,

                // Windows
                "EffectsWindow" => delegate
                {
                    ConfigureWindows.EffectsWindow();
                    return Task.CompletedTask;
                }
                ,
                "InfoWindow" => delegate
                {
                    ConfigureWindows.InfoWindow();
                    return Task.CompletedTask;
                }

                ,
                "ChangeBackground" => delegate
                {
                    ConfigColors.ChangeBackground();
                    return Task.CompletedTask;
                }
                ,
                "Save" => OpenSave.SaveFilesAsync,

                _ => null
            });
        }
    }
}