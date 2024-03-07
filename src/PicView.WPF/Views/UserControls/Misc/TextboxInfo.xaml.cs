using System.Windows;
using PicView.Core.Localization;

namespace PicView.WPF.Views.UserControls.Misc;

public partial class TextBoxInfo
{
    public TextBoxInfo(string valueDesc, string value, bool readOnly)
    {
        InitializeComponent();
        SetValues(valueDesc, value, readOnly);

        ValueCopy.TheButton.Click += delegate { Clipboard.SetText(ValueBox.Text); };
        UpdateLanguage();
    }

    public void SetValues(string valueDesc, string value, bool readOnly)
    {
        if (!string.IsNullOrEmpty(valueDesc))
        {
            ValueName.Text = valueDesc;
        }

        if (!string.IsNullOrEmpty(value))
        {
            ValueBox.Text = value;
        }

        ValueBox.IsReadOnly = readOnly;
    }

    public void UpdateLanguage()
    {
        ValueCopy.ToolTip = TranslationHelper.GetTranslation("Copy");
    }
}