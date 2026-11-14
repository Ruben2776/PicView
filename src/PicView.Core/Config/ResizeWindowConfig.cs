namespace PicView.Core.Config;

public sealed class ResizeWindowConfig() : RememberedWindowConfig<ResizeWindowConfig.ResizeWindowProperties>("ResizeWindow.json")
{
    public class ResizeWindowProperties : RememberedWindowProperties;
}
