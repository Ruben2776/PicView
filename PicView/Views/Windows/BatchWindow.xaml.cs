using PicView.Editing;
using System;
using System.Windows;
using System.Windows.Input;
using static PicView.Library.Fields;
using static PicView.UILogic.Animations.MouseOverAnimations;

namespace PicView.Views.Windows
{
    public partial class BatchWindow : Window
    {
        public BatchWindow()
        {
            InitializeComponent();

            ContentRendered += Window_ContentRendered;
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            // Switch radioes
            SelectedRadioBorder.MouseLeftButtonDown += delegate { SelectedRadio.IsChecked = true; };
            AllRadioBorder.MouseLeftButtonDown += delegate { AllRadio.IsChecked = true; };
            AllRadio.Checked += delegate { SelectedRadio.IsChecked = false; };
            SelectedRadio.Checked += delegate { AllRadio.IsChecked = false; };

            // SelectedRadio
            SelectedRadioBorder.PreviewMouseLeftButtonDown += (s, x) => PreviewMouseButtonDownAnim(SelectedBrush);
            SelectedRadioBorder.MouseEnter += (s, x) => ButtonMouseOverAnim(SelectedBrush, true);
            SelectedRadioBorder.MouseLeave += (s, x) => ButtonMouseLeaveAnimBgColor(SelectedBrush, false);

            // AllRadio
            AllRadioBorder.PreviewMouseLeftButtonDown += (s, x) => PreviewMouseButtonDownAnim(AllBrush);
            AllRadioBorder.MouseEnter += (s, x) => ButtonMouseOverAnim(AllBrush, true);
            AllRadioBorder.MouseLeave += (s, x) => ButtonMouseLeaveAnimBgColor(AllBrush, false);

            // FlipBox
            FlipBox.PreviewMouseLeftButtonDown += (s, x) => PreviewMouseButtonDownAnim(FlipBoxBrush);
            FlipBox.MouseEnter += (s, x) => ButtonMouseOverAnim(FlipBoxBrush, true);
            FlipBox.MouseLeave += (s, x) => ButtonMouseLeaveAnimBgColor(FlipBoxBrush, false);

            // OptimizeBox
            OptimizeBox.PreviewMouseLeftButtonDown += (s, x) => PreviewMouseButtonDownAnim(OptimizeBoxBrush);
            OptimizeBox.MouseEnter += (s, x) => ButtonMouseOverAnim(OptimizeBoxBrush, true);
            OptimizeBox.MouseLeave += (s, x) => ButtonMouseLeaveAnimBgColor(OptimizeBoxBrush, false);

            // AspectRatioBox
            AspectRatioBox.PreviewMouseLeftButtonDown += (s, x) => PreviewMouseButtonDownAnim(AspectRatioBoxBrush);
            AspectRatioBox.MouseEnter += (s, x) => ButtonMouseOverAnim(AspectRatioBoxBrush, true);
            AspectRatioBox.MouseLeave += (s, x) => ButtonMouseLeaveAnimBgColor(AspectRatioBoxBrush, false);

            // StartButton
            StartButton.PreviewMouseLeftButtonDown += (s, x) => PreviewMouseButtonDownAnim(StartBrush);
            StartButton.MouseEnter += (s, x) => ButtonMouseOverAnim(StartBrush, true);
            StartButton.MouseLeave += (s, x) => ButtonMouseLeaveAnimBgColor(StartBrush, false);
            StartButton.Click += delegate { _ = Batch_Resize.StartProcessing(); };

            // CloseButton
            CloseButton.TheButton.Click += delegate { Hide(); TheMainWindow.Focus(); };

            // MinButton
            MinButton.TheButton.Click += delegate { SystemCommands.MinimizeWindow(this); };

            TitleBar.MouseLeftButtonDown += delegate { DragMove(); };

            KeyUp += KeysUp;
        }

        #region Keyboard Shortcuts

        private void KeysUp(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Escape:
                    Hide();
                    TheMainWindow.Focus();
                    break;

                case Key.Q:
                    if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                    {
                        Environment.Exit(0);
                    }
                    break;
            }
        }

        #endregion Keyboard Shortcuts
    }
}