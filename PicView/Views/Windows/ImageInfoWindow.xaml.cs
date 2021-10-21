using PicView.Animations;
using PicView.ImageHandling;
using PicView.UILogic;
using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using static PicView.Animations.MouseOverAnimations;
using static PicView.ChangeImage.Navigation;
using static PicView.UILogic.ImageInfo;

namespace PicView.Views.Windows
{
    public partial class ImageInfoWindow : Window
    {
        double startheight;

        public ImageInfoWindow()
        {
            InitializeComponent();
            startheight = Height;
            ContentRendered += async (sender, e) =>
            {
                Window_ContentRendered(sender, e);
                if (Pics.Count > FolderIndex)
                {
                    await UpdateValuesAsync(new FileInfo(Pics?[FolderIndex])).ConfigureAwait(false);
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

            // Delete
            Delete.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(DeleteFill); };
            Delete.MouseEnter += delegate { ButtonMouseOverAnim(DeleteFill); };
            Delete.MouseEnter += delegate { AnimationHelper.MouseEnterBgTexColor(DeleteBrush); };
            Delete.MouseLeave += delegate { ButtonMouseLeaveAnim(DeleteFill); };
            Delete.MouseLeave += delegate { AnimationHelper.MouseLeaveBgTexColor(DeleteBrush); };
            Delete.Click += async delegate
            {
                await FileHandling.DeleteFiles.DeleteFileAsync(Keyboard.IsKeyDown(Key.LeftShift)).ConfigureAwait(false);
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
            OptimizeImageButton.PreviewMouseLeftButtonDown += (_, _) => PreviewMouseButtonDownAnim(OptimizeImageFill);
            OptimizeImageButton.MouseEnter += (_, _) => ButtonMouseOverAnim(OptimizeImageFill);
            OptimizeImageButton.MouseEnter += (_, _) => AnimationHelper.MouseEnterBgTexColor(OptimizeImageBrush);
            OptimizeImageButton.MouseLeave += (_, _) => ButtonMouseLeaveAnim(OptimizeImageFill);
            OptimizeImageButton.MouseLeave += (_, _) => AnimationHelper.MouseLeaveBgTexColor(OptimizeImageBrush);
            OptimizeImageButton.Click += async (_, _) => await ImageFunctions.OptimizeImageAsyncWithErrorChecking().ConfigureAwait(false);

            // ExpandButton
            ExpandButton.PreviewMouseLeftButtonDown += (_, _) => PreviewMouseButtonDownAnim(chevronDownBrush);
            ExpandButton.MouseEnter += (_, _) => ButtonMouseOverAnim(chevronDownBrush);
            ExpandButton.MouseEnter += (_, _) => AnimationHelper.MouseEnterBgTexColor(ExpandButtonBg);
            ExpandButton.MouseLeave += (_, _) => ButtonMouseLeaveAnim(chevronDownBrush);
            ExpandButton.MouseLeave += (_, _) => AnimationHelper.MouseLeaveBgTexColor(ExpandButtonBg);

            ExpandButton.Click += (_, _) =>
            {
                double from, to;
                bool reverse;
                if (Height == startheight)
                {
                    from = startheight;
                    to = 500;
                    reverse = false;
                }
                else
                {
                    to = startheight;
                    from = 500;
                    reverse = true;
                }

                AnimationHelper.HeightAnimation(this, from, to, reverse);
                if (reverse)
                {
                    xGeo.Geometry = Geometry.Parse("F1 M512,512z M0,0z M98,190.06L237.78,353.18A24,24,0,0,0,274.22,353.18L414,190.06C427.34,174.49,416.28,150.44,395.78,150.44L116.18,150.44C95.6799999999999,150.44,84.6199999999999,174.49,97.9999999999999,190.06z");
                }
                else
                {
                    xGeo.Geometry = Geometry.Parse("F1 M512,512z M0,0z M414,321.94L274.22,158.82A24,24,0,0,0,237.78,158.82L98,321.94C84.66,337.51,95.72,361.56,116.22,361.56L395.82,361.56C416.32,361.56,427.38,337.51,414,321.94z");
                }
            };

            TitleBar.MouseLeave += delegate { UpdateStars(); };

            // Stars
            Star1.MouseLeftButtonDown += async delegate
            {
                await ImageFunctions.SetRating(1).ConfigureAwait(false);
                await UpdateValuesAsync(new FileInfo(Pics?[FolderIndex])).ConfigureAwait(false);
            };
            Star1.MouseEnter += delegate { UpdateStars(1); };
            Star1.MouseLeave += delegate { UpdateStars(); };

            Star2.MouseLeftButtonDown += async delegate
            {
                await ImageFunctions.SetRating(2).ConfigureAwait(false);
                await UpdateValuesAsync(new FileInfo(Pics?[FolderIndex])).ConfigureAwait(false);
            };
            Star2.MouseEnter += delegate { UpdateStars(2); };
            Star2.MouseLeave += delegate { UpdateStars(); };

            Star3.MouseLeftButtonDown += async delegate
            {
                await ImageFunctions.SetRating(3).ConfigureAwait(false);
                await UpdateValuesAsync(new FileInfo(Pics?[FolderIndex])).ConfigureAwait(false);
            };
            Star3.MouseEnter += delegate { UpdateStars(3); };
            Star3.MouseLeave += delegate { UpdateStars(); };

            Star4.MouseLeftButtonDown += async delegate
            {
                await ImageFunctions.SetRating(4).ConfigureAwait(false);
                await UpdateValuesAsync(new FileInfo(Pics?[FolderIndex])).ConfigureAwait(false);
            };
            Star4.MouseEnter += delegate { UpdateStars(4); };
            Star4.MouseLeave += delegate { UpdateStars(); };

            Star5.MouseLeftButtonDown += async delegate
            {
                await ImageFunctions.SetRating(5).ConfigureAwait(false);
                await UpdateValuesAsync(new FileInfo(Pics?[FolderIndex])).ConfigureAwait(false);
            };
            Star5.MouseEnter += delegate { UpdateStars(5); };
            Star5.MouseLeave += delegate { UpdateStars(); };

            // FilenameBox
            FilenameBox.AcceptsReturn = false;
            FilenameBox.KeyUp += async (_, e) =>
            await ImageInfo.RenameTask(e, FilenameBox, (Path.GetDirectoryName(ChangeImage.Navigation.Pics[ChangeImage.Navigation.FolderIndex])) + "/" + FilenameBox.Text + Path.GetExtension(ChangeImage.Navigation.Pics[ChangeImage.Navigation.FolderIndex])).ConfigureAwait(false);

            // FolderBox
            FolderBox.AcceptsReturn = false;
            FolderBox.KeyUp += async (_, e) => await ImageInfo.RenameTask(e, FolderBox, FolderBox.Text + "/" + Path.GetFileName(FullPathBox.Text)).ConfigureAwait(false);

            // FullPathBox
            FullPathBox.AcceptsReturn = false;
            FullPathBox.KeyUp += async (_, e) => await ImageInfo.RenameTask(e, FullPathBox, FullPathBox.Text).ConfigureAwait(false);

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
    }
}