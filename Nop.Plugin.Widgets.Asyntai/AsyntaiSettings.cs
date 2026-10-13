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
}
