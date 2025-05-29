using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using HttpLogsAnalyzer.Models;

namespace HttpLogsAnalyzer;

public interface ILogLineParser
{
    public LogLine ParseLogLine(string line);
}

public partial class LogLineParser : ILogLineParser
{
    [GeneratedRegex(@""".+?""|\[.*\]|[^ ]+")]
    // Matches each element in a log line, which must be either:
    // - a quoted string
    // - a string surrounded by brackets []
    // - a sequence of non-space characters
    private static partial Regex LogLinePartsRegex();

    private const int kClientIpAddressIndex = 0;
    private const int kTimestampIndex = 3;
    private const int kHttpRequestSummaryIndex = 4;
    private const int kHttpStatusCodeIndex = 5;

    // Should be at least (max index of interest) + 1.
    private const int kMinLogLineParts = 6;

    // Example: 10/Jul/2018:22:01:17 +0200.
    private const string kTimestampFormat = "dd/MMM/yyyy:HH:mm:ss zzz";

    // Matches the HTTP request line (e.g. "GET http://example.net/faq/ HTTP/1.1"),
    // with capturing groups for the HTTP method and request Uri.
    // See https://datatracker.ietf.org/doc/html/rfc2616#section-5.1.
    [GeneratedRegex(@"(\w+) ([^ ]+) HTTP/.*")]
    private static partial Regex HttpRequestLineRegex();

    /// <summary>
    /// Parses a line containing info about a single incoming HTTP request.
    /// </summary>
    /// <returns>The parsed log line.</returns>
    /// <exception cref="FormatException">If the log line is badly formatted.</exception>
    public LogLine ParseLogLine(string line)
    {
        MatchCollection parts = LogLinePartsRegex().Matches(line);
        if (parts.Count < kMinLogLineParts)
        {
            throw new FormatException($"Log line has too few parts (expected at least {kMinLogLineParts}, actual {parts.Count}): {line}");
        }
        IPAddress clientIpAddress = IPAddress.Parse(parts[kClientIpAddressIndex].Value);
        DateTimeOffset timestamp = DateTimeOffset.ParseExact(parts[kTimestampIndex].Value.Trim('[', ']'), kTimestampFormat, CultureInfo.InvariantCulture);
        string httpRequestSummary = parts[kHttpRequestSummaryIndex].Value.Trim('"');
        (HttpMethod method, Uri requestUri) = ParseHttpRequestLine(httpRequestSummary);
        HttpStatusCode statusCode = (HttpStatusCode)int.Parse(parts[kHttpStatusCodeIndex].Value);
        return new LogLine
        {
            ClientIpAddress = clientIpAddress,
            Timestamp = timestamp,
            HttpMethod = method,
            RequestUri = requestUri,
            StatusCode = statusCode
        };
    }

    private (HttpMethod method, Uri requestUri) ParseHttpRequestLine(string summary)
    {
        Match m = HttpRequestLineRegex().Match(summary);
        if (!m.Success)
        {
            throw new FormatException($"Failed to parse HTTP request summary into its components: {summary}");
        }
        HttpMethod method = HttpMethod.Parse(m.Groups[1].Value);
        // The HTTP request URI can be an absoluteURI (e.g. http://example.com/foo/bar) or an abs_path (e.g. /foo/bar),
        // or in some cases "*" or an authority specification.
        // All the latter forms are considered to be relative Uris, so the `UriKind.RelativeOrAbsolute` is necessary.
        Uri requestUri = new(m.Groups[2].Value, UriKind.RelativeOrAbsolute);
        return (method, requestUri);
    }
}
