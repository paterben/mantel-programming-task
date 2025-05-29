using System.Net;
using HttpLogsAnalyzer.Models;

public class LogsAnalyzer
{
    private static bool IsSuccessStatusCode(HttpStatusCode statusCode)
    {
        return (int)statusCode >= 200 && (int)statusCode <= 299;
    }

    /// <summary>
    /// Converts an HTTP request Uri to abs_path form.
    /// HTTP request URIs can be in absoluteURI (e.g. http://example.com/foo/bar) or abs_path (e.g. /foo/bar) forms,
    /// or in some cases "*" or an authority specification.
    /// This drops the host and port for the absoluteURI form.
    /// See https://datatracker.ietf.org/doc/html/rfc2616#section-5.1.2.
    /// </summary>
    /// <returns>The Uri absolute path, or empty if there is no absolute path.</returns>
    private static string GetUrlAbsolutePath(Uri requestUri)
    {
        if (requestUri.IsAbsoluteUri)
        {
            return requestUri.AbsolutePath;
        }
        if (requestUri.OriginalString == "*") return "";
        // This is a hack to extract the absolute path from the relative Uri.
        // Calling uri.AbsolutePath on a relative Uri throws an exception.
        // This also ensures query params are dropped even though they shouldn't normally be present.
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
    /// Requests with a URL in absoluteURI form (e.g. http://example.com/foo/bar) are first converted to abs_path form.
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
