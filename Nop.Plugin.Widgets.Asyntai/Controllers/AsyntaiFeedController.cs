using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Plugin.Widgets.Asyntai.Services;

namespace Nop.Plugin.Widgets.Asyntai.Controllers;

/// <summary>
/// The public, signed product and page feed. It answers only requests signed
/// with the store's feed token, and only while sharing is switched on.
/// </summary>
public class AsyntaiFeedController : Controller
{
    #region Fields

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    protected readonly AsyntaiFeedService _feedService;
    protected readonly IStoreContext _storeContext;

    #endregion

    #region Ctor

    public AsyntaiFeedController(AsyntaiFeedService feedService, IStoreContext storeContext)
    {
        _feedService = feedService;
        _storeContext = storeContext;
    }

    #endregion

    #region Utilities

    protected virtual string Query(string name)
    {
        return Request.Query.ContainsKey(name) ? Request.Query[name].ToString() : null;
    }

    protected virtual IActionResult Answer(int status, object body)
    {
        Response.Headers["Cache-Control"] = "no-store";
        Response.Headers["X-Robots-Tag"] = "noindex";

        return new ContentResult
        {
            StatusCode = status,
            ContentType = "application/json; charset=utf-8",
            Content = JsonSerializer.Serialize(body, JsonOptions)
        };
    }

    protected virtual async Task<IActionResult> RefusedAsync()
    {
        var error = await _feedService.RefuseAsync(Query("page"), Query("limit"), Query("ids"), Query("ts"), Query("sig"));

        return error == null ? null : Answer(403, new { ok = false, error });
    }

    #endregion

    #region Methods

    [HttpGet]
    public virtual async Task<IActionResult> Products()
    {
        var refused = await RefusedAsync();
        if (refused != null)
            return refused;

        var store = await _storeContext.GetCurrentStoreAsync();

        return Answer(200, await _feedService.GetProductFeedAsync(store, Query("page"), Query("limit"), Query("ids")));
    }

    [HttpGet]
    public virtual async Task<IActionResult> Pages()
    {
        var refused = await RefusedAsync();
        if (refused != null)
            return refused;

        var store = await _storeContext.GetCurrentStoreAsync();

        return Answer(200, await _feedService.GetPageFeedAsync(store, Query("page"), Query("limit")));
    }

    #endregion
}
