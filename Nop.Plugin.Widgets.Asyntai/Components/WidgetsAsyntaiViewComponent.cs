using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Services.Customers;
using Nop.Web.Framework.Components;

namespace Nop.Plugin.Widgets.Asyntai.Components;

/// <summary>
/// Writes the Asyntai loader into the storefront
/// </summary>
public class WidgetsAsyntaiViewComponent : NopViewComponent
{
    #region Fields

    protected readonly AsyntaiSettings _asyntaiSettings;
    protected readonly ICustomerService _customerService;
    protected readonly IWorkContext _workContext;

    #endregion

    #region Ctor

    public WidgetsAsyntaiViewComponent(AsyntaiSettings asyntaiSettings,
        ICustomerService customerService,
        IWorkContext workContext)
    {
        _asyntaiSettings = asyntaiSettings;
        _customerService = customerService;
        _workContext = workContext;
    }

    #endregion

    #region Utilities

    /// <summary>
    /// Whether the current request is a checkout page
    /// </summary>
    /// <remarks>
    /// The path is matched rather than the route, because a store can run the
    /// one page checkout, the standard checkout, or a localized path prefix,
    /// and all three keep the "checkout" segment.
    /// </remarks>
    protected bool IsCheckoutPage()
    {
        var path = HttpContext?.Request?.Path.Value ?? string.Empty;
        return path.Contains("/checkout", StringComparison.InvariantCultureIgnoreCase)
            || path.Contains("/onepagecheckout", StringComparison.InvariantCultureIgnoreCase);
    }

    /// <summary>
    /// Builds the script that loads the widget
    /// </summary>
    /// <returns>A task that represents the asynchronous operation</returns>
    protected async Task<string> GetScriptAsync()
    {
        //JsonSerializer is used for every value that reaches the page, so a
        //quote or an angle bracket in a customer name cannot break out of the
        //script element.
        var widgetId = JsonSerializer.Serialize(_asyntaiSettings.WidgetId?.Trim() ?? string.Empty);
        var scriptUrl = JsonSerializer.Serialize(string.IsNullOrWhiteSpace(_asyntaiSettings.ScriptUrl)
            ? AsyntaiDefaults.DefaultScriptUrl
            : _asyntaiSettings.ScriptUrl.Trim());

        var builder = new StringBuilder();
        builder.AppendLine("<!-- Asyntai AI chat widget -->");
        builder.AppendLine("<script>");
        builder.AppendLine("(function(){");
        builder.AppendLine("  if (window.__asyntaiNopLoaded) { return; }");
        builder.AppendLine("  window.__asyntaiNopLoaded = true;");
        builder.AppendLine($"  var id = {widgetId};");
        builder.AppendLine("  if (!id) { return; }");

        if (_asyntaiSettings.PassCustomerDetails)
        {
            var customer = await _workContext.GetCurrentCustomerAsync();
            if (customer is not null && !await _customerService.IsGuestAsync(customer))
            {
                var name = await _customerService.GetCustomerFullNameAsync(customer);
                var context = new Dictionary<string, string>();
                if (!string.IsNullOrWhiteSpace(name))
                    context["name"] = name;
                if (!string.IsNullOrWhiteSpace(customer.Email))
                    context["email"] = customer.Email;

                if (context.Count > 0)
                {
                    builder.AppendLine("  window.Asyntai = window.Asyntai || {};");
                    builder.AppendLine($"  window.Asyntai.userContext = {JsonSerializer.Serialize(context)};");
                }
            }
        }

        builder.AppendLine("  window.ASYNTAI_WIDGET_ID = id;");
        builder.AppendLine("  var s = document.createElement('script');");
        builder.AppendLine("  s.async = true;");
        builder.AppendLine($"  s.src = {scriptUrl};");
        builder.AppendLine("  s.setAttribute('data-asyntai-id', id);");
        builder.AppendLine("  (document.head || document.documentElement).appendChild(s);");
        builder.AppendLine("})();");
        builder.AppendLine("</script>");

        return builder.ToString();
    }

    #endregion

    #region Methods

    /// <summary>
    /// Invoke view component
    /// </summary>
    /// <param name="widgetZone">Widget zone</param>
    /// <param name="additionalData">Additional data</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    public async Task<IViewComponentResult> InvokeAsync(string widgetZone, object additionalData)
    {
        //an empty widget id means the merchant has not finished the setup yet,
        //so nothing at all is written to the page
        if (string.IsNullOrWhiteSpace(_asyntaiSettings.WidgetId))
            return Content(string.Empty);

        if (_asyntaiSettings.HideOnCheckout && IsCheckoutPage())
            return Content(string.Empty);

        var script = await GetScriptAsync();

        return View("~/Plugins/Widgets.Asyntai/Views/PublicInfo.cshtml", script);
    }

    #endregion
}
