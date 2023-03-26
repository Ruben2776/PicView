using PicView.Animations;
using PicView.ChangeImage;
using PicView.ConfigureSettings;
using PicView.Properties;
using PicView.UILogic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using static PicView.Animations.MouseOverAnimations;
using static PicView.ChangeImage.Navigation;
using static PicView.UILogic.UC;

namespace PicView.Views.UserControls.Buttons
{
    public partial class GoToPicButton : UserControl
    {
        public GoToPicButton()
        {
            InitializeComponent();

            Loaded += delegate
            {
                TheButton.MouseEnter += (_, _) => ButtonMouseOverAnim(GoToPicBrush, true);
                TheButton.MouseLeave += (_, _) => ButtonMouseLeaveAnimBgColor(GoToPicBrush);
                TheButton.Click += async (_, _) => await GoToPicEventAsync().ConfigureAwait(false);

                if (!Settings.Default.DarkTheme)
                {
                    AnimationHelper.LightThemeMouseEvent(this, IconBrush1);
                    AnimationHelper.LightThemeMouseEvent(this, IconBrush2);
                }

                GoToPicBox.PreviewMouseLeftButtonDown += delegate
                {
                    GoToPicBox.CaretBrush = new SolidColorBrush(ConfigColors.MainColor);
                };
                GoToPicBox.PreviewKeyDown += async (s, x) => await GoToPicPreviewKeysAsync(s, x).ConfigureAwait(false);
            };
        }

        internal static async Task GoToPicEventAsync()
        {
            if (ErrorHandling.CheckOutOfRange()) return;

            if (int.TryParse(GetImageSettingsMenu.GoToPic.GoToPicBox.Text, out int x))
            {
                x--;
                x = x <= 0 ? 0 : x;
                x = x >= Pics.Count ? Pics.Count - 1 : x;
                await LoadPic.LoadPicAtIndexAsync(x).ConfigureAwait(false);
                await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(() => 
                    GetImageSettingsMenu.GoToPic.GoToPicBox.Text = (x + 1).ToString(CultureInfo.CurrentCulture));
            }
            else if (Pics.Count > 0 && Pics.Count > FolderIndex)
            {
                GetImageSettingsMenu.GoToPic.GoToPicBox.Text = FolderIndex.ToString(CultureInfo.CurrentCulture);
            }
        }

        internal static void ClearGoTo()
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
                    GoToPicBox.Text = FolderIndex.ToString(CultureInfo.CurrentCulture);
                    ClearGoTo();
                    e.Handled = true;
                    break;

                case Key.Enter: // Execute it!
                    await GoToPicEventAsync().ConfigureAwait(false);
                    await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(() => ClearGoTo());
                    break;

                default:
                    e.Handled = true; // Don't allow other keys
                    break;
            }
        }
    }
}