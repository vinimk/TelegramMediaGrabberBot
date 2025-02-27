using System.Net.Http.Headers;

namespace TelegramMediaGrabberBot.Utils;

public static class HttpUtils
{

    public static async Task<string> GetRealUrlFromMoved(string url)
    {
        string redirectedUrl = url;
        try
        {
            //this allows you to set the settings so that we can get the redirect url
            HttpClientHandler handler = new()
            {
                AllowAutoRedirect = false
            };

            using HttpClient client = new(handler);
            _ = client.DefaultRequestHeaders.UserAgent.TryParseAdd("curl");
            using HttpResponseMessage response = await client.GetAsync(url);
            using HttpContent content = response.Content;
            // ... Read the response to see if we have the redirected url
            if (response.StatusCode is System.Net.HttpStatusCode.Found or
                System.Net.HttpStatusCode.Moved or
                System.Net.HttpStatusCode.RedirectKeepVerb or
                System.Net.HttpStatusCode.PermanentRedirect
                )
            {
                HttpResponseHeaders headers = response.Headers;
                if (headers != null && headers.Location != null)
                {
                    redirectedUrl = headers.Location.AbsoluteUri;
                    return await GetRealUrlFromMoved(redirectedUrl); //recursive call until we have the final url
                }
            }
        }
        catch { }

        return redirectedUrl;
    }
    public static async Task<Stream?> GetStreamFromUrl(Uri uri)
    {
        HttpClient httpClient = new();
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("curl");

        HttpResponseMessage response = await httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead);

        _ = response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStreamAsync();
    }
}