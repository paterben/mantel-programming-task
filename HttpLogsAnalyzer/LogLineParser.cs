using System.Diagnostics;
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

    private const int kIpAddressIndex = 0;
    private const int kTimestampIndex = 3;
    private const int kHttpRequestSummaryIndex = 4;
    private const int kHttpStatusCodeIndex = 5;

    // Should be at least (max index of interest) + 1.
    private const int kMinLogLineParts = 6;

    // Example: 10/Jul/2018:22:01:17 +0200.
    private const string kTimestampFormat = "dd/MMM/yyyy:HH:mm:ss zzz";

    // Matches the HTTP request summary (e.g. "GET http://example.net/faq/ HTTP/1.1"),
    // with capturing groups for the HTTP method and request Uri.
    [GeneratedRegex(@"(\w+) ([^ ]+) HTTP/.*")]
    private static partial Regex HttpRequestSummaryRegex();

    /// <summary>
    /// Parses a line containing info about a single HTTP request.
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
        IPAddress ipAddress = IPAddress.Parse(parts[kIpAddressIndex].Value);
        DateTimeOffset timestamp = DateTimeOffset.ParseExact(parts[kTimestampIndex].Value.Trim('[', ']'), kTimestampFormat, CultureInfo.InvariantCulture);
        string httpRequestSummary = parts[kHttpRequestSummaryIndex].Value.Trim('"');
        (HttpMethod method, string? domain, string absolutePath) = ParseHttpRequestSummary(httpRequestSummary);
        HttpStatusCode statusCode = (HttpStatusCode)int.Parse(parts[kHttpStatusCodeIndex].Value);
        return new LogLine
        {
            IpAddress = ipAddress,
            Timestamp = timestamp,
            HttpMethod = method,
            Domain = domain,
            AbsolutePath = absolutePath,
            StatusCode = statusCode
        };
    }

    private (HttpMethod method, string? domain, string absolutePath) ParseHttpRequestSummary(string summary)
    {
        Match m = HttpRequestSummaryRegex().Match(summary);
        if (!m.Success)
        {
            throw new FormatException($"Failed to parse HTTP request summary into its components: {summary}");
        }
        HttpMethod method = HttpMethod.Parse(m.Groups[1].Value);
        Uri uri = new Uri(m.Groups[2].Value, UriKind.RelativeOrAbsolute);
        if (uri.IsAbsoluteUri)
        {
            // TODO: Are we sure that the host is always a domain name?
            return (method, uri.Host, uri.AbsolutePath);
        }
        // This is a hack to extract the absolute path without query params.
        // Calling uri.AbsolutePath on a relative Uri throws an exception.
        // TODO: This can likely be improved, as currently a Uri like example.com/foo (without http://)
        // will be treated as relative and example.com will end up as part of the path instead of the domain.
        uri = new Uri(new Uri("http://fake.com"), uri);
        return (method, null, uri.AbsolutePath);
    }
}