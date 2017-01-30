using PicView.lib;
using System;
using System.Diagnostics;
using System.Reflection;
using static PicView.lib.Helper;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;

namespace PicView.UserControls
{
    /// <summary>
    /// Interaction logic for About.xaml
    /// </summary>
    public partial class About : UserControl
    {
        private const string version = "Version : ";

        #region Window Logic

        #region Constructor

        public About()
        {
            InitializeComponent();
            Loaded += About_Loaded;

            #region Get version

            Assembly assembly = Assembly.GetExecutingAssembly();
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            appVersion.Content = version + fvi.FileVersion;

            #endregion
        }

        #endregion

        #region Loaded

        private void About_Loaded(object sender, EventArgs e)
        {
            #region Add events

            #region CloseButton

            CloseButton.Click += (s, x) => Close(this);

            CloseButton.MouseEnter += (s, x) =>
            {
                AnimationHelper.MouseEnterColorEvent(0, 0, 0, 0, CloseButtonBrush, true);
            };

            CloseButton.MouseLeave += (s, x) =>
            {
                AnimationHelper.MouseLeaveColorEvent(0, 0, 0, 0, CloseButtonBrush, true);
            };

            CloseButton.PreviewMouseLeftButtonDown += (s, x) =>
            {
                AnimationHelper.PreviewMouseLeftButtonDownColorEvent(CloseButtonBrush, true);
            };

            #endregion

            Aller.MouseEnter += AllerMouseOver;
            Aller.MouseLeave += AllerMouseLeave;
            Aller.MouseLeftButtonDown += AllerMouseButtonDown;

            TexGyre.MouseEnter += TexGyreMouseOver;
            TexGyre.MouseLeave += TexGyreMouseLeave;
            TexGyre.MouseLeftButtonDown += TexGyreMouseButtonDown;

            Iconic.MouseEnter += IconicMouseOver;
            Iconic.MouseLeave += IconicMouseLeave;
            Iconic.MouseLeftButtonDown += IconicMouseButtonDown;

            FlatIcon.MouseEnter += FlatIconMouseOver;
            FlatIcon.MouseLeave += FlatIconMouseLeave;
            FlatIcon.MouseLeftButtonDown += FlatIconMouseButtonDown;

            Ionic.MouseEnter += IonicMouseOver;
            Ionic.MouseLeave += IonicMouseLeave;
            Ionic.MouseLeftButtonDown += IonicMouseButtonDown;

            #endregion
        }

        #endregion

        #region Add Mouseover events

        #region Aller

        private void AllerMouseOver(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseEnterColorEvent(255, 245, 245, 245, AllerBrush, false);
        }

        private void AllerMouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(AllerBrush, false);
        }

        private void AllerMouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(255, 245, 245, 245, AllerBrush, false);
        }

        #endregion

        #region TexGyre

        private void TexGyreMouseOver(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseEnterColorEvent(255, 245, 245, 245, TexGyreBrush, false);
        }

        private void TexGyreMouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(TexGyreBrush, false);
        }

        private void TexGyreMouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(255, 245, 245, 245, TexGyreBrush, false);
        }

        #endregion

        #region Iconic

        private void IconicMouseOver(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseEnterColorEvent(255, 245, 245, 245, IconicBrush, false);
        }

        private void IconicMouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(IconicBrush, false);
        }

        private void IconicMouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(255, 245, 245, 245, IconicBrush, false);
        }

        #endregion

        #region FlatIcon

        private void FlatIconMouseOver(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseEnterColorEvent(255, 245, 245, 245, FlatIconBrush, false);
        }

        private void FlatIconMouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(FlatIconBrush, false);
        }

        private void FlatIconMouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(255, 245, 245, 245, FlatIconBrush, false);
        }

        #endregion

        #region Ionic

        private void IonicMouseOver(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseEnterColorEvent(255, 245, 245, 245, IonicBrush, false);
        }

        private void IonicMouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(IonicBrush, false);
        }

        private void IonicMouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(255, 245, 245, 245, IonicBrush, false);
        }

        #endregion

        #endregion

        #endregion

        #region Hyperlink

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }

        #endregion

    }
}
