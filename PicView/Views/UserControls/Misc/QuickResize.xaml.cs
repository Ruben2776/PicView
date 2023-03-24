using PicView.Animations;
using PicView.Shortcuts;
using PicView.UILogic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace PicView.Views.UserControls.Misc
{
    public partial class QuickResize : UserControl
    {
        public QuickResize()
        {
            InitializeComponent();

            Loaded += delegate
            {
                x2.MouseLeftButtonDown += (_, _) => Hide();
                WidthBox.GotKeyboardFocus += (_, _) => WidthBox.SelectAll();
                HeightBox.GotKeyboardFocus += (_, _) => HeightBox.SelectAll();

                // WidhtBox
                WidthBox.AcceptsReturn = false;
                WidthBox.PreviewKeyDown += async (_, e) => await QuickResizeShortcuts.QuickResizePreviewKeys(e, WidthBox.Text, HeightBox.Text).ConfigureAwait(false);

                // HeightBox
                HeightBox.AcceptsReturn = false;
                HeightBox.PreviewKeyDown += async (_, e) => await QuickResizeShortcuts.QuickResizePreviewKeys(e, WidthBox.Text, HeightBox.Text).ConfigureAwait(false);

                PercentageBox.PreviewKeyDown += async (_, e) =>
                {
                    // Prevent alt tab navigation and instead move back to WidthBox
                    if (e.Key == Key.Tab)
                    {
                        e.Handled = true;
                        WidthBox.Focus();
                    }
                    if (e.Key == Key.Enter)
                    {
                        e.Handled = true;
                        await QuickResizeShortcuts.Fire(WidthBox.Text, HeightBox.Text).ConfigureAwait(false);
                    }
                };

                WidthBox.KeyUp += (_, e) => QuickResizeShortcuts.QuickResizeAspectRatio(WidthBox, HeightBox, true, e);
                HeightBox.KeyUp += (_, e) => QuickResizeShortcuts.QuickResizeAspectRatio(WidthBox, HeightBox, false, e);

                var colorAnimation = new ColorAnimation { Duration = TimeSpan.FromSeconds(.1) };

                // ApplyButton
                ApplyButton.MouseEnter += delegate
                {
                    colorAnimation.From = (Color)Application.Current.Resources["MainColor"];
                    colorAnimation.To = AnimationHelper.GetPrefferedColor();
                    ApplyBrush.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
                    Cursor = Cursors.Hand;
                };
                ApplyButton.MouseLeave += delegate
                {
                    colorAnimation.From = AnimationHelper.GetPrefferedColor();
                    colorAnimation.To = (Color)Application.Current.Resources["MainColor"];
                    ApplyBrush.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
                    Cursor = Cursors.Arrow;
                };

                ApplyButton.MouseLeftButtonDown += async (_, _) => await QuickResizeShortcuts.Fire(WidthBox.Text, HeightBox.Text).ConfigureAwait(false);

                for (int i = 0; i < PercentageBox.Items.Count; i++)
                {
                    var item = (ComboBoxItem)PercentageBox.Items[i];
                    item.GotFocus += (_, _) =>
                    {
                        if (item.IsMouseOver is false)
                        {
                            item.IsSelected = true;
                        }
                    };
                }
                PercentageBox.SelectionChanged += PercentageBox_SelectionChanged;
            };
        }

        private void PercentageBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var originalWidth = ConfigureWindows.GetMainWindow.MainImage.Source.Width;
            var originalHeight = ConfigureWindows.GetMainWindow.MainImage.Source.Height;

            var content = ((ComboBoxItem)PercentageBox.SelectedItem).Content as string;
            if (content == null) { return; }

            var value = decimal.Parse(content.TrimEnd('%', ' ')) / 100M; // Convert from percentage to decimal

            QuickResizeShortcuts.QuickResizeAspectRatio(WidthBox, HeightBox, true, null, (double)value);
        }

        public void Show()
        {
            grid.Width = UILogic.Sizing.ScaleImage.XWidth;
            grid.Height = UILogic.Sizing.ScaleImage.XHeight;
            Visibility = Visibility.Visible;
            AnimationHelper.Fade(this, TimeSpan.FromSeconds(.4), TimeSpan.Zero, 0, 1);
            PercentageBox.SelectedIndex = 0;
            WidthBox.Text = ConfigureWindows.GetMainWindow.MainImage.Source?.Width.ToString();
            HeightBox.Text = ConfigureWindows.GetMainWindow.MainImage?.Source?.Height.ToString();

            var timer = new System.Timers.Timer(401) { AutoReset = false, Enabled = true };
            timer.Elapsed += delegate
            {
                ConfigureWindows.GetMainWindow.Dispatcher.Invoke(() =>
                {
                    Keyboard.Focus(WidthBox);
                });
            };
        }

        public void Hide()
        {
            ConfigureWindows.GetMainWindow.Dispatcher.Invoke(() =>
            {
                AnimationHelper.Fade(this, TimeSpan.FromSeconds(.6), TimeSpan.Zero, 1, 0);
            });

            var timer = new System.Timers.Timer(601) { AutoReset = false, Enabled = true };
            timer.Elapsed += delegate
            {
                ConfigureWindows.GetMainWindow.Dispatcher.Invoke(() =>
                {
                    Visibility = Visibility.Collapsed;
                });
            };
        }
    }
}