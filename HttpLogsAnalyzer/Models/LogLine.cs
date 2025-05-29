using System.Net;

namespace HttpLogsAnalyzer.Models;

// Structured elements extracted from a line in a log file of HTTP requests.
public class LogLine
{
    public required IPAddress ClientIpAddress { get; set; }

    public required DateTimeOffset Timestamp { get; set; }

    public required HttpMethod HttpMethod { get; set; }

    // The HTTP request URI, e.g. http://example.com/foo/bar (absoluteURI) or /foo/bar (abs_path).
    // See https://datatracker.ietf.org/doc/html/rfc2616#section-5.1.2.
    public required Uri RequestUri { get; set; }

    public required HttpStatusCode StatusCode { get; set; }

}
