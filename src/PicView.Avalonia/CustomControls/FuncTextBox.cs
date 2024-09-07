using Avalonia.Controls;

namespace PicView.Avalonia.CustomControls;

public class FuncTextBox : TextBox
{
    protected override Type StyleKeyOverride => typeof(TextBox);

    // TODO: Add contextmenu functions "Select all" "Copy" "Paste" "Cut" "Delete" and translations
    
    // TODO: Disable caret and border in read only mode
}