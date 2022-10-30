namespace Maple_Server.HTTP;

public class HTTPWrapper
{
    private HttpClient _http;

    private HTTPWrapper()
    {
        _http = new HttpClient();
        _http.DefaultRequestHeaders.Add("User-Agent", "mapleserver/azuki is a cutie");
    }

    private static HTTPWrapper _instance;
    public static HTTPWrapper Instance => _instance ??= new HTTPWrapper();
    
    public string Get(string query)
    {
        var response = _http.GetAsync(query).GetAwaiter().GetResult();

        return response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
    }

    public string Post(string url, Dictionary<string, string> args)
    {
        var content = new FormUrlEncodedContent(args);

        var response = _http.PostAsync(url, content).GetAwaiter().GetResult();

        return response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
    }
}