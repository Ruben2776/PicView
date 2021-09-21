using PicView.ImageHandling;
using PicView.UILogic;
using PicView.UILogic.Animations;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using static PicView.ChangeImage.Navigation;
using static PicView.UILogic.Animations.MouseOverAnimations;

namespace PicView.Views.Windows
{
    public partial class ImageInfoWindow : Window
    {
        public ImageInfoWindow()
        {
            InitializeComponent();

            ContentRendered += async (sender, e) =>
            {
                Window_ContentRendered(sender, e);
                if (Pics.Count > FolderIndex)
                {
                    await UpdateValuesAsync(Pics[FolderIndex]).ConfigureAwait(false);
                }
                else
                {
                    await UpdateValuesAsync(null).ConfigureAwait(false);
                }
            };
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            KeyDown += (_, e) => Shortcuts.GenericWindowShortcuts.KeysDown(null, e, this);

            // Hack to deselect border on mouse click
            MouseLeftButtonDown += delegate
            {
                FocusManager.SetFocusedElement(FocusManager.GetFocusScope(this), null);
                Keyboard.ClearFocus();
                Focus();
            };

            // CloseButton
            CloseButton.TheButton.Click += delegate { Hide(); ConfigureWindows.GetMainWindow.Focus(); };

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
            OpenWith.Click += delegate { FileHandling.Open_Save.OpenWith(Pics[FolderIndex]); };

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

            FilenameCopy.TheButton.Click += delegate
            {
                Clipboard.SetText(FilenameBox.Text);
            };

            FolderCopy.TheButton.Click += delegate
            {
                Clipboard.SetText(FolderBox.Text);
            };

            FullpathCopy.TheButton.Click += delegate
            {
                Clipboard.SetText(FullPathBox.Text);
            };

            CreatedCopy.TheButton.Click += delegate
            {
                Clipboard.SetText(CreatedBox.Text);
            };

            ModifiedCopy.TheButton.Click += delegate
            {
                Clipboard.SetText(ModifiedBox.Text);
            };

            SizePxCopy.TheButton.Click += delegate
            {
                Clipboard.SetText(SizePxBox.Text);
            };

            SizeMpCopy.TheButton.Click += delegate
            {
                Clipboard.SetText(SizeMpBox.Text);
            };

            DpiCopy.TheButton.Click += delegate
            {
                Clipboard.SetText(ResolutionBox.Text);
            };

            PrintSizeCmCopy.TheButton.Click += delegate
            {
                Clipboard.SetText(PrintSizeCmBox.Text);
            };

            PrintSizeInCopy.TheButton.Click += delegate
            {
                Clipboard.SetText(PrintSizeInBox.Text);
            };

            AspectRatioCopy.TheButton.Click += delegate
            {
                Clipboard.SetText(AspectRatioBox.Text);
            };
        }

        internal async Task UpdateValuesAsync(string file)
        {
            var data = await Task.Run(async () => (await GetImageData.RetrieveDataAsync(file).ConfigureAwait(false)));

            await ConfigureWindows.GetImageInfoWindow.Dispatcher.BeginInvoke(DispatcherPriority.Normal, () =>
            {
                if (data != null)
                {

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
                else
                {
                    FilenameBox.Text =

                    FolderBox.Text =

                    FullPathBox.Text =

                    CreatedBox.Text =

                    ModifiedBox.Text =

                    SizePxBox.Text =

                    ResolutionBox.Text =

                    BitDepthBox.Text =

                    SizeMpBox.Text =

                    PrintSizeCmBox.Text =

                    PrintSizeInBox.Text =

                    AspectRatioBox.Text = string.Empty;
                }

            });
        }
    }
}