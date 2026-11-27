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

    /// <summary>
    /// Gets the platform name sent to Asyntai when the store connects
    /// </summary>
    public static string Platform => "nopcommerce";

    /// <summary>
    /// Gets the route name of the product feed
    /// </summary>
    public static string ProductFeedRouteName => "Plugin.Widgets.Asyntai.ProductFeed";

    /// <summary>
    /// Gets the route name of the page feed
    /// </summary>
    public static string PageFeedRouteName => "Plugin.Widgets.Asyntai.PageFeed";

    /// <summary>
    /// Gets the path of the product feed, relative to the store URL
    /// </summary>
    public static string ProductFeedPath => "asyntai-feed/products";

    /// <summary>
    /// Gets the path of the page feed, relative to the store URL
    /// </summary>
    public static string PageFeedPath => "asyntai-feed/pages";

    /// <summary>
    /// Gets how far the clock of a signed feed request may drift, in seconds
    /// </summary>
    public static int FeedClockSkew => 300;

    /// <summary>
    /// Gets the number of products on one feed page when none is asked for
    /// </summary>
    public static int FeedPageSize => 100;

    /// <summary>
    /// Gets the largest number of products on one feed page
    /// </summary>
    public static int FeedMaxPageSize => 250;

    /// <summary>
    /// Gets the largest number of products in one lookup by id
    /// </summary>
    public static int FeedMaxIds => 50;

    /// <summary>
    /// Gets how long a connect handshake stays valid, in seconds
    /// </summary>
    public static int ConnectStateTtl => 900;

    /// <summary>
    /// Gets the Asyntai address. The ASYNTAI_CHATBOT_ORIGIN environment
    /// variable can point it at a test server.
    /// </summary>
    public static string Origin
    {
        get
        {
            var value = Environment.GetEnvironmentVariable("ASYNTAI_CHATBOT_ORIGIN");
            if (string.IsNullOrWhiteSpace(value)
                || !Uri.TryCreate(value.Trim(), UriKind.Absolute, out var parsed)
                || (parsed.Scheme != Uri.UriSchemeHttps && parsed.Scheme != Uri.UriSchemeHttp))
            {
                return "https://asyntai.com";
            }

            return value.Trim().TrimEnd('/');
        }
    }
}
