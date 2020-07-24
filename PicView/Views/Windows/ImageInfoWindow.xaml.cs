using PicView.ImageHandling;
using PicView.UILogic.Animations;
using System;
using System.Windows;
using System.Windows.Input;
using static PicView.ChangeImage.Navigation;
using static PicView.Library.Fields;
using static PicView.UILogic.Animations.MouseOverAnimations;

namespace PicView.Views.Windows
{
    public partial class ImageInfoWindow : Window
    {
        public ImageInfoWindow()
        {
            InitializeComponent();

            ContentRendered += Window_ContentRendered;
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            KeyUp += KeysUp;

            // CloseButton
            CloseButton.TheButton.Click += delegate { Hide(); TheMainWindow.Focus(); };

            // MinButton
            MinButton.TheButton.Click += delegate { SystemCommands.MinimizeWindow(this); };

            TitleBar.MouseLeftButtonDown += delegate { DragMove(); };
            MainBackground.MouseLeftButtonDown += delegate { DragMove(); };

            // FileProperties
            FileProperties.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(FilePropertiesFill); };
            FileProperties.MouseEnter += delegate { ButtonMouseOverAnim(FilePropertiesFill); };
            FileProperties.MouseEnter += delegate { AnimationHelper.MouseEnterBgTexColor(FilePropertiesBrush); };
            FileProperties.MouseLeave += delegate { ButtonMouseLeaveAnim(FilePropertiesFill); };
            FileProperties.MouseLeave += delegate { AnimationHelper.MouseLeaveBgTexColor(FilePropertiesBrush); };
            FileProperties.Click += delegate 
            {
                SystemIntegration.NativeMethods.ShowFileProperties(Pics[FolderIndex]);
            };

            // Print
            Print.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(PrintFill); };
            Print.MouseEnter += delegate { ButtonMouseOverAnim(PrintFill); };
            Print.MouseEnter += delegate { AnimationHelper.MouseEnterBgTexColor(PrintBrush); };
            Print.MouseLeave += delegate { ButtonMouseLeaveAnim(PrintFill); };
            Print.MouseLeave += delegate { AnimationHelper.MouseLeaveBgTexColor(PrintBrush); };
            Print.Click += delegate
            {
                FileHandling.Open_Save.Print(Pics[FolderIndex]);
            };

            // OpenWith
            OpenWith.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(OpenWithFill); };
            OpenWith.MouseEnter += delegate { ButtonMouseOverAnim(OpenWithFill); };
            OpenWith.MouseEnter += delegate { AnimationHelper.MouseEnterBgTexColor(OpenWithBrush); };
            OpenWith.MouseLeave += delegate { ButtonMouseLeaveAnim(OpenWithFill); };
            OpenWith.MouseLeave += delegate { AnimationHelper.MouseLeaveBgTexColor(OpenWithBrush); };
            OpenWith.Click += delegate
            {
                FileHandling.Open_Save.OpenWith(Pics[FolderIndex]);
            };

            // ShowInFoler
            ShowInFoler.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(ShowInFolerFill); };
            ShowInFoler.MouseEnter += delegate { ButtonMouseOverAnim(ShowInFolerFill); };
            ShowInFoler.MouseEnter += delegate { AnimationHelper.MouseEnterBgTexColor(ShowInFolerBrush); };
            ShowInFoler.MouseLeave += delegate { ButtonMouseLeaveAnim(ShowInFolerFill); };
            ShowInFoler.MouseLeave += delegate { AnimationHelper.MouseLeaveBgTexColor(ShowInFolerBrush); };
            ShowInFoler.Click += delegate
            {
                FileHandling.Open_Save.Open_In_Explorer();
            };

            if (Pics.Count > 0)
            {
                UpdateValues();
            }
        }

        internal void UpdateValues()
        {
            var data = GetImageData.RetrieveData(Pics[FolderIndex]);

            FilenameBox.Text = data[0];

            FolderBox.Text = data[1];

            FullPathBox.Text = data[2];

            CreatedBox.Text = data[3];

            ModifiedBox.Text = data[4];

            SizePxBox.Text = data[5];

            ResolutionBox.Text = data[6];

            BitDepthBox.Text = data[7];

            SizeMpBox.Text = data[8];

            PrintSizeCmBox.Text = data[9];

            PrintSizeInBox.Text = data[10];

            AspectRatioBox.Text = data[11];

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