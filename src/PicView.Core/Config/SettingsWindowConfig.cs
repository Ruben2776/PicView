namespace PicView.Core.Config;

public sealed class SettingsWindowConfig()
    : RememberedWindowConfig<SettingsWindowConfig.SettingsWindowProperties>("SettingsWindow.json")
{
    public class SettingsWindowProperties : RememberedWindowProperties
    {
        public int LastTab { get; set; }
    }
}
