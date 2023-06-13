using PicView.ProcessHandling;

namespace PicView.Views.UserControls.Misc;

public partial class LinkTextBox
{
    public LinkTextBox()
    {
        InitializeComponent();
        linkButton.TheButton.Click += (_, _) => ProcessLogic.Hyperlink_RequestNavigate(ValueBox.Text);
    }

    public LinkTextBox(string url, string name)
    {
        InitializeComponent();
        SetURL(url, name);
        linkButton.TheButton.Click += (_, _) => ProcessLogic.Hyperlink_RequestNavigate(ValueBox.Text);
    }

    // ReSharper disable once InconsistentNaming
    public void SetURL(string url, string name)
    {
        ValueBox.Text = url;
        linkButton.ToolTip = url;
        ValueName.Text = name;
    }
}