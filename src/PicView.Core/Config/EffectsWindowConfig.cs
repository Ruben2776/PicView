namespace PicView.Core.Config;

public sealed class EffectsWindowConfig() : RememberedWindowConfig<EffectsWindowConfig.EffectsWindowProperties>("EffectsWindow.json")
{
    public class EffectsWindowProperties : RememberedWindowProperties;
}
