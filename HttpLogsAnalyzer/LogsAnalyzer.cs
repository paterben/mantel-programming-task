using System.Net;
using HttpLogsAnalyzer.Models;

namespace HttpLogsAnalyzer;

public class LogsAnalyzer
{
    private static bool IsSuccessStatusCode(HttpStatusCode statusCode)
    {
        return (int)statusCode >= 200 && (int)statusCode <= 299;
    }

    /// <summary>
    /// Converts an HTTP request Uri to its abs_path (aka origin) form (e.g. /foo/bar), and drops any query parameters.
    /// HTTP request URIs can be in absolute (e.g. http://example.com/foo/bar) or origin (e.g. /foo/bar) forms,
    /// or in some cases "*" or authority form.
    /// See https://datatracker.ietf.org/doc/html/rfc7230#section-5.3.
    /// </summary>
    /// <returns>The Uri abs_path, or empty if there is no abs_path.</returns>
    private static string GetUrlAbsolutePath(Uri requestUri)
    {
        if (requestUri.IsAbsoluteUri)
        {
            return requestUri.AbsolutePath;
        }
        if (requestUri.OriginalString == "*") return "";
        // This is a hack to extract the absolute path from the relative Uri.
        // Calling uri.AbsolutePath on a relative Uri throws an exception.
        // This also ensures query params are dropped.
        var absoluteUri = new Uri(new Uri("http://fake"), requestUri);
        return absoluteUri.AbsolutePath;
    }

    /// <summary>
    /// Counts the number of unique client IP addresses in the logs.
    /// </summary>
    /// <returns>The count of unique client IP addresses.</returns>
    public int CountUniqueClientIpAddresses(IList<LogLine> logLines)
    {
        return logLines.Select(line => line.ClientIpAddress).Distinct().Count();
    }

    /// <summary>
    /// Computes the 3 most active client IP addresses, as well as the associated request count for each.
    /// </summary>
    /// <returns>Up to three tuples of (IP address, count) ordered from highest to lowest count.</returns>
    public IList<Tuple<IPAddress, int>> ComputeTopClientIpAddresses(IList<LogLine> logLines)
    {
        return logLines
            .GroupBy(line => line.ClientIpAddress)
            .OrderByDescending(g => g.Count())
            .Take(3)
            .Select(g => new Tuple<IPAddress, int>(g.Key, g.Count()))
            .ToList();
    }

    /// <summary>
    /// Computes the top 3 URLs in abs_path form (e.g. /foo/bar), as well as the associated request count for each.
    /// Only GET requests with successful HTTP status codes are considered.
    /// Query parameters are dropped.
    /// Requests with a URL in absolute form (e.g. http://example.com/foo/bar) are first converted to abs_path form.
    /// </summary>
    /// <returns>Up to three tuples of (URL, count) ordered from highest to lowest count.</returns>
    public IList<Tuple<string, int>> ComputeTopUrls(IList<LogLine> logLines)
    {
        return logLines
            .Where(line => line.HttpMethod == HttpMethod.Get && IsSuccessStatusCode(line.StatusCode))
            .GroupBy(line => GetUrlAbsolutePath(line.RequestUri))
            .Where(g => g.Key != "")
            .OrderByDescending(g => g.Count())
            .Take(3)
            .Select(g => new Tuple<string, int>(g.Key, g.Count()))
            .ToList();
    }
}
