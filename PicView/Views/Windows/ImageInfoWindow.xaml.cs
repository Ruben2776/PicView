using PicView.FileHandling;
using PicView.ImageHandling;
using PicView.UILogic;
using PicView.UILogic.Animations;
using System;
using System.IO;
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
        static object? rating;

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
            Top = ConfigureWindows.GetMainWindow.LeftButton.PointToScreen(new Point(0, 0)).X - Height;

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

            // ShowInFolder
            ShowInFolder.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(ShowInFolderFill); };
            ShowInFolder.MouseEnter += delegate { ButtonMouseOverAnim(ShowInFolderFill); };
            ShowInFolder.MouseEnter += delegate { AnimationHelper.MouseEnterBgTexColor(ShowInFolderBrush); };
            ShowInFolder.MouseLeave += delegate { ButtonMouseLeaveAnim(ShowInFolderFill); };
            ShowInFolder.MouseLeave += delegate { AnimationHelper.MouseLeaveBgTexColor(ShowInFolderBrush); };
            ShowInFolder.Click += delegate
            {
                FileHandling.Open_Save.Open_In_Explorer();
            };

            // Optimize Image
            OptimizeImageButton.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(OptimizeImageFill); };
            OptimizeImageButton.MouseEnter += delegate { ButtonMouseOverAnim(OptimizeImageFill); };
            OptimizeImageButton.MouseEnter += delegate { AnimationHelper.MouseEnterBgTexColor(OptimizeImageBrush); };
            OptimizeImageButton.MouseLeave += delegate { ButtonMouseLeaveAnim(OptimizeImageFill); };
            OptimizeImageButton.MouseLeave += delegate { AnimationHelper.MouseLeaveBgTexColor(OptimizeImageBrush); };
            OptimizeImageButton.Click += async (_, _) => await ImageFunctions.OptimizeImageAsyncWithErrorChecking().ConfigureAwait(false);

            TitleBar.MouseLeave += delegate { UpdateStars(); };

            // Stars
            Star1.MouseLeftButtonDown += async delegate 
            { 
                await ImageFunctions.SetRating(1).ConfigureAwait(false);
                await UpdateValuesAsync(ChangeImage.Navigation.Pics[ChangeImage.Navigation.FolderIndex]).ConfigureAwait(false);
            };
            Star1.MouseEnter += delegate { UpdateStars(1); };
            Star1.MouseLeave += delegate { UpdateStars(); };

            Star2.MouseLeftButtonDown += async delegate
            {
                await ImageFunctions.SetRating(2).ConfigureAwait(false);
                await UpdateValuesAsync(ChangeImage.Navigation.Pics[ChangeImage.Navigation.FolderIndex]).ConfigureAwait(false);
            };
            Star2.MouseEnter += delegate { UpdateStars(2); };
            Star2.MouseLeave += delegate { UpdateStars(); };

            Star3.MouseLeftButtonDown += async delegate
            {
                await ImageFunctions.SetRating(3).ConfigureAwait(false);
                await UpdateValuesAsync(ChangeImage.Navigation.Pics[ChangeImage.Navigation.FolderIndex]).ConfigureAwait(false);
            };
            Star3.MouseEnter += delegate { UpdateStars(3); };
            Star3.MouseLeave += delegate { UpdateStars(); };

            Star4.MouseLeftButtonDown += async delegate
            {
                await ImageFunctions.SetRating(4).ConfigureAwait(false);
                await UpdateValuesAsync(ChangeImage.Navigation.Pics[ChangeImage.Navigation.FolderIndex]).ConfigureAwait(false);
            };
            Star4.MouseEnter += delegate { UpdateStars(4); };
            Star4.MouseLeave += delegate { UpdateStars(); };

            Star5.MouseLeftButtonDown += async delegate
            {
                await ImageFunctions.SetRating(5).ConfigureAwait(false);
                await UpdateValuesAsync(ChangeImage.Navigation.Pics[ChangeImage.Navigation.FolderIndex]).ConfigureAwait(false);
            };
            Star5.MouseEnter += delegate{ UpdateStars(5); };
            Star5.MouseLeave += delegate { UpdateStars(); };

            // FilenameBox
            FilenameBox.AcceptsReturn = false;
            FilenameBox.KeyUp += async (_, e) => 
            {
                if (e.Key != System.Windows.Input.Key.Enter) { return; }

                e.Handled = true;
                var file = (Path.GetDirectoryName(ChangeImage.Navigation.Pics[ChangeImage.Navigation.FolderIndex])) + "/" + FilenameBox.Text;
                var rename = await FileFunctions.RenameFileWithErrorChecking(file).ConfigureAwait(false);
                if (rename == false)
                {
                    Tooltip.ShowTooltipMessage(Application.Current.Resources["AnErrorOccuredMovingFile"]);
                }
                else
                {
                    await ConfigureWindows.GetImageInfoWindow.Dispatcher.BeginInvoke(DispatcherPriority.Background, () =>
                    {
                        Keyboard.ClearFocus();
                    });
                }
            };

            // FolderBox
            FolderBox.AcceptsReturn = false;
            FolderBox.KeyUp += async (_, e) =>
            {
                if (e.Key != System.Windows.Input.Key.Enter) { return; }

                e.Handled = true;
                var file =  FolderBox.Text + "/" + Path.GetFileName(FullPathBox.Text);
                var rename = await FileFunctions.RenameFileWithErrorChecking(file).ConfigureAwait(false);
                if (rename == false)
                {
                    Tooltip.ShowTooltipMessage(Application.Current.Resources["AnErrorOccuredMovingFile"]);
                }
                else
                {
                    await ConfigureWindows.GetImageInfoWindow.Dispatcher.BeginInvoke(DispatcherPriority.Background, () =>
                    {
                        Keyboard.ClearFocus();
                    });
                }
            };

            // FullPathBox
            FullPathBox.AcceptsReturn = false;
            FullPathBox.KeyUp += async (_, e) =>
            {
                if (e.Key != System.Windows.Input.Key.Enter) { return; }

                e.Handled = true;
                var rename = await FileFunctions.RenameFileWithErrorChecking(FullPathBox.Text).ConfigureAwait(false);
                if (rename == false)
                {
                    Tooltip.ShowTooltipMessage(Application.Current.Resources["AnErrorOccuredMovingFile"]);
                }
                else
                {
                    await ConfigureWindows.GetImageInfoWindow.Dispatcher.BeginInvoke(DispatcherPriority.Background, () =>
                    {
                        Keyboard.ClearFocus();
                    });
                }
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

        internal async Task UpdateValuesAsync(string? file)
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

                    rating = data[12];
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

                    rating = 0;
                }

                UpdateStars();
            });
        }

        private void UpdateStars()
        {
            if (rating is null || (string)rating == string.Empty || (string)rating == "0")
            {
                UpdateStars(0);
                return;
            }

            int percent = Convert.ToInt32(rating.ToString());
            var stars = Math.Ceiling(percent / 20d);

            UpdateStars((int)stars);
        }

        private void UpdateStars(int stars)
        {
            switch (stars)
            {
                default:
                case 0:
                    Star1.OutlineStar();
                    Star2.OutlineStar();
                    Star3.OutlineStar();
                    Star4.OutlineStar();
                    Star5.OutlineStar();
                return;
                case 1:
                    Star1.FillStar();
                    Star2.OutlineStar();
                    Star3.OutlineStar();
                    Star4.OutlineStar();
                    Star5.OutlineStar();
                return;
                case 2:
                    Star1.FillStar();
                    Star2.FillStar();
                    Star3.OutlineStar();
                    Star4.OutlineStar();
                    Star5.OutlineStar();
                return;
                case 3:
                    Star1.FillStar();
                    Star2.FillStar();
                    Star3.FillStar();
                    Star4.OutlineStar();
                    Star5.OutlineStar();
                return;
                case 4:
                    Star1.FillStar();
                    Star2.FillStar();
                    Star3.FillStar();
                    Star4.FillStar();
                    Star5.OutlineStar();
                return;
                case 5:
                    Star1.FillStar();
                    Star2.FillStar();
                    Star3.FillStar();
                    Star4.FillStar();
                    Star5.FillStar();
                return;
            }
        }
    }
}