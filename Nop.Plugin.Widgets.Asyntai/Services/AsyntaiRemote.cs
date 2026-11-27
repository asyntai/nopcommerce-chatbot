using System.Text;
using System.Text.Json;

namespace Nop.Plugin.Widgets.Asyntai.Services;

/// <summary>
/// Server-side calls to Asyntai: stage a feed token, poll a sign-in, read the
/// sync status. The browser never talks to Asyntai for these.
/// </summary>
public static class AsyntaiRemote
{
    private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromSeconds(15) };

    /// <summary>
    /// Post a JSON body. Returns the JSON answer, or null when Asyntai cannot
    /// be reached or answers with an error.
    /// </summary>
    public static async Task<JsonElement?> PostJsonAsync(string path, IDictionary<string, string> body)
    {
        try
        {
            //a plain body with a length: some servers refuse a chunked one
            using var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
            using var response = await Client.PostAsync(AsyntaiDefaults.Origin + path, content);
            if (!response.IsSuccessStatusCode)
                return null;

            return JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync());
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Get a JSON answer, or null when Asyntai cannot be reached
    /// </summary>
    public static async Task<JsonElement?> GetJsonAsync(string pathAndQuery)
    {
        try
        {
            using var response = await Client.GetAsync(AsyntaiDefaults.Origin + pathAndQuery);
            if (!response.IsSuccessStatusCode)
                return null;

            return JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync());
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// A string property of a JSON object, trimmed, or an empty string
    /// </summary>
    public static string Str(JsonElement obj, string name)
    {
        return obj.ValueKind == JsonValueKind.Object
            && obj.TryGetProperty(name, out var value)
            && value.ValueKind == JsonValueKind.String
            ? (value.GetString() ?? string.Empty).Trim()
            : string.Empty;
    }
}
