namespace PicView.WPF.Views.UserControls.Misc;

public partial class CustomTextBox
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

    public new bool IsFocused => InnerTextBox.IsFocused;
}