using System.Windows.Controls;
using System.Windows.Input;
using static PicView.lib.Helper;

namespace PicView.lib.UserControls
{
    /// <summary>
    /// Interaction logic for fileMenu.xaml
    /// </summary>
    public partial class FileMenu : UserControl
    {
        public FileMenu()
        {
            InitializeComponent();

            #region Register Events...

            CloseButton.MouseEnter += CloseButtonMouseOver;
            CloseButton.MouseLeave += CloseButtonMouseLeave;
            CloseButton.PreviewMouseLeftButtonDown += CloseButtonMouseButtonDown;

            PasteButton.MouseEnter += PasteButtonMouseOver;
            PasteButton.MouseLeave += PasteButtonMouseLeave;
            PasteButton.PreviewMouseLeftButtonDown += PasteButtonMouseButtonDown;

            CopyButton.MouseEnter += CopyButtonMouseOver;
            CopyButton.MouseLeave += CopyButtonMouseLeave;
            CopyButton.PreviewMouseLeftButtonDown += CopyButtonMouseButtonDown;

            Open_Border.MouseEnter += Open_Border_MouseEnter;
            Print_Border.MouseEnter += Print_Border_MouseEnter;
            Open_File_Location_Border.MouseEnter += Open_File_Location_Border_MouseEnter;
            Save_File_Location_Border.MouseEnter += Save_File_Location_Border_MouseEnter;

            Open_Border.MouseLeave += Open_Border_MouseLeave;
            Open_File_Location_Border.MouseLeave += Open_File_Location_Border_MouseLeave;
            Print_Border.MouseLeave += Print_Border_MouseLeave;
            Save_File_Location_Border.MouseLeave += Save_File_Location_Border_MouseLeave;

            Open_Border.MouseLeftButtonDown += Open_Border_MouseLeftButtonDown;
            Open_File_Location_Border.MouseLeftButtonDown += Open_File_Location_Border_MouseLeftButtonDown;
            Print_Border.MouseLeftButtonDown += Print_Border_MouseLeftButtonDown;
            Save_File_Location_Border.MouseLeftButtonDown += Save_Border_MouseLeftButtonDown;

            #endregion
        }

        #region Close Button

        private void CloseButtonMouseOver(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseEnterColorEvent(0, 0, 0, 0, CloseButtonBrush, false);
        }

        private void CloseButtonMouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(CloseButtonBrush, false);
        }

        private void CloseButtonMouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(0, 0, 0, 0, CloseButtonBrush, false);
        }

        #endregion

        #region Paste Button

        private void PasteButtonMouseOver(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseEnterColorEvent(0, 0, 0, 0, PasteButtonBrush, false);
        }

        private void PasteButtonMouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(PasteButtonBrush, false);
        }

        private void PasteButtonMouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(0, 0, 0, 0, PasteButtonBrush, false);
        }

        #endregion

        #region Copy Button

        private void CopyButtonMouseOver(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseEnterColorEvent(0, 0, 0, 0, CopyButtonBrush, false);
        }

        private void CopyButtonMouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(CopyButtonBrush, false);
        }

        private void CopyButtonMouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(0, 0, 0, 0, CopyButtonBrush, false);
        }

        #endregion

        #region Borders

        #region Mouse Leave

        private void Open_Border_MouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(
                backgroundBorderColor.A,
                backgroundBorderColor.R,
                backgroundBorderColor.G,
                backgroundBorderColor.B,
                OpenBorderBrush,
                true
            );
        }

        private void Open_File_Location_Border_MouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(
                backgroundBorderColor.A,
                backgroundBorderColor.R,
                backgroundBorderColor.G,
                backgroundBorderColor.B,
                OpenFileLocationBorderBrush,
                true
            );
        }

        private void Print_Border_MouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(
                backgroundBorderColor.A,
                backgroundBorderColor.R,
                backgroundBorderColor.G,
                backgroundBorderColor.B,
                PrintBorderBrush,
                true
            );
        }

        private void Save_File_Location_Border_MouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(
                backgroundBorderColor.A,
                backgroundBorderColor.R,
                backgroundBorderColor.G,
                backgroundBorderColor.B,
                SaveFileLocationBorderBrush,
                true
            );
        }

        #endregion

        #region Mouse Enter

        private void Open_File_Location_Border_MouseEnter(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseEnterColorEvent(165, 23, 23, 23, OpenFileLocationBorderBrush, true);
        }

        private void Open_Border_MouseEnter(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseEnterColorEvent(165, 23, 23, 23, OpenBorderBrush, true);
        }

        private void Print_Border_MouseEnter(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseEnterColorEvent(165, 23, 23, 23, PrintBorderBrush, true);
        }

        private void Save_File_Location_Border_MouseEnter(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseEnterColorEvent(165, 23, 23, 23, SaveFileLocationBorderBrush, true);
        }

        #endregion

        #region Preview Mouse LeftButton Down....

        private void Open_File_Location_Border_MouseLeftButtonDown(object sender, MouseEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(OpenFileLocationBorderBrush, true);
        }

        private void Open_Border_MouseLeftButtonDown(object sender, MouseEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(OpenBorderBrush, true);
        }

        private void Print_Border_MouseLeftButtonDown(object sender, MouseEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(PrintBorderBrush, true);
        }

        private void Save_Border_MouseLeftButtonDown(object sender, MouseEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(SaveFileLocationBorderBrush, true);
        }


        #endregion

        #endregion
    }
}
