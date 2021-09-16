using PicView.FileHandling;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using static PicView.ChangeImage.Navigation;

namespace PicView.UILogic
{
    internal static class EditTitleBar
    {
        private static string backupTitle;

        internal static void Bar_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (ConfigureWindows.GetMainWindow.TitleText.IsFocused)
            {
                return;
            }

            ConfigureWindows.Move(sender, e);
            Refocus();
            e.Handled = true; // Disable text clicking
        }

        internal static void Bar_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (Pics == null || !Properties.Settings.Default.ShowInterface || Pics.Count == 0)
            {
                e.Handled = true; // Disable text clicking
            }
        }

        internal static void EditTitleBar_Text()
        {
            if (Pics == null || !Properties.Settings.Default.ShowInterface || Pics.Count == 0)
            {
                return;
            }
            if (!ConfigureWindows.GetMainWindow.TitleText.IsFocused)
            {
                ConfigureWindows.GetMainWindow.TitleText.InnerTextBox.Focus();
                SelectFileName();
            }
            else
            {
                Refocus();
            }
        }

        /// <summary>
        /// Set the text to the filename
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal static void EditTitleBar_Text(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (Pics == null || !Properties.Settings.Default.ShowInterface || Pics.Count == 0)
            {
                return;
            }

            e.Handled = true;

            backupTitle = ConfigureWindows.GetMainWindow.TitleText.Text;
            ConfigureWindows.GetMainWindow.TitleText.Text = Pics[FolderIndex];
        }

        internal static void SelectFileName()
        {
            var filename = Path.GetFileName(Pics[FolderIndex]);
            var start = Pics[FolderIndex].Length - filename.Length;
            var end = Path.GetFileNameWithoutExtension(filename).Length;
            ConfigureWindows.GetMainWindow.TitleText.InnerTextBox.Select(start, end);
        }

        internal static async Task HandleRename()
        {
            if (FileFunctions.RenameFile(Pics[FolderIndex], ConfigureWindows.GetMainWindow.TitleText.Text))
            {
                // Check if the file is not in the same folder
                if (Path.GetDirectoryName(ConfigureWindows.GetMainWindow.TitleText.Text) != Path.GetDirectoryName(Pics[FolderIndex]))
                {
                    Pics.Remove(Pics[FolderIndex]);
                    if (Pics.Count > 0)
                    {
                        await PicAsync().ConfigureAwait(false);
                    }
                    else
                    {
                        await LoadPiFromFileAsync(ConfigureWindows.GetMainWindow.TitleText.Text).ConfigureAwait(false);
                    }
                }
                else
                {
                    var text = ConfigureWindows.GetMainWindow.TitleText.Text;
                    var width = ConfigureWindows.GetMainWindow.MainImage.Source.Width;
                    var height = ConfigureWindows.GetMainWindow.MainImage.Source.Height;
                    await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, (Action)(() =>
                    {
                        Pics[FolderIndex] = text;
                        SetTitle.SetTitleString((int)width, (int)height, FolderIndex);
                    }));
                }
                await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, (Action)(() =>
                {
                    Refocus(false);
                }));

            }
            else
            {
                await Tooltip.ShowTooltipMessage(Application.Current.Resources["AnErrorOccuredMovingFile"]).ConfigureAwait(false);
                await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, (Action)(() =>
                {
                    Refocus();
                }));
            }
        }

        /// <summary>
        /// Removes focus from titlebar and sets it to the mainwindow
        /// </summary>
        internal static void Refocus(bool backup = true)
        {
            if (!ConfigureWindows.GetMainWindow.TitleText.IsFocused)
            {
                return;
            }

            FocusManager.SetFocusedElement(FocusManager.GetFocusScope(ConfigureWindows.GetMainWindow.TitleText), null);
            Keyboard.ClearFocus();
            ConfigureWindows.GetMainWindow.Focus();

            if (backup)
            {
                ConfigureWindows.GetMainWindow.TitleText.Text = backupTitle;
                backupTitle = string.Empty;
            }
        }
    }
}