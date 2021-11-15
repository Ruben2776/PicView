using PicView.ImageHandling;
using PicView.UILogic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PicView.Shortcuts
{
    internal static class QuickResizeShortcuts
    {
        internal static async Task QuickResizePreviewKeys(TextBox box, KeyEventArgs e, string width, string height)
        {
            switch (e.Key)
            {
                case Key.D0:
                case Key.D1:
                case Key.D2:
                case Key.D3:
                case Key.D4:
                case Key.D5:
                case Key.D6:
                case Key.D7:
                case Key.D8:
                case Key.D9:
                case Key.NumPad0:
                case Key.NumPad1:
                case Key.NumPad2:
                case Key.NumPad3:
                case Key.NumPad4:
                case Key.NumPad5:
                case Key.NumPad6:
                case Key.NumPad7:
                case Key.NumPad8:
                case Key.NumPad9:
                case Key.Back:
                case Key.Delete:
                case Key.Left:
                case Key.Right:
                case Key.Tab:
                case Key.OemBackTab:
                    break;  // Allow these keys
                case Key.A:
                case Key.C:
                case Key.X:
                    if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                    {
                        // Allow Ctrl + A, Ctrl + C, Ctrl + X
                        break;
                    }
                    else
                    {
                        e.Handled = true;// only allowed on ctrl
                        return;
                    }
                case Key.Escape: // Escape logic
                    UC.GetQuickResize.Hide();
                    e.Handled = true;
                    break;

                case Key.Enter: // Execute it!
                    await Fire(e, width, height).ConfigureAwait(false);
                    break;

                default:
                    e.Handled = true; // Don't allow other keys
                    break;
            }
        }

        internal static async Task Fire(KeyEventArgs? e, string width, string height)
        {
            var resize = await FireResizeAsync(e, width, height).ConfigureAwait(false);
            if (resize)
            {
                UC.GetQuickResize.Hide();
            }
        }

        static async Task<bool> FireResizeAsync(KeyEventArgs? e, string widthText, string heightText)
        {
            string file = ChangeImage.Navigation.Pics[ChangeImage.Navigation.FolderIndex];
            if (int.TryParse(widthText, out var width) && int.TryParse(heightText, out var height))
            {
                var resize = await ImageSizeFunctions.ResizeImageAsync(file, width, height, 0).ConfigureAwait(false);
                if (resize)
                {
                    await ChangeImage.Error_Handling.ReloadAsync().ConfigureAwait(false);
                }
                else
                {
                    UILogic.Tooltip.ShowTooltipMessage(Application.Current.Resources["UnexpectedError"]);
                }
            }
            else // handle if it contains percentage
            {
                var tryWidth = await FirePercentageAsync(widthText, file).ConfigureAwait(false);
                if (tryWidth)
                {
                    await ChangeImage.Error_Handling.ReloadAsync().ConfigureAwait(false);
                    return false;
                }
                var tryHeight = await FirePercentageAsync(heightText, file).ConfigureAwait(false);
                if (tryHeight)
                {
                    await ChangeImage.Error_Handling.ReloadAsync().ConfigureAwait(false);
                }
                else
                {
                    UILogic.Tooltip.ShowTooltipMessage(Application.Current.Resources["UnexpectedError"]);
                }
            }
            return true;
        }

        static async Task<bool> FirePercentageAsync(string text, string file)
        {
            foreach (Match match in Regex.Matches(text, @"(\d+)%")) // Find % sign
            {
                if (match.Success)
                {
                    if (double.TryParse(match.Groups[1].Value, out double percentage))
                    {
                        var resize = await ImageSizeFunctions.ResizeImageAsync(file, 0, 0, 0, new ImageMagick.Percentage(percentage)).ConfigureAwait(false);
                        if (resize)
                        {
                            await ChangeImage.Error_Handling.ReloadAsync().ConfigureAwait(false);
                        }
                    }
                    return true;
                }
            }
            return false;
        }
    }
}