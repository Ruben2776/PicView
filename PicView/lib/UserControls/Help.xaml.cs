using PicView.lib;
using System.Windows;
using System.Windows.Controls;
using static PicView.lib.Helper;

namespace PicView.UserControls
{
    /// <summary>
    /// A userControl used to inform about the application
    /// </summary>
    public partial class Help : UserControl
    {
        #region Window Logic

        #region Constructor
        public Help()
        {
            InitializeComponent();

            Loaded += Help_Loaded;
        }
        #endregion

        #region Loaded

        private void Help_Loaded(object sender, RoutedEventArgs e)
        {
            #region Add Events

            #region CloseButton

            CloseButton.Click += (s,x) => Close(this);

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

            #endregion

        }

        #endregion

        #endregion
    }
}
