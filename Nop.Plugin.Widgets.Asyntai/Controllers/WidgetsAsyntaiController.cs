using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Plugin.Widgets.Asyntai.Models;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Messages;
using Nop.Services.Security;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;

namespace Nop.Plugin.Widgets.Asyntai.Controllers;

[Area(AreaNames.ADMIN)]
[AuthorizeAdmin]
[AutoValidateAntiforgeryToken]
public class WidgetsAsyntaiController : BasePluginController
{
    #region Fields

    protected readonly ILocalizationService _localizationService;
    protected readonly INotificationService _notificationService;
    protected readonly ISettingService _settingService;
    protected readonly IStoreContext _storeContext;

    #endregion

    #region Ctor

    public WidgetsAsyntaiController(ILocalizationService localizationService,
        INotificationService notificationService,
        ISettingService settingService,
        IStoreContext storeContext)
    {
        _localizationService = localizationService;
        _notificationService = notificationService;
        _settingService = settingService;
        _storeContext = storeContext;
    }

    #endregion

    #region Methods

    [CheckPermission(StandardPermission.Configuration.MANAGE_WIDGETS)]
    public async Task<IActionResult> Configure()
    {
        var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
        var settings = await _settingService.LoadSettingAsync<AsyntaiSettings>(storeScope);

        var model = new ConfigurationModel
        {
            WidgetId = settings.WidgetId,
            ScriptUrl = string.IsNullOrWhiteSpace(settings.ScriptUrl)
                ? AsyntaiDefaults.DefaultScriptUrl
                : settings.ScriptUrl,
            PassCustomerDetails = settings.PassCustomerDetails,
            HideOnCheckout = settings.HideOnCheckout,
            ActiveStoreScopeConfiguration = storeScope
        };

        if (storeScope > 0)
        {
            model.WidgetId_OverrideForStore = await _settingService.SettingExistsAsync(settings, x => x.WidgetId, storeScope);
            model.ScriptUrl_OverrideForStore = await _settingService.SettingExistsAsync(settings, x => x.ScriptUrl, storeScope);
            model.PassCustomerDetails_OverrideForStore = await _settingService.SettingExistsAsync(settings, x => x.PassCustomerDetails, storeScope);
            model.HideOnCheckout_OverrideForStore = await _settingService.SettingExistsAsync(settings, x => x.HideOnCheckout, storeScope);
        }

        return View("~/Plugins/Widgets.Asyntai/Views/Configure.cshtml", model);
    }

    [HttpPost]
    [CheckPermission(StandardPermission.Configuration.MANAGE_WIDGETS)]
    public async Task<IActionResult> Configure(ConfigurationModel model)
    {
        var widgetId = model.WidgetId?.Trim() ?? string.Empty;
        var scriptUrl = model.ScriptUrl?.Trim() ?? string.Empty;

        if (string.IsNullOrEmpty(widgetId))
            ModelState.AddModelError(nameof(model.WidgetId),
                await _localizationService.GetResourceAsync("Plugins.Widgets.Asyntai.WidgetId.Required"));

        if (string.IsNullOrEmpty(scriptUrl))
            ModelState.AddModelError(nameof(model.ScriptUrl),
                await _localizationService.GetResourceAsync("Plugins.Widgets.Asyntai.ScriptUrl.Required"));
        else if (!Uri.TryCreate(scriptUrl, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
        {
            //an http script would be blocked as mixed content on an https store,
            //and the bubble would silently never appear
            ModelState.AddModelError(nameof(model.ScriptUrl),
                await _localizationService.GetResourceAsync("Plugins.Widgets.Asyntai.ScriptUrl.Invalid"));
        }

        if (!ModelState.IsValid)
            return await Configure();

        var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
        var settings = await _settingService.LoadSettingAsync<AsyntaiSettings>(storeScope);

        settings.WidgetId = widgetId;
        settings.ScriptUrl = scriptUrl;
        settings.PassCustomerDetails = model.PassCustomerDetails;
        settings.HideOnCheckout = model.HideOnCheckout;

        await _settingService.SaveSettingOverridablePerStoreAsync(settings, x => x.WidgetId, model.WidgetId_OverrideForStore, storeScope, false);
        await _settingService.SaveSettingOverridablePerStoreAsync(settings, x => x.ScriptUrl, model.ScriptUrl_OverrideForStore, storeScope, false);
        await _settingService.SaveSettingOverridablePerStoreAsync(settings, x => x.PassCustomerDetails, model.PassCustomerDetails_OverrideForStore, storeScope, false);
        await _settingService.SaveSettingOverridablePerStoreAsync(settings, x => x.HideOnCheckout, model.HideOnCheckout_OverrideForStore, storeScope, false);

        await _settingService.ClearCacheAsync();

        _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Admin.Plugins.Saved"));

        return await Configure();
    }

    #endregion
}
