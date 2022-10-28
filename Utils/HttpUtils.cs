using System.Net.Http.Headers;

namespace TelegramMediaGrabberBot.Utils;

public static class HttpUtils
{

    public static async Task<string> GetRealUrlFromMoved(string url)
    {
        //this allows you to set the settings so that we can get the redirect url
        var handler = new HttpClientHandler()
        {
            AllowAutoRedirect = false
        };
        string redirectedUrl = url;

        using (HttpClient client = new(handler))
        using (HttpResponseMessage response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Head, url)))
        using (HttpContent content = response.Content)
        {
            // ... Read the response to see if we have the redirected url
            if (response.StatusCode == System.Net.HttpStatusCode.Found ||
                response.StatusCode == System.Net.HttpStatusCode.Moved)
            {
                HttpResponseHeaders headers = response.Headers;
                if (headers != null && headers.Location != null)
                {
                    redirectedUrl = headers.Location.AbsoluteUri;
                    return await GetRealUrlFromMoved(redirectedUrl); //recursive call until we have the final url
                }
            }
        }

        return redirectedUrl;
    }
}
