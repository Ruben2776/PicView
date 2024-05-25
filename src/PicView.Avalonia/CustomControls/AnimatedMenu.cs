using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia;
using PicView.Avalonia.Helpers;
using ReactiveUI;

namespace PicView.Avalonia.CustomControls
{
    public class AnimatedMenu : UserControl
    {
        public static readonly AvaloniaProperty<bool> IsOpenProperty =
            AvaloniaProperty.Register<AnimatedMenu, bool>(nameof(IsOpen));

        public bool IsOpen
        {
            get => (bool)(GetValue(IsOpenProperty) ?? false);
            set => SetValue(IsOpenProperty, value);
        }

        protected AnimatedMenu()
        {
            this.WhenAnyValue(x => x.IsOpen)
                .Select(async x =>
                {
                    await DoAnimation(x);
                })
                .Subscribe();
        }

        private async Task DoAnimation(bool isOpen)
        {
            var from = isOpen ? 0 : 1;
            var to = isOpen ? 1 : 0;
            var anim = AnimationsHelper.OpacityAnimation(from, to, 0.3);
            await anim.RunAsync(this);
        }
    }
}