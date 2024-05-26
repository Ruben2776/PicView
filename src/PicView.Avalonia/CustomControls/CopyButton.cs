using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace PicView.Avalonia.CustomControls
{
    public class CopyButton : Button
    {
        public static readonly AvaloniaProperty<string> CopyTextProperty =
            AvaloniaProperty.Register<CopyButton, string>(nameof(CopyText));
        
        protected override Type StyleKeyOverride => typeof(Button);
        
        public string CopyText
        {
            get => (string)GetValue(CopyTextProperty);
            set => SetValue(CopyTextProperty, value);
        }

        public CopyButton()
        {
            Click += CopyButton_OnClick;
        }

        private async void CopyButton_OnClick(object? sender, RoutedEventArgs args)
        {
            if (string.IsNullOrWhiteSpace(CopyText))
            {
                return;
            }

            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel?.Clipboard != null)
            {
                await topLevel.Clipboard.SetTextAsync(CopyText);
            }
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);
            Click -= CopyButton_OnClick;  // Ensure unsubscription from event
        }
    }
}