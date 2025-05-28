using System.Net;

namespace HttpLogsAnalyzer.Models;

// Structured elements extracted from a line in a log file of HTTP requests.
public class LogLine
{
    public required IPAddress IpAddress { get; set; }

    public required DateTimeOffset Timestamp { get; set; }

    public required HttpMethod HttpMethod { get; set; }

    // The domain name, e.g example.net, if known.
    public string? Domain { get; set; }

    // The URL absolute path e.g. "/", "/foo", "/bar/index.html".
    // Does not include scheme, host or query params.
    public required string AbsolutePath { get; set; }

    public required HttpStatusCode StatusCode { get; set; }

}