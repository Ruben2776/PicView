using System.Windows;

namespace PicView.Views.UserControls.Misc;

public partial class TextBoxInfo
{
    public TextBoxInfo(string valueDesc, string value, bool readOnly)
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