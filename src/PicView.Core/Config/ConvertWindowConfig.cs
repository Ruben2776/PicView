namespace PicView.Core.Config;

public sealed class ConvertWindowConfig() : RememberedWindowConfig<ConvertWindowConfig.ConvertWindowProperties>("ConvertWindow.json")
{
    public class ConvertWindowProperties : RememberedWindowProperties;
}
