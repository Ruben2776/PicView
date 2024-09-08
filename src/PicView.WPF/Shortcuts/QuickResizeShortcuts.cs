﻿using ImageMagick;
using PicView.Core.Localization;
using PicView.WPF.ChangeImage;
using PicView.WPF.UILogic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using System.Windows.Input;
using PicView.Core.ImageDecoding;

namespace PicView.WPF.Shortcuts;

internal static partial class QuickResizeShortcuts
{
    internal static async Task QuickResizePreviewKeys(KeyEventArgs e, string width, string height)
    {
        // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
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
                break;

            case Key.Left:
            case Key.Right:
            case Key.Tab:
            case Key.OemBackTab:
                break; // Allow these keys
            case Key.A:
            case Key.C:
            case Key.X:
                if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                {
                    // Allow Ctrl + A, Ctrl + C, Ctrl + X
                    break;
                }

                e.Handled = true; // only allowed on ctrl
                return;

            case Key.Escape: // Escape logic
                UC.GetQuickResize.Hide();
                e.Handled = true;
                break;

            case Key.Enter: // Execute it!
                await Fire(width, height).ConfigureAwait(false);
                break;

            default:
                e.Handled = true; // Don't allow other keys
                break;
        }
    }

    internal static void QuickResizeAspectRatio(TextBox widthBox, TextBox heightBox, bool widthBoxChanged,
        KeyEventArgs? e, double percentageValue = 0)
    {
        if (widthBox == null || heightBox == null)
        {
            return;
        }

        if (e?.Key == Key.Tab)
        {
            return;
        } // Fixes weird bug

        var originalWidth = ConfigureWindows.GetMainWindow.MainImage.Source.Width;
        var originalHeight = ConfigureWindows.GetMainWindow.MainImage.Source.Height;

        var aspectRatio = originalWidth / originalHeight;

        if (aspectRatio <= 0)
        {
            return;
        }

        if (percentageValue <= 0 && int.TryParse(widthBox.Text, out var width) &&
            int.TryParse(heightBox.Text, out var height))
        {
            if (widthBoxChanged)
            {
                var newHeight = Math.Round(width / aspectRatio);
                heightBox.Text = newHeight.ToString(CultureInfo.CurrentCulture);
            }
            else
            {
                var newWidth = Math.Round(height / aspectRatio);
                widthBox.Text = newWidth.ToString(CultureInfo.CurrentCulture);
            }
        }
        else
        {
            if (e?.Key == Key.Back || e?.Key == Key.Delete)
            {
                return;
            }

            if (percentageValue > 0)
            {
                var newWidth = originalWidth * percentageValue;
                var newHeight = originalHeight * percentageValue;

                widthBox.Text = newWidth.ToString("# ", CultureInfo.CurrentCulture);
                heightBox.Text = newHeight.ToString("# ", CultureInfo.CurrentCulture);
                return;
            }

            // Determine if % character
            var text = widthBoxChanged ? widthBox.Text : heightBox.Text;
            percentageValue = ReturnPercentageFromString(text);
            if (percentageValue <= 0)
            {
                return;
            }

            if (widthBoxChanged && ReturnPercentageFromString(widthBox.Text) > 0)
            {
                heightBox.Text = widthBox.Text;
            }
            else if (ReturnPercentageFromString(heightBox.Text) > 0)
            {
                widthBox.Text = heightBox.Text;
            }
        }
    }

    internal static async Task Fire(string width, string height)
    {
        if (UC.GetQuickResize is not null)
        {
            UC.GetQuickResize.Hide();
        }
        try
        {
            var resize = await FireResizeAsync(width, height).ConfigureAwait(false);
            if (!resize)
            {
                Tooltip.ShowTooltipMessage(TranslationHelper.GetTranslation("UnexpectedError"));
            }
        }
        catch (Exception ex)
        {
#if DEBUG
            Trace.WriteLine($"{nameof(QuickResizeShortcuts)} {nameof(Fire)} exception:\n{ex.Message}");
#endif
            Tooltip.ShowTooltipMessage(ex.Message, true, TimeSpan.FromSeconds(5));
        }
    }

    private static async Task<bool> FireResizeAsync(string widthText, string heightText)
    {
        var fileInfo = PreLoader.Get(Navigation.FolderIndex)?.FileInfo;
        fileInfo ??= new FileInfo(Navigation.Pics[Navigation.FolderIndex]);
        PreLoader.Remove(Navigation.FolderIndex);
        if (uint.TryParse(widthText, out var width) && uint.TryParse(heightText, out var height))
        {
            var resize = await SaveImageFileHelper.ResizeImageAsync(fileInfo, width, height, 0)
                .ConfigureAwait(false);
            if (resize)
            {
                await LoadPic.LoadPicAtIndexAsync(Navigation.FolderIndex).ConfigureAwait(false);
            }
            else
            {
                Tooltip.ShowTooltipMessage(TranslationHelper.GetTranslation("UnexpectedError"));
                return false;
            }
        }
        else // handle if it contains percentage
        {
            var tryWidth = await FirePercentageAsync(widthText, fileInfo).ConfigureAwait(false);
            if (tryWidth)
            {
                await LoadPic.LoadPicAtIndexAsync(Navigation.FolderIndex).ConfigureAwait(false);
            }
            else
            {
                var tryHeight = await FirePercentageAsync(heightText, fileInfo).ConfigureAwait(false);
                if (tryHeight)
                {
                    await LoadPic.LoadPicAtIndexAsync(Navigation.FolderIndex).ConfigureAwait(false);
                }
                else
                {
                    Tooltip.ShowTooltipMessage(TranslationHelper.GetTranslation("UnexpectedError"));
                    return false;
                }
            }
        }

        return true;
    }

    private static async Task<bool> FirePercentageAsync(string text, FileInfo fileInfo)
    {
        var percentage = ReturnPercentageFromString(text);
        if (!(percentage > 0))
        {
            return false;
        }

        var resize = await SaveImageFileHelper.ResizeImageAsync(fileInfo, 0, 0, 0, new Percentage(percentage))
            .ConfigureAwait(false);
        if (resize)
        {
            await ErrorHandling.ReloadAsync().ConfigureAwait(false);
        }

        return true;
    }

    private static double ReturnPercentageFromString(string text)
    {
        foreach (Match match in MyRegex().Matches(text)) // Find % sign
        {
            if (!match.Success)
            {
                continue;
            }

            if (double.TryParse(match.Groups[1].Value, out var percentage))
            {
                return percentage;
            }
        }

        return 0;
    }

    [GeneratedRegex("(\\d+)%")]
    private static partial Regex MyRegex();
}