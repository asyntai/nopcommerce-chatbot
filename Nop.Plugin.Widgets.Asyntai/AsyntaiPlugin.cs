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

    #region Utilities

    /// <summary>
    /// Every text of this plugin
    /// </summary>
    protected static Dictionary<string, string> GetLocaleResources()
    {
        return new Dictionary<string, string>
        {
            ["Plugins.Widgets.Asyntai.WidgetId"] = "Widget ID",
            ["Plugins.Widgets.Asyntai.WidgetId.Hint"] = "The widget ID of your Asyntai assistant. The Get started button fills it in. You can also copy it from the Asyntai dashboard under Setup & Integration.",
            ["Plugins.Widgets.Asyntai.WidgetId.Required"] = "The widget ID is required.",
            ["Plugins.Widgets.Asyntai.ScriptUrl"] = "Script URL",
            ["Plugins.Widgets.Asyntai.ScriptUrl.Hint"] = "The address of the Asyntai loader script. Keep the default value unless Asyntai support asks you to change it.",
            ["Plugins.Widgets.Asyntai.ScriptUrl.Required"] = "The script URL is required.",
            ["Plugins.Widgets.Asyntai.ScriptUrl.Invalid"] = "The script URL must be an absolute HTTPS address.",
            ["Plugins.Widgets.Asyntai.PassCustomerDetails"] = "Pass customer details",
            ["Plugins.Widgets.Asyntai.PassCustomerDetails.Hint"] = "Check to send the name and the email address of the signed-in customer to the assistant, so it can greet them and open their orders faster. Guests send nothing.",
            ["Plugins.Widgets.Asyntai.HideOnCheckout"] = "Hide on checkout",
            ["Plugins.Widgets.Asyntai.HideOnCheckout.Hint"] = "Check to hide the chat bubble on the checkout pages.",
            ["Plugins.Widgets.Asyntai.Instructions"] = "<p>Asyntai answers your shoppers from your own products and pages, day and night, in 36 languages.<br /><br />Press <b>Get started</b> above, then sign in to Asyntai or create a free account in the window that opens. The chat bubble then appears in the bottom corner of your store.<br /><br />You can also connect by hand: copy the <b>widget ID</b> from <b>Setup &amp; Integration</b> in the <a href=\"https://asyntai.com/dashboard\" target=\"_blank\">Asyntai dashboard</a>, paste it into the box below and click <b>Save</b>.<br /><br />Need help? Write to <a href=\"mailto:hello@asyntai.com\">hello@asyntai.com</a>.</p>",
            ["Plugins.Widgets.Asyntai.Connect.Intro"] = "Connect this store to Asyntai to put the AI chatbot on it.",
            ["Plugins.Widgets.Asyntai.Connect.Button"] = "Get started",
            ["Plugins.Widgets.Asyntai.Connect.Share"] = "Let the chatbot read your products and pages",
            ["Plugins.Widgets.Asyntai.Connect.Share.Hint"] = "The chatbot then answers from your published products, categories and topic pages, with current prices. Asyntai reads them again every day. Only what a visitor of your store can see is shared.",
            ["Plugins.Widgets.Asyntai.Connect.Waiting"] = "Finish the sign-in in the window that opened. This page updates by itself.",
            ["Plugins.Widgets.Asyntai.Connect.PopupBlocked"] = "Your browser blocked the sign-in window. Allow pop-ups for this page and try again.",
            ["Plugins.Widgets.Asyntai.Connect.Unreachable"] = "Could not reach Asyntai. Check that this store can make outgoing HTTPS requests, then try again.",
            ["Plugins.Widgets.Asyntai.Connect.Unknown"] = "This connection attempt is not known. Press Get started again.",
            ["Plugins.Widgets.Asyntai.Connect.Expired"] = "This connection attempt has expired. Press Get started again.",
            ["Plugins.Widgets.Asyntai.Connect.Done"] = "Connected. The chatbot is now on your store.",
            ["Plugins.Widgets.Asyntai.Connected"] = "Connected to Asyntai",
            ["Plugins.Widgets.Asyntai.OpenDashboard"] = "Open Asyntai dashboard",
            ["Plugins.Widgets.Asyntai.Disconnect"] = "Disconnect",
            ["Plugins.Widgets.Asyntai.Disconnect.Confirm"] = "Disconnect this store from Asyntai? The chat bubble disappears from your store.",
            ["Plugins.Widgets.Asyntai.Share.NeedsConnect"] = "To share your products and pages, sign in to Asyntai once more in the window that opened.",
            ["Plugins.Widgets.Asyntai.Sync.Reading"] = "Asyntai is reading your products and pages.",
            ["Plugins.Widgets.Asyntai.Sync.Count"] = "Shared with the chatbot: {0} items.",
            ["Plugins.Widgets.Asyntai.Sync.Error"] = "The last read failed:"
        };
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

        await _localizationService.AddOrUpdateLocaleResourceAsync(GetLocaleResources());

        await base.InstallAsync();
    }

    /// <summary>
    /// Update plugin
    /// </summary>
    /// <param name="currentVersion">Current version of plugin</param>
    /// <param name="targetVersion">New version of plugin</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    public override async Task UpdateAsync(string currentVersion, string targetVersion)
    {
        //the texts of the connect button and the feed came with 1.10
        await _localizationService.AddOrUpdateLocaleResourceAsync(GetLocaleResources());

        await base.UpdateAsync(currentVersion, targetVersion);
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
