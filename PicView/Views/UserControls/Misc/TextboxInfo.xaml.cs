using System.Windows;
using System.Windows.Controls;

namespace PicView.Views.UserControls.Misc
{
    /// <summary>
    /// Interaction logic for TextboxInfo.xaml
    /// </summary>
    public partial class TextboxInfo : UserControl
    {
        public TextboxInfo(string valueDesc, string value, bool readOnly)
        {
            InitializeComponent();
            SetValues(valueDesc, value, readOnly);

            ValueCopy.TheButton.Click += delegate
            {
                Clipboard.SetText(ValueBox.Text);
            };
        }

        public void SetValues(string valueDesc, string value, bool readOnly)
        {
            ValueName.Text = string.IsNullOrEmpty(valueDesc) ? "?" : valueDesc;
            ValueBox.Text = string.IsNullOrEmpty(value) ? "" : value;
            ValueBox.IsReadOnly = readOnly;
        }
    }
}