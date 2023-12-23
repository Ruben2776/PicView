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
        ValueName.Text = string.IsNullOrEmpty(valueDesc) ? "?" : valueDesc;
        ValueBox.Text = string.IsNullOrEmpty(value) ? "" : value;
        ValueBox.IsReadOnly = readOnly;
    }

    public void UpdateLanguage()
    {
        ValueName.Text = TranslationHelper.GetTranslation("Filename");
        ValueCopy.ToolTip = TranslationHelper.GetTranslation("Copy");
    }
}