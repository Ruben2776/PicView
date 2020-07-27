using System.Windows.Controls;

namespace PicView.UILogic.UserControls
{
    /// <summary>
    /// Interaction logic for CustomTextBox.xaml
    /// </summary>
    public partial class CustomTextBox : UserControl
    {
        public CustomTextBox()
        {
            InitializeComponent();
        }

        public string Text
        {
            get => InnerTextBox.Text;
            set => InnerTextBox.Text = value;
        }

        public new bool IsFocused
        {
            get
            {
                return InnerTextBox.IsFocused;
            }
        }
    }
}