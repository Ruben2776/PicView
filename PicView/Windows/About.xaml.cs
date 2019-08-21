using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Windows.Navigation;
using static PicView.Fields;

namespace PicView.Windows
{
    /// <summary>
    /// About window
    /// </summary>
    public partial class About : Window
    {
        #region Window Logic

        public About()
        {
            InitializeComponent();

            // Get version
            Assembly assembly = Assembly.GetExecutingAssembly();
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            appVersion.Content += fvi.FileVersion;
        }
        private void Window_ContentRendered(object sender, EventArgs e)
        {
            #region Add events

            CloseButton.Click += (s, x) => Close();

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
            
            KeyDown += Keys;

            Aller.MouseEnter += AllerMouseOver;
            Aller.MouseLeave += AllerMouseLeave;
            Aller.PreviewMouseLeftButtonDown += AllerMouseButtonDown;

            TexGyre.MouseEnter += TexGyreMouseOver;
            TexGyre.MouseLeave += TexGyreMouseLeave;
            TexGyre.PreviewMouseLeftButtonDown += TexGyreMouseButtonDown;

            Iconic.MouseEnter += IconicMouseOver;
            Iconic.MouseLeave += IconicMouseLeave;
            Iconic.PreviewMouseLeftButtonDown += IconicMouseButtonDown;

            FlatIcon.MouseEnter += FlatIconMouseOver;
            FlatIcon.MouseLeave += FlatIconMouseLeave;
            FlatIcon.PreviewMouseLeftButtonDown += FlatIconMouseButtonDown;

            Ionic.MouseEnter += IonicMouseOver;
            Ionic.MouseLeave += IonicMouseLeave;
            Ionic.PreviewMouseLeftButtonDown += IonicMouseButtonDown;

            FontAwesome.MouseEnter += FontAwesomeMouseOver;
            FontAwesome.MouseLeave += FontAwesomeMouseLeave;
            FontAwesome.PreviewMouseLeftButtonDown += FontAwesomeMouseButtonDown;

            #endregion
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            Closing -= Window_Closing;
            e.Cancel = true;
            AnimationHelper.FadeWindow(this, 0, TimeSpan.FromSeconds(.5));
            FocusManager.SetFocusedElement(Application.Current.MainWindow, this);
        }

        #endregion

        #region Methods

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }

        private void Keys(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape ||
                e.Key == Key.Q && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control || e.Key == Key.F2)
            {
                Close();
            }
        }

        #endregion

        #region Interface

        #region Add Mouseover events

        #region Aller

        private void AllerMouseOver(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseEnterColorEvent(
                mainColor.A,
                mainColor.R,
                mainColor.G,
                mainColor.B,
                AllerBrush,
                false
            );
        }

        private void AllerMouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(AllerBrush, false);
        }

        private void AllerMouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(
                mainColor.A,
                mainColor.R,
                mainColor.G,
                mainColor.B,
                AllerBrush,
                false
            );
        }

        #endregion

        #region TexGyre

        private void TexGyreMouseOver(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseEnterColorEvent(
                mainColor.A,
                mainColor.R,
                mainColor.G,
                mainColor.B,
                TexGyreBrush,
                false
            );
        }

        private void TexGyreMouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(TexGyreBrush, false);
        }

        private void TexGyreMouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(
                mainColor.A,
                mainColor.R,
                mainColor.G,
                mainColor.B,
                TexGyreBrush,
                false
            );
        }

        #endregion

        #region Iconic

        private void IconicMouseOver(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseEnterColorEvent(
                mainColor.A,
                mainColor.R,
                mainColor.G,
                mainColor.B,
                IconicBrush,
                false
            );
        }

        private void IconicMouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(IconicBrush, false);
        }

        private void IconicMouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(
                mainColor.A,
                mainColor.R,
                mainColor.G,
                mainColor.B,
                IconicBrush,
                false
            );
        }

        #endregion

        #region FlatIcon

        private void FlatIconMouseOver(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseEnterColorEvent(
                mainColor.A,
                mainColor.R,
                mainColor.G,
                mainColor.B,
                FlatIconBrush,
                false
            );
        }

        private void FlatIconMouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(FlatIconBrush, false);
        }

        private void FlatIconMouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(
                mainColor.A,
                mainColor.R,
                mainColor.G,
                mainColor.B,
                FlatIconBrush,
                false
            );
        }

        #endregion

        #region Ionic

        private void IonicMouseOver(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseEnterColorEvent(
                mainColor.A,
                mainColor.R,
                mainColor.G,
                mainColor.B,
                IonicBrush,
                false
            );
        }

        private void IonicMouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(IonicBrush, false);
        }

        private void IonicMouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(
                mainColor.A,
                mainColor.R,
                mainColor.G,
                mainColor.B,
                IonicBrush,
                false
            );
        }

        #endregion

        #region FontAwesome

        private void FontAwesomeMouseOver(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseEnterColorEvent(
                mainColor.A,
                mainColor.R,
                mainColor.G,
                mainColor.B,
                FontAwesomeBrush,
                false
            );
        }

        private void FontAwesomeMouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(FontAwesomeBrush, false);
        }

        private void FontAwesomeMouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(
                mainColor.A,
                mainColor.R,
                mainColor.G,
                mainColor.B,
                FontAwesomeBrush,
                false
            );
        }

        #endregion

        #endregion

        #endregion

    }
}