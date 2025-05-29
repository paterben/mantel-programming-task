using System.Net;

namespace HttpLogsAnalyzer.Models;

// Structured elements extracted from a line in a log file of HTTP requests.
public class LogLine
{
    public required IPAddress ClientIpAddress { get; set; }

    public required DateTimeOffset Timestamp { get; set; }

    public required HttpMethod HttpMethod { get; set; }

    // The HTTP request URI, e.g. http://example.com/foo/bar (absolute form) or /foo/bar (origin form).
    // See https://datatracker.ietf.org/doc/html/rfc7230#section-5.3.
    public required Uri RequestUri { get; set; }

    public required HttpStatusCode StatusCode { get; set; }

}
