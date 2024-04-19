using System.Collections.ObjectModel;
using System.Text;
using System.Web;

namespace mobiletelemetry;

public class InfluxDBClient(string BaseUrl, string Token, string Org, string Bucket, Dictionary<string, string> tags)
{
    public ReadOnlyDictionary<string, string> Tags { get; } = tags.AsReadOnly();

    HttpClient _client = new();
    string _tagsString = string.Join(",", tags.Select(pair => $"{Escape(pair.Key)}={Escape(pair.Value)}"));

    public async Task<string> Send(string measurement, Dictionary<string,string> data)
    {
        string dataString = string.Join(",", data.Select(pair => $"{pair.Key}={pair.Value}"));
        string timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();

        Uri uri = new($"{BaseUrl}/api/v2/write");
        var query = HttpUtility.ParseQueryString("");
        query.Add("org", Org);
        query.Add("bucket", Bucket);
        query.Add("precision", "ms");

        HttpResponseMessage response;

        using (HttpRequestMessage request = new(HttpMethod.Post, $"{uri}?{query}"))
        {
            request.Headers.Authorization = new("Token", Token);
            request.Headers.Accept.Add(new("application/json"));
            request.Content = new StringContent(
                $"{measurement},{_tagsString} {dataString} {timestamp}",
                Encoding.UTF8,
                "text/plain"
            );
            response = await _client.SendAsync(request);
        }

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync();
    }

    // cf. https://docs.influxdata.com/influxdb/cloud/reference/syntax/line-protocol/
    private static string Escape(string text)
    {
        return text
            .Replace(",", "\\,")
            .Replace("=", "\\=")
            .Replace(" ", "\\ ");
    }
}
