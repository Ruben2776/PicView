using Avalonia.Controls;
using Avalonia.Controls.Metadata;

namespace PicView.Avalonia.CustomControls;

[PseudoClasses(Error, Empty)]
public class ValidationTextBox : FuncTextBox
{
    public const string Error = ":error";
    public const string Empty = ":empty";

    public void SetError(bool hasError)
    {
        PseudoClasses.Set(Error, hasError);
    }

    public void SetEmpty(bool isEmpty)
    {
        PseudoClasses.Set(Empty, isEmpty);
    }
}