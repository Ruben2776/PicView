using Avalonia.Input;

namespace PicView.Avalonia.Input;

public static class InputHelper
{
    public static async Task OnKeyDownVerifyInput(KeyEventArgs e, Func<bool> focus, Task saveImage)
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
                break; // Allow numbers and basic operations

            case Key.Left:
            case Key.Right:
            case Key.Tab:
            case Key.OemBackTab:
                break; // Allow navigation keys

            case Key.A:
            case Key.C:
            case Key.X:
            case Key.V:
                if (e.KeyModifiers == KeyModifiers.Control)
                {
                    // Allow Ctrl + A, Ctrl + C, Ctrl + X, and Ctrl + V (paste)
                    break;
                }

                e.Handled = true; // Only allow with Ctrl
                return;

            case Key.Oem5: // Key for `%` symbol (may vary based on layout)
                break; // Allow the percentage symbol (%)

            case Key.Escape: // Handle Escape key
                focus();
                e.Handled = true;
                return;

            case Key.Enter: // Handle Enter key for function

                await saveImage.ConfigureAwait(false);
                return;

            default:
                e.Handled = true; // Block all other inputs
                return;
        }
    }
}
