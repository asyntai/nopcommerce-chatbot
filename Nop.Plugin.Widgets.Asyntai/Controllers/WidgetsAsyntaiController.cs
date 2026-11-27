using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Plugin.Widgets.Asyntai.Models;
using Nop.Plugin.Widgets.Asyntai.Services;
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

    private static readonly Regex SiteIdPattern = new("^[A-Za-z0-9_-]{6,64}$");

    protected readonly ILocalizationService _localizationService;
    protected readonly INotificationService _notificationService;
    protected readonly ISettingService _settingService;
    protected readonly IStoreContext _storeContext;
    protected readonly IWorkContext _workContext;

    #endregion

    #region Ctor

    public WidgetsAsyntaiController(ILocalizationService localizationService,
        INotificationService notificationService,
        ISettingService settingService,
        IStoreContext storeContext,
        IWorkContext workContext)
    {
        _localizationService = localizationService;
        _notificationService = notificationService;
        _settingService = settingService;
        _storeContext = storeContext;
        _workContext = workContext;
    }

    #endregion

    #region Utilities

    /// <summary>
    /// The public address of the current store
    /// </summary>
    protected virtual async Task<string> GetSiteUrlAsync()
    {
        var store = await _storeContext.GetCurrentStoreAsync();
        var url = AsyntaiFeedService.StoreUrl(store);
        if (Uri.TryCreate(url, UriKind.Absolute, out var parsed)
            && (parsed.Scheme == Uri.UriSchemeHttps || parsed.Scheme == Uri.UriSchemeHttp))
        {
            return url;
        }

        return Request.Scheme + "://" + Request.Host.Value;
    }

    /// <summary>
    /// The connection settings. They are always stored for all stores, so the
    /// feed token is one value and never printed into a page.
    /// </summary>
    protected virtual async Task<AsyntaiSettings> LoadAsync()
    {
        return await _settingService.LoadSettingAsync<AsyntaiSettings>();
    }

    protected virtual async Task SaveAsync(AsyntaiSettings settings)
    {
        await _settingService.SaveSettingAsync(settings);
        await _settingService.ClearCacheAsync();
    }

    protected virtual async Task<ConfigurationModel> PrepareModelAsync()
    {
        var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
        var settings = await _settingService.LoadSettingAsync<AsyntaiSettings>(storeScope);
        var shared = await LoadAsync();

        var model = new ConfigurationModel
        {
            WidgetId = settings.WidgetId,
            ScriptUrl = string.IsNullOrWhiteSpace(settings.ScriptUrl)
                ? AsyntaiDefaults.DefaultScriptUrl
                : settings.ScriptUrl,
            PassCustomerDetails = settings.PassCustomerDetails,
            HideOnCheckout = settings.HideOnCheckout,
            ActiveStoreScopeConfiguration = storeScope,
            Connected = !string.IsNullOrWhiteSpace(shared.WidgetId),
            AccountEmail = shared.AccountEmail ?? string.Empty,
            Sharing = shared.FeedEnabled && !string.IsNullOrEmpty(shared.FeedToken),
            HasToken = !string.IsNullOrEmpty(shared.FeedToken),
            DashboardUrl = AsyntaiDefaults.Origin + "/dashboard"
        };

        if (storeScope > 0)
        {
            model.WidgetId_OverrideForStore = await _settingService.SettingExistsAsync(settings, x => x.WidgetId, storeScope);
            model.ScriptUrl_OverrideForStore = await _settingService.SettingExistsAsync(settings, x => x.ScriptUrl, storeScope);
            model.PassCustomerDetails_OverrideForStore = await _settingService.SettingExistsAsync(settings, x => x.PassCustomerDetails, storeScope);
            model.HideOnCheckout_OverrideForStore = await _settingService.SettingExistsAsync(settings, x => x.HideOnCheckout, storeScope);
        }

        return model;
    }

    #endregion

    #region Methods

    [CheckPermission(StandardPermission.Configuration.MANAGE_WIDGETS)]
    public async Task<IActionResult> Configure()
    {
        var model = await PrepareModelAsync();

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

        //only these four are saved here, so the connection and its feed token
        //are never touched by this form
        await _settingService.SaveSettingOverridablePerStoreAsync(settings, x => x.WidgetId, model.WidgetId_OverrideForStore, storeScope, false);
        await _settingService.SaveSettingOverridablePerStoreAsync(settings, x => x.ScriptUrl, model.ScriptUrl_OverrideForStore, storeScope, false);
        await _settingService.SaveSettingOverridablePerStoreAsync(settings, x => x.PassCustomerDetails, model.PassCustomerDetails_OverrideForStore, storeScope, false);
        await _settingService.SaveSettingOverridablePerStoreAsync(settings, x => x.HideOnCheckout, model.HideOnCheckout_OverrideForStore, storeScope, false);

        await _settingService.ClearCacheAsync();

        _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Admin.Plugins.Saved"));

        return await Configure();
    }

    /// <summary>
    /// Start a connect. With sharing ticked, a fresh feed token is parked at
    /// Asyntai first.
    /// </summary>
    [HttpPost]
    [CheckPermission(StandardPermission.Configuration.MANAGE_WIDGETS)]
    public async Task<IActionResult> Prepare(bool share)
    {
        var state = AsyntaiDefaults.Platform + "_" + Convert.ToHexString(RandomNumberGenerator.GetBytes(12)).ToLowerInvariant();
        var siteUrl = await GetSiteUrlAsync();

        var pending = string.Empty;
        if (share)
        {
            pending = Convert.ToHexString(RandomNumberGenerator.GetBytes(24)).ToLowerInvariant();
            var staged = await AsyntaiRemote.PostJsonAsync("/api/v1/wp-search/stage/", new Dictionary<string, string>
            {
                ["state"] = state,
                ["platform"] = AsyntaiDefaults.Platform,
                ["feed_protocol"] = "store_feed_v1",
                //a chatbot install never unlocks the paid AI Search bar
                ["product"] = "chat",
                ["site_url"] = siteUrl,
                ["feed_url"] = siteUrl + "/" + AsyntaiDefaults.ProductFeedPath,
                ["feed_token"] = pending
            });
            if (staged == null)
                return Json(new { ok = false, error = await _localizationService.GetResourceAsync("Plugins.Widgets.Asyntai.Connect.Unreachable") });
        }

        var settings = await LoadAsync();
        settings.PendingToken = pending;
        settings.ConnectState = state;
        settings.ConnectStateAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        if (share)
            settings.SiteUrl = siteUrl;
        await SaveAsync(settings);

        var customer = await _workContext.GetCurrentCustomerAsync();
        var query = new Dictionary<string, string>
        {
            ["state"] = state,
            ["platform"] = AsyntaiDefaults.Platform,
            ["product"] = "chat",
            ["site_url"] = siteUrl,
            ["wp_email"] = customer?.Email ?? string.Empty
        };
        var url = AsyntaiDefaults.Origin + "/wp-auth?"
            + string.Join("&", query.Select(p => Uri.EscapeDataString(p.Key) + "=" + Uri.EscapeDataString(p.Value)));

        return Json(new { ok = true, state, url });
    }

    /// <summary>
    /// Has the owner finished signing in?
    /// </summary>
    [HttpPost]
    [CheckPermission(StandardPermission.Configuration.MANAGE_WIDGETS)]
    public async Task<IActionResult> Poll(string state)
    {
        state = state?.Trim() ?? string.Empty;
        var settings = await LoadAsync();

        //only the handshake this store just started is ever polled
        if (state.Length == 0 || state != settings.ConnectState)
            return Json(new { ok = false, error = await _localizationService.GetResourceAsync("Plugins.Widgets.Asyntai.Connect.Unknown") });

        if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() - settings.ConnectStateAt > AsyntaiDefaults.ConnectStateTtl)
            return Json(new { ok = false, error = await _localizationService.GetResourceAsync("Plugins.Widgets.Asyntai.Connect.Expired") });

        var body = await AsyntaiRemote.GetJsonAsync("/api/v1/wp-search/connect-status/?state=" + Uri.EscapeDataString(state));
        if (body is not { ValueKind: JsonValueKind.Object } root
            || !root.TryGetProperty("ready", out var ready) || ready.ValueKind != JsonValueKind.True
            || !root.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Object)
        {
            return Json(new { ok = true, ready = false });
        }

        var siteId = AsyntaiRemote.Str(data, "site_id");
        if (!SiteIdPattern.IsMatch(siteId))
            return Json(new { ok = true, ready = false });

        //the sign-in result is read here on the server, so nothing the
        //browser sends can set the widget id
        var scriptUrl = AsyntaiRemote.Str(data, "script_url");
        var email = AsyntaiRemote.Str(data, "account_email");

        settings.WidgetId = siteId;
        if (scriptUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            settings.ScriptUrl = scriptUrl;
        settings.AccountEmail = email.Length > 255 ? email[..255] : email;
        if (!string.IsNullOrEmpty(settings.PendingToken))
        {
            settings.FeedToken = settings.PendingToken;
            settings.FeedEnabled = true;
        }
        else
        {
            //connected without sharing: no earlier token stays valid
            settings.FeedToken = string.Empty;
            settings.FeedEnabled = false;
        }
        settings.PendingToken = string.Empty;
        settings.ConnectState = string.Empty;
        settings.ConnectStateAt = 0;
        await SaveAsync(settings);

        return Json(new { ok = true, ready = true, email });
    }

    /// <summary>
    /// Switch product and page sharing on or off
    /// </summary>
    [HttpPost]
    [CheckPermission(StandardPermission.Configuration.MANAGE_WIDGETS)]
    public async Task<IActionResult> Share(bool enabled)
    {
        var settings = await LoadAsync();
        if (enabled && string.IsNullOrEmpty(settings.FeedToken))
        {
            //connected before sharing existed, or by a pasted widget id:
            //Asyntai has no token for this store yet, so the owner signs in once more
            return Json(new { ok = false, needsConnect = true });
        }

        settings.FeedEnabled = enabled;
        await SaveAsync(settings);

        return Json(new { ok = true, enabled });
    }

    /// <summary>
    /// What Asyntai says about the last read of the shared products and pages
    /// </summary>
    [HttpPost]
    [CheckPermission(StandardPermission.Configuration.MANAGE_WIDGETS)]
    public async Task<IActionResult> Status()
    {
        var settings = await LoadAsync();
        if (string.IsNullOrWhiteSpace(settings.WidgetId))
            return Json(new { ok = true, hasSync = false });

        var body = await AsyntaiRemote.GetJsonAsync("/api/v1/search-widget/status/?widget_id=" + Uri.EscapeDataString(settings.WidgetId.Trim()));
        if (body is not { ValueKind: JsonValueKind.Object } root
            || !root.TryGetProperty("sync", out var sync) || sync.ValueKind != JsonValueKind.Object)
        {
            return Json(new { ok = true, hasSync = false });
        }

        var count = sync.TryGetProperty("count", out var c) && c.TryGetInt32(out var n) ? n : 0;

        return Json(new
        {
            ok = true,
            hasSync = true,
            status = AsyntaiRemote.Str(sync, "status"),
            count,
            error = AsyntaiRemote.Str(sync, "error")
        });
    }

    /// <summary>
    /// Forget the connection. The feed token goes with it.
    /// </summary>
    [HttpPost]
    [CheckPermission(StandardPermission.Configuration.MANAGE_WIDGETS)]
    public async Task<IActionResult> Disconnect()
    {
        var settings = await LoadAsync();
        settings.WidgetId = string.Empty;
        settings.AccountEmail = string.Empty;
        settings.FeedEnabled = false;
        settings.FeedToken = string.Empty;
        settings.PendingToken = string.Empty;
        settings.ConnectState = string.Empty;
        settings.ConnectStateAt = 0;
        settings.SiteUrl = string.Empty;
        await SaveAsync(settings);

        return Json(new { ok = true });
    }

    #endregion
}
