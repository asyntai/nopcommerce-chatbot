namespace Nop.Plugin.Widgets.Asyntai;

/// <summary>
/// Represents plugin constants
/// </summary>
public static class AsyntaiDefaults
{
    /// <summary>
    /// Gets a plugin system name
    /// </summary>
    public static string SystemName => "Widgets.Asyntai";

    /// <summary>
    /// Gets the configuration route name
    /// </summary>
    public static string ConfigurationRouteName => "Plugin.Widgets.Asyntai.Configure";

    /// <summary>
    /// Gets the default URL of the chat widget loader
    /// </summary>
    public static string DefaultScriptUrl => "https://widget.asyntai.com/static/js/chat-widget.js";

    /// <summary>
    /// Gets the prefix of every localization resource of this plugin
    /// </summary>
    public static string LocaleResourcePrefix => "Plugins.Widgets.Asyntai";
}
