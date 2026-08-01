using Avalonia.Controls;
using Avalonia.Controls.Metadata;

namespace PicView.Avalonia.CustomControls;

[PseudoClasses(Error)]
public class ValidationTextBox : FuncTextBox
{
    private const string Error = ":error";

    public void SetError(bool hasError)
    {
        PseudoClasses.Set(Error, hasError);
    }
}