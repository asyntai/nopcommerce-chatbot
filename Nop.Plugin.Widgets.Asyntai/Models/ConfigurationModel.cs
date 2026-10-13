using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Widgets.Asyntai.Models;

public record ConfigurationModel : BaseNopModel
{
    public int ActiveStoreScopeConfiguration { get; set; }

    [NopResourceDisplayName("Plugins.Widgets.Asyntai.WidgetId")]
    public string WidgetId { get; set; }
    public bool WidgetId_OverrideForStore { get; set; }

    [NopResourceDisplayName("Plugins.Widgets.Asyntai.ScriptUrl")]
    public string ScriptUrl { get; set; }
    public bool ScriptUrl_OverrideForStore { get; set; }

    [NopResourceDisplayName("Plugins.Widgets.Asyntai.PassCustomerDetails")]
    public bool PassCustomerDetails { get; set; }
    public bool PassCustomerDetails_OverrideForStore { get; set; }

    [NopResourceDisplayName("Plugins.Widgets.Asyntai.HideOnCheckout")]
    public bool HideOnCheckout { get; set; }
    public bool HideOnCheckout_OverrideForStore { get; set; }
}
