using PicView.UILogic;
using System.Windows;
using System.Windows.Interop;
using static PicView.SystemIntegration.NativeMethods;
using System.Windows.Media.Effects;
using System.Windows.Media.Animation;
using System;

namespace PicView.Views.Windows
{
    /// <summary>
    /// Interaction logic for FileAssociationsWindow.xaml
    /// </summary>
    public partial class FileAssociationsWindow : Window
    {
        public FileAssociationsWindow()
        {
            Title = Application.Current.Resources["SetAsDefualt"] + " - PicView";
            Width = ConfigureWindows.GetSettingsWindow.ActualWidth;
            Height = ConfigureWindows.GetSettingsWindow.ActualHeight;
            InitializeComponent();

            ContentRendered += delegate
            {
                // Hide from alt tab
                var helper = new WindowInteropHelper(this).Handle;
                _ = SetWindowLong(helper, GWL_EX_STYLE, (GetWindowLong(helper, GWL_EX_STYLE) | WS_EX_TOOLWINDOW) & ~WS_EX_APPWINDOW);

                CloseButton.TheButton.Click += (_, _) => HideLogic();
            };      

            KeyDown += (_, e) => 
            {
                if (e.Key == System.Windows.Input.Key.Escape)
                {
                    HideLogic();
                }
            };
        }

        internal void HideLogic()
        {
            var da = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromSeconds(.3)
            };

            da.Completed += delegate
            {
                Hide();
            };

            ConfigureWindows.GetSettingsWindow.Effect = null;
            BeginAnimation(OpacityProperty, da);
            ConfigureWindows.GetSettingsWindow.Focus();
        }
    }
}
