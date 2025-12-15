using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
using PicView.Avalonia.CustomControls;

namespace PicView.Avalonia.Views.UC.PopUps;

public partial class MessageDialog : AnimatedPopUp
{
    public MessageDialog(string titleText, string messageText)
    {
        InitializeComponent();

        CloseButton.Click += async delegate
        {
            await AnimatedClosing();
        };
        
        TitleText.Text = titleText;
        MessageText.Text = messageText;

        Focus();
    }
}
