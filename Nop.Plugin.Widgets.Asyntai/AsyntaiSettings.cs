using Nop.Core.Configuration;

namespace Nop.Plugin.Widgets.Asyntai;

/// <summary>
/// Represents the settings of the Asyntai chat widget
/// </summary>
public class AsyntaiSettings : ISettings
{
    /// <summary>
    /// Gets or sets the widget identifier taken from the Asyntai dashboard
    /// </summary>
    public string WidgetId { get; set; }

    /// <summary>
    /// Gets or sets the URL of the widget loader script
    /// </summary>
    public string ScriptUrl { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the name and the email address of
    /// the signed-in customer are passed to the assistant
    /// </summary>
    public bool PassCustomerDetails { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the widget is hidden on the
    /// checkout pages
    /// </summary>
    public bool HideOnCheckout { get; set; }

    /// <summary>
    /// Gets or sets the email address of the connected Asyntai account
    /// </summary>
    public string AccountEmail { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether Asyntai may read the products
    /// and pages of the store
    /// </summary>
    public bool FeedEnabled { get; set; }

    /// <summary>
    /// Gets or sets the secret that signs every feed request
    /// </summary>
    public string FeedToken { get; set; }

    /// <summary>
    /// Gets or sets the secret of a connect that has not finished yet
    /// </summary>
    public string PendingToken { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the connect that is in progress
    /// </summary>
    public string ConnectState { get; set; }

    /// <summary>
    /// Gets or sets when the connect in progress started, in Unix seconds
    /// </summary>
    public long ConnectStateAt { get; set; }

    /// <summary>
    /// Gets or sets the store address that was sent to Asyntai at connect
    /// </summary>
    public string SiteUrl { get; set; }
}
