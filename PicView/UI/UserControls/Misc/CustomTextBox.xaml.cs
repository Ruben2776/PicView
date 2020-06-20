using System.Windows.Controls;

namespace PicView.UI.UserControls
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

        public string Text {
            get => Bar.Text;
            set => Bar.Text = value;
        }

        public new bool IsFocused {
            get {
                return Bar.IsFocused; 
            }
        }
    }
}