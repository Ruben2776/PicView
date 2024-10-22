using System.Globalization;
using Avalonia.Controls;
using PicView.Avalonia.ViewModels;
using PicView.Core.Extensions;

namespace PicView.Avalonia.Resizing;

public static class AspectRatioHelper
{
    public static void SetAspectRatioForTextBox(TextBox widthTextBox, TextBox heightTextBox, bool isWidth, double aspectRatio, MainViewModel vm)
    {
        var percentage = isWidth ? widthTextBox.Text.GetPercentage() : heightTextBox.Text.GetPercentage();
        if (percentage > 0)
        {
            var newWidth = vm.PixelWidth * (percentage / 100);
            var newHeight = vm.PixelHeight * (percentage / 100);

            widthTextBox.Text = newWidth.ToString("# ", CultureInfo.CurrentCulture);
            heightTextBox.Text = newHeight.ToString("# ", CultureInfo.CurrentCulture);
            
            if (isWidth)
            {
                heightTextBox.Text = newHeight.ToString(CultureInfo.CurrentCulture);
            }
            else
            {
                widthTextBox.Text = newWidth.ToString(CultureInfo.CurrentCulture);
            }
        }
        else
        {
            if (!uint.TryParse(widthTextBox.Text, out var width) || !uint.TryParse(heightTextBox.Text, out var height))
            {
                // Invalid input, delete last character
                try
                {
                    if (isWidth && widthTextBox.Text.Length > 1)
                    {
                        widthTextBox.Text = widthTextBox.Text[..^1];
                    }
                    else if (heightTextBox.Text.Length > 1)
                    {
                        heightTextBox.Text = heightTextBox.Text[..^1];
                    }
                    
                }
                catch (Exception e)
                {
#if DEBUG
                    Console.WriteLine(e);
#endif
                }
                return;
            }
            if (isWidth)
            {
                var newHeight = Math.Round(width / aspectRatio);
                heightTextBox.Text = newHeight.ToString(CultureInfo.CurrentCulture);
            }
            else
            {
                var newWidth = Math.Round(height * aspectRatio);
                widthTextBox.Text = newWidth.ToString(CultureInfo.CurrentCulture);
            }
        }
    }
}
