using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace RaceDayWeb.Services;

public class ApiResult<T>
{
    public bool Success { get; init; }
    public T? Data { get; init; }
    public string Error { get; init; } = string.Empty;
    public HttpStatusCode Status { get; init; }
}

public class ApiClient
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private const string ApiCookiePrefix = ".RaceDay.Session=";
    private readonly HttpClient _http;
    private readonly IHttpContextAccessor _ctx;

    public ApiClient(HttpClient http, IHttpContextAccessor ctx)
    {
        _http = http;
        _ctx = ctx;
    }

    private ISession Session => _ctx.HttpContext!.Session;

    public Task<ApiResult<T>> GetAsync<T>(string url) => SendAsync<T>(HttpMethod.Get, url, null);

    public Task<ApiResult<T>> PostAsync<T>(string url, object? body = null) =>
        SendAsync<T>(HttpMethod.Post, url, ToJson(body));

    public Task<ApiResult<T>> PutAsync<T>(string url, object? body = null) =>
        SendAsync<T>(HttpMethod.Put, url, ToJson(body));

    public Task<ApiResult<object>> DeleteAsync(string url) => SendAsync<object>(HttpMethod.Delete, url, null);

    public Task<ApiResult<T>> PostFileAsync<T>(string url, IFormFile file)
    {
        var content = new MultipartFormDataContent();
        var part = new StreamContent(file.OpenReadStream());
        part.Headers.ContentType = new MediaTypeHeaderValue(
            string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType);
        content.Add(part, "file", file.FileName);
        return SendAsync<T>(HttpMethod.Post, url, content);
    }

    private static HttpContent? ToJson(object? body) =>
        body == null ? null : new StringContent(JsonSerializer.Serialize(body, Json), Encoding.UTF8, "application/json");

    private async Task<ApiResult<T>> SendAsync<T>(HttpMethod method, string url, HttpContent? content)
    {
        try
        {
            using var request = new HttpRequestMessage(method, url) { Content = content };

            // forward the API session cookie that we stored at login
            var cookie = Session.GetString("ApiCookie");
            if (!string.IsNullOrEmpty(cookie))
            {
                request.Headers.TryAddWithoutValidation("Cookie", cookie);
            }

            using var response = await _http.SendAsync(request);
            CaptureCookie(response);
            var text = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                T? data = default;
                if (!string.IsNullOrWhiteSpace(text))
                {
                    data = JsonSerializer.Deserialize<T>(text, Json);
                }

                return new ApiResult<T> { Success = true, Data = data, Status = response.StatusCode };
            }

            return new ApiResult<T> { Success = false, Error = ReadError(text, response.StatusCode), Status = response.StatusCode };
        }
        catch (Exception)
        {
            return new ApiResult<T>
            {
                Success = false,
                Status = HttpStatusCode.ServiceUnavailable,
                Error = "The RaceDay service is not reachable right now. Please try again shortly."
            };
        }
    }

    private void CaptureCookie(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var values)) return;
        foreach (var value in values)
        {
            if (value.StartsWith(ApiCookiePrefix))
            {
                Session.SetString("ApiCookie", value.Split(';')[0]);
            }
        }
    }

    private static string ReadError(string body, HttpStatusCode status)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            if (root.TryGetProperty("message", out var message) && message.ValueKind == JsonValueKind.String)
            {
                return message.GetString()!;
            }

            if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
            {
                var all = errors.EnumerateObject()
                    .SelectMany(p => p.Value.EnumerateArray().Select(x => x.GetString()))
                    .Where(x => !string.IsNullOrWhiteSpace(x));
                var joined = string.Join(" ", all);
                if (joined.Length > 0) return joined;
            }
        }
        catch
        {
            // fall through to friendly defaults
        }

        return status switch
        {
            HttpStatusCode.Unauthorized => "Please log in to continue.",
            HttpStatusCode.Forbidden => "You do not have permission to do that.",
            HttpStatusCode.NotFound => "That item could not be found.",
            _ => "Something went wrong. Please check your details and try again."
        };
    }
}