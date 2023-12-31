using PicView.Core.Localization;
using PicView.WPF.ChangeImage;
using PicView.WPF.ConfigureSettings;
using PicView.WPF.PicGallery;
using PicView.WPF.UILogic;
using System.Globalization;
using System.Windows.Input;
using System.Windows.Media;
using static PicView.WPF.Animations.MouseOverAnimations;
using static PicView.WPF.ChangeImage.Navigation;
using static PicView.WPF.UILogic.UC;

namespace PicView.WPF.Views.UserControls.Buttons;

public partial class GoToPicButton
{
    public GoToPicButton()
    {
        InitializeComponent();

        Loaded += delegate
        {
            TheButton.Click += async (_, _) => await GoToPicEventAsync().ConfigureAwait(false);
            SetButtonIconMouseOverAnimations(TheButton, GoToPicBrush, (SolidColorBrush)Resources["PlayIconBrush"]);

            GoToPicBox.PreviewMouseLeftButtonDown += delegate
            {
                GoToPicBox.CaretBrush = new SolidColorBrush(ConfigColors.MainColor);
            };
            GoToPicBox.PreviewKeyDown += async (s, x) => await GoToPicPreviewKeysAsync(s, x).ConfigureAwait(false);
            GoToPicBox.ToolTip = TranslationHelper.GetTranslation("GoToImageAtSpecifiedIndex");
        };
    }

    private static async Task GoToPicEventAsync()
    {
        if (ErrorHandling.CheckOutOfRange()) return;

        if (int.TryParse(GetImageSettingsMenu.GoToPic.GoToPicBox.Text, out var x))
        {
            x--;
            x = x <= 0 ? 0 : x;
            x = x >= Pics.Count ? Pics.Count - 1 : x;
            // If the gallery is open, deselect current index
            if (GetPicGallery is not null)
            {
                await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(() =>
                {
                    GetPicGallery.Scroller.CanContentScroll = true; // Disable animations
                                                                    // Deselect current item
                    GalleryNavigation.SetSelected(GalleryNavigation.SelectedGalleryItem, false);
                    GalleryNavigation.SetSelected(FolderIndex, false);
                });
            }

            await LoadPic.LoadPicAtIndexAsync(x).ConfigureAwait(false);
            await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(() =>
            {
                GetImageSettingsMenu.GoToPic.GoToPicBox.Text = (x + 1).ToString(CultureInfo.CurrentCulture);
                if (GetPicGallery is null)
                {
                    return;
                }

                // Select next item
                GalleryNavigation.SetSelected(FolderIndex, true);
                GalleryNavigation.SelectedGalleryItem = FolderIndex;
                GalleryNavigation.ScrollToGalleryCenter();
            });
        }
        else if (Pics.Count > 0 && Pics.Count > FolderIndex)
        {
            GetImageSettingsMenu.GoToPic.GoToPicBox.Text = FolderIndex.ToString(CultureInfo.CurrentCulture);
        }
    }

    private static void ClearGoTo()
    {
        GetImageSettingsMenu.GoToPic.GoToPicBox.CaretBrush = new SolidColorBrush(Colors.Transparent);
        FocusManager.SetFocusedElement(FocusManager.GetFocusScope(GetImageSettingsMenu.GoToPic.GoToPicBox), null);
        Close_UserControls();
        Keyboard.ClearFocus();
        ConfigureWindows.GetMainWindow.Focus();
    }

    private async Task GoToPicPreviewKeysAsync(object sender, KeyEventArgs e)
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
                GoToPicBox.Text = FolderIndex.ToString(CultureInfo.CurrentCulture);
                ClearGoTo();
                e.Handled = true;
                break;

            case Key.Enter: // Execute it!
                await GoToPicEventAsync().ConfigureAwait(false);
                await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(ClearGoTo);
                break;

            default:
                e.Handled = true; // Don't allow other keys
                break;
        }
    }
}