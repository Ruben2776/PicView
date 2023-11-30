using System.IO;
using PicView.Shortcuts;
using System.Text.Json;
using System.Windows.Controls;
using System.Windows.Input;
using PicView.UILogic;

namespace PicView.Views.UserControls.Misc;

public partial class ShortcutList
{
    public ShortcutList()
    {
        InitializeComponent();

        // Subscribe to the PreviewKeyDown event for both textboxes
        NextBox1.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "Next", false);
        NextBox2.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "Next", true);

        AutoFitWindowBox1.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "AutoFitWindow", false);
        AutoFitWindowBox2.PreviewKeyDown += async (s, e) => await AssociateKey(s, e, "AutoFitWindow", true);
    }

    private async Task AssociateKey(object sender, KeyEventArgs e, string functionName, bool alt)
    {
        e.Handled = true;

        // Update the text box content
        var textBox = (TextBox)sender;
        textBox.Text = e.Key.ToString();
        await Dispatcher.InvokeAsync(Keyboard.ClearFocus);

        var function = CustomKeybindings.GetFunctionByName(functionName);

        // Handle whether it's an alternative key or not
        if (alt)
        {
            // Check if the main key is already in the dictionary
            if (CustomKeybindings.CustomShortcuts.ContainsKey(e.Key))
            {
                if (CustomKeybindings.CustomShortcuts.ContainsValue(function.Result))
                {
                    var prevKey = CustomKeybindings.CustomShortcuts.FirstOrDefault(x => x.Value == function.Result).Key;
                    CustomKeybindings.CustomShortcuts.Remove(prevKey);
                }
                // Add the alternative key to the dictionary
                CustomKeybindings.CustomShortcuts[e.Key] = await function.ConfigureAwait(false);
            }
            else if (CustomKeybindings.CustomShortcuts.ContainsValue(function.Result))
            {
                // If the main key is not present, add a new entry with the alternative key
                var altKey = (Key)Enum.Parse(typeof(Key), textBox.Text);
                CustomKeybindings.CustomShortcuts[altKey] = await function.ConfigureAwait(false);
            }
            else
            {
                // Update the key and function name in the CustomShortcuts dictionary
                CustomKeybindings.CustomShortcuts[e.Key] = await function.ConfigureAwait(false);
            }
        }
        else
        {
            // Update the key and function name in the CustomShortcuts dictionary
            CustomKeybindings.CustomShortcuts[e.Key] = await function.ConfigureAwait(false);
        }

        // Update the keybindings.json file
        await CustomKeybindings.UpdateKeyBindingsFile().ConfigureAwait(false);
    }
}