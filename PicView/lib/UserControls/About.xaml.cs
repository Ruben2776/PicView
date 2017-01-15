using PicView.lib;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
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
        public About()
        {
            InitializeComponent();

            Assembly assembly = Assembly.GetExecutingAssembly();
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            appVersion.Content = version + fvi.FileVersion;
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            Aller.MouseEnter += AllerMouseOver;
            Aller.MouseLeave += AllerMouseLeave;
            Aller.MouseLeftButtonDown += AllerMouseButtonDown;

            Cabin.MouseEnter += CabinMouseOver;
            Cabin.MouseLeave += CabinMouseLeave;
            Cabin.MouseLeftButtonDown += CabinMouseButtonDown;

            TexGyre.MouseEnter += TexGyreMouseOver;
            TexGyre.MouseLeave += TexGyreMouseLeave;
            TexGyre.MouseLeftButtonDown += TexGyreMouseButtonDown;

            Iconic.MouseEnter += IconicMouseOver;
            Iconic.MouseLeave += IconicMouseLeave;
            Iconic.MouseLeftButtonDown += IconicMouseButtonDown;

            FlatIcon.MouseEnter += FlatIconMouseOver;
            FlatIcon.MouseLeave += FlatIconMouseLeave;
            FlatIcon.MouseLeftButtonDown += FlatIconMouseButtonDown;

            ionic.MouseEnter += ionicMouseOver;
            ionic.MouseLeave += ionicMouseLeave;
            ionic.MouseLeftButtonDown += ionicMouseButtonDown;

        }

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

        #region Cabin

        private void CabinMouseOver(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseEnterColorEvent(255, 245, 245, 245, CabinBrush, false);
        }

        private void CabinMouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(CabinBrush, false);
        }

        private void CabinMouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(255, 245, 245, 245, CabinBrush, false);
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

        #region ionic

        private void ionicMouseOver(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseEnterColorEvent(255, 245, 245, 245, ionicBrush, false);
        }

        private void ionicMouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(ionicBrush, false);
        }

        private void ionicMouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(255, 245, 245, 245, ionicBrush, false);
        }

        #endregion

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }

        private void Close(object sender, RoutedEventArgs e)
        {
            Helper.Close(this);
        }
    }
}
