using PicView.Animations;
using PicView.Properties;
using PicView.Shortcuts;
using PicView.UILogic;
using PicView.UILogic.Sizing;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Timer = System.Timers.Timer;

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
                WidthBox.PreviewKeyDown += async (_, e) =>
                    await QuickResizeShortcuts.QuickResizePreviewKeys(e, WidthBox.Text, HeightBox.Text)
                        .ConfigureAwait(false);

                // HeightBox
                HeightBox.AcceptsReturn = false;
                HeightBox.PreviewKeyDown += async (_, e) =>
                    await QuickResizeShortcuts.QuickResizePreviewKeys(e, WidthBox.Text, HeightBox.Text)
                        .ConfigureAwait(false);

                PercentageBox.PreviewKeyDown += async (_, e) =>
                {
                    switch (e.Key)
                    {
                        // Prevent alt tab navigation and instead move back to WidthBox
                        case Key.Tab:
                            e.Handled = true;
                            WidthBox.Focus();
                            break;

                        case Key.Enter:
                            e.Handled = true;
                            await QuickResizeShortcuts.Fire(WidthBox.Text, HeightBox.Text).ConfigureAwait(false);
                            break;
                    }
                };

                WidthBox.KeyUp += (_, e) => QuickResizeShortcuts.QuickResizeAspectRatio(WidthBox, HeightBox, true, e);
                HeightBox.KeyUp += (_, e) => QuickResizeShortcuts.QuickResizeAspectRatio(WidthBox, HeightBox, false, e);

                var colorAnimation = new ColorAnimation { Duration = TimeSpan.FromSeconds(.1) };

                // ApplyButton
                ApplyButton.MouseEnter += delegate
                {
                    colorAnimation.From = (Color)Application.Current.Resources["MainColor"];
                    colorAnimation.To = AnimationHelper.GetPreferredColor();
                    ApplyBrush.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
                    Cursor = Cursors.Hand;
                };
                ApplyButton.MouseLeave += delegate
                {
                    colorAnimation.From = AnimationHelper.GetPreferredColor();
                    colorAnimation.To = (Color)Application.Current.Resources["MainColor"];
                    ApplyBrush.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
                    Cursor = Cursors.Arrow;
                };

                ApplyButton.MouseLeftButtonDown += async (_, _) =>
                    await QuickResizeShortcuts.Fire(WidthBox.Text, HeightBox.Text).ConfigureAwait(false);

                foreach (var t in PercentageBox.Items)
                {
                    var item = (ComboBoxItem)t;
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
            if (((ComboBoxItem)PercentageBox.SelectedItem).Content is not string content)
            {
                return;
            }

            var value = decimal.Parse(content.TrimEnd('%', ' ')) / 100M; // Convert from percentage to decimal

            QuickResizeShortcuts.QuickResizeAspectRatio(WidthBox, HeightBox, true, null, (double)value);
        }

        public void Show()
        {
            grid.Width = Settings.Default.AutoFitWindow
                ? ScaleImage.XWidth
                : ConfigureWindows.GetMainWindow.ActualWidth;
            grid.Height = Settings.Default.AutoFitWindow
                ? ScaleImage.XHeight
                : ConfigureWindows.GetMainWindow.ActualHeight;
            Visibility = Visibility.Visible;
            AnimationHelper.Fade(this, TimeSpan.FromSeconds(.4), TimeSpan.Zero, 0, 1);
            PercentageBox.SelectedIndex = 0;
            WidthBox.Text = ConfigureWindows.GetMainWindow.MainImage.Source?.Width.ToString(CultureInfo.CurrentCulture);
            HeightBox.Text =
                ConfigureWindows.GetMainWindow.MainImage?.Source?.Height.ToString(CultureInfo.CurrentCulture);

            var timer = new Timer(401) { AutoReset = false, Enabled = true };
            timer.Elapsed += delegate
            {
                ConfigureWindows.GetMainWindow.Dispatcher.Invoke(() => { Keyboard.Focus(WidthBox); });
            };
        }

        public void Hide()
        {
            ConfigureWindows.GetMainWindow.Dispatcher.Invoke(() =>
            {
                AnimationHelper.Fade(this, TimeSpan.FromSeconds(.6), TimeSpan.Zero, 1, 0);
            });

            var timer = new Timer(601) { AutoReset = false, Enabled = true };
            timer.Elapsed += delegate
            {
                ConfigureWindows.GetMainWindow.Dispatcher.Invoke(() => { Visibility = Visibility.Collapsed; });
            };
        }
    }
}