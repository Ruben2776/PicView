namespace PicView.Core.Config;

public sealed class KeybindingWindowConfig()
    : RememberedWindowConfig<KeybindingWindowConfig.KeybindingWindowProperties>("KeybindingWindowConfig.json")
{
    public class KeybindingWindowProperties : RememberedWindowProperties;
}
