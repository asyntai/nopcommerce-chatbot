using Nop.Core.Domain.Cms;
using Nop.Plugin.Widgets.Asyntai.Components;
using Nop.Services.Cms;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Plugins;
using Nop.Web.Framework.Infrastructure;
using Nop.Web.Framework.Mvc.Routing;

namespace Nop.Plugin.Widgets.Asyntai;

/// <summary>
/// Asyntai AI chat widget plugin
/// </summary>
public class AsyntaiPlugin : BasePlugin, IWidgetPlugin
{
    #region Fields

    protected readonly ILocalizationService _localizationService;
    protected readonly INopUrlHelper _nopUrlHelper;
    protected readonly ISettingService _settingService;
    protected readonly WidgetSettings _widgetSettings;

    #endregion

    #region Ctor

    public AsyntaiPlugin(ILocalizationService localizationService,
        INopUrlHelper nopUrlHelper,
        ISettingService settingService,
        WidgetSettings widgetSettings)
    {
        _localizationService = localizationService;
        _nopUrlHelper = nopUrlHelper;
        _settingService = settingService;
        _widgetSettings = widgetSettings;
    }

    #endregion

    #region Methods

    /// <summary>
    /// Gets widget zones where this widget should be rendered
    /// </summary>
    /// <remarks>
    /// The script goes to the end of the body rather than the head. The loader
    /// waits for the load event by itself, and a chat panel that is written
    /// last cannot delay the first paint of the store.
    /// </remarks>
    public Task<IList<string>> GetWidgetZonesAsync()
    {
        return Task.FromResult<IList<string>>(new List<string>
        {
            PublicWidgetZones.BodyEndHtmlTagBefore
        });
    }

    /// <summary>
    /// Gets a configuration page URL
    /// </summary>
    public override string GetConfigurationPageUrl()
    {
        return _nopUrlHelper.RouteUrl(AsyntaiDefaults.ConfigurationRouteName);
    }

    /// <summary>
    /// Gets a type of a view component for displaying widget
    /// </summary>
    /// <param name="widgetZone">Name of the widget zone</param>
    /// <returns>View component type</returns>
    public Type GetWidgetViewComponent(string widgetZone)
    {
        ArgumentNullException.ThrowIfNull(widgetZone);

        if (widgetZone.Equals(PublicWidgetZones.BodyEndHtmlTagBefore))
            return typeof(WidgetsAsyntaiViewComponent);

        return null;
    }

    /// <summary>
    /// Install plugin
    /// </summary>
    /// <returns>A task that represents the asynchronous operation</returns>
    public override async Task InstallAsync()
    {
        var settings = new AsyntaiSettings
        {
            WidgetId = string.Empty,
            ScriptUrl = AsyntaiDefaults.DefaultScriptUrl,
            PassCustomerDetails = false,
            HideOnCheckout = false
        };
        await _settingService.SaveSettingAsync(settings);

        if (!_widgetSettings.ActiveWidgetSystemNames.Contains(AsyntaiDefaults.SystemName))
        {
            _widgetSettings.ActiveWidgetSystemNames.Add(AsyntaiDefaults.SystemName);
            await _settingService.SaveSettingAsync(_widgetSettings);
        }

        await _localizationService.AddOrUpdateLocaleResourceAsync(new Dictionary<string, string>
        {
            ["Plugins.Widgets.Asyntai.WidgetId"] = "Widget ID",
            ["Plugins.Widgets.Asyntai.WidgetId.Hint"] = "Enter the widget ID of your Asyntai assistant. You find it in the Asyntai dashboard under Setup & Integration.",
            ["Plugins.Widgets.Asyntai.WidgetId.Required"] = "The widget ID is required.",
            ["Plugins.Widgets.Asyntai.ScriptUrl"] = "Script URL",
            ["Plugins.Widgets.Asyntai.ScriptUrl.Hint"] = "The address of the Asyntai loader script. Keep the default value unless Asyntai support asks you to change it.",
            ["Plugins.Widgets.Asyntai.ScriptUrl.Required"] = "The script URL is required.",
            ["Plugins.Widgets.Asyntai.ScriptUrl.Invalid"] = "The script URL must be an absolute HTTPS address.",
            ["Plugins.Widgets.Asyntai.PassCustomerDetails"] = "Pass customer details",
            ["Plugins.Widgets.Asyntai.PassCustomerDetails.Hint"] = "Check to send the name and the email address of the signed-in customer to the assistant, so it can greet them and open their orders faster. Guests send nothing.",
            ["Plugins.Widgets.Asyntai.HideOnCheckout"] = "Hide on checkout",
            ["Plugins.Widgets.Asyntai.HideOnCheckout.Hint"] = "Check to hide the chat bubble on the checkout pages.",
            ["Plugins.Widgets.Asyntai.Instructions"] = "<p>Asyntai answers your shoppers from your own products and pages, day and night, in 36 languages.<br /><br />Follow these steps:<br /><ul><li><a href=\"https://asyntai.com/\" target=\"_blank\">Create an Asyntai account</a> and add your store address. Asyntai reads your pages and builds the assistant.</li><li>Open <b>Setup &amp; Integration</b> in the Asyntai dashboard and copy the <b>widget ID</b>. It looks like <b>asyntai_0123456789ab</b>.</li><li>Paste it into the <b>Widget ID</b> box below and click <b>Save</b>.</li><li>Open your store. The chat bubble is in the bottom corner.</li></ul>Need help? Write to <a href=\"mailto:hello@asyntai.com\">hello@asyntai.com</a>.</p>"
        });

        await base.InstallAsync();
    }

    /// <summary>
    /// Uninstall plugin
    /// </summary>
    /// <returns>A task that represents the asynchronous operation</returns>
    public override async Task UninstallAsync()
    {
        if (_widgetSettings.ActiveWidgetSystemNames.Contains(AsyntaiDefaults.SystemName))
        {
            _widgetSettings.ActiveWidgetSystemNames.Remove(AsyntaiDefaults.SystemName);
            await _settingService.SaveSettingAsync(_widgetSettings);
        }
        await _settingService.DeleteSettingAsync<AsyntaiSettings>();

        await _localizationService.DeleteLocaleResourcesAsync(AsyntaiDefaults.LocaleResourcePrefix);

        await base.UninstallAsync();
    }

    #endregion

    #region Properties

    /// <summary>
    /// Gets a value indicating whether to hide this plugin on the widget list page in the admin area
    /// </summary>
    public bool HideInWidgetList => false;

    #endregion
}
