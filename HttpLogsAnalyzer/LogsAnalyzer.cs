using System.Diagnostics;
using System.Net;
using HttpLogsAnalyzer.Models;

public class LogsAnalyzer
{
    private static bool IsSuccessStatusCode(HttpStatusCode statusCode)
    {
        return (int)statusCode >= 200 && (int)statusCode <= 299;
    }

    // Computes IP addresses which are associated with a known, single domain name in the logs.
    private static Dictionary<IPAddress, string> ComputeIpsWithKnownDomain(IList<LogLine> logLines)
    {
        HashSet<IPAddress> ipsWithAmbiguousDomain = [];
        Dictionary<IPAddress, string> ipsWithKnownDomain = [];
        foreach (LogLine line in logLines)
        {
            if (line.Domain == null) continue;
            if (ipsWithAmbiguousDomain.Contains(line.IpAddress)) continue;
            if (ipsWithKnownDomain.TryGetValue(line.IpAddress, out string? value))
            {
                if (value != line.Domain)
                {
                    Debug.WriteLine($"IP address {line.IpAddress} has multiple domains: {value}, {line.Domain}");
                    ipsWithAmbiguousDomain.Add(line.IpAddress);
                    ipsWithKnownDomain.Remove(line.IpAddress);
                }
            }
            else { ipsWithKnownDomain[line.IpAddress] = line.Domain; }
        }
        return ipsWithKnownDomain;
    }

    private static string GetUrlIncludingDomain(LogLine line)
    {
        string domain = line.Domain ?? line.IpAddress.ToString();
        var domainUri = new Uri("http://" + domain, UriKind.Absolute);
        var pathUri = new Uri(line.AbsolutePath, UriKind.Relative);
        return new Uri(domainUri, pathUri).ToString();
    }

    /// <summary>
    /// Counts the number of unique IP addresses in the logs.
    /// </summary>
    /// <returns>The count of unique IP addresses.</returns>
    public int CountUniqueIpAddresses(IList<LogLine> logLines)
    {
        return logLines.Select(line => line.IpAddress).Distinct().Count();
    }

    /// <summary>
    /// Computes the top 3 IP addresses, as well as the associated frequency for each.
    /// </summary>
    /// <returns>Up to three tuples of (IP address, frequency) ordered from most to least frequent.</returns>
    public IList<Tuple<IPAddress, int>> ComputeTopIpAddresses(IList<LogLine> logLines)
    {
        return logLines
            .GroupBy(line => line.IpAddress)
            .OrderByDescending(g => g.Count())
            .Take(3)
            .Select(g => new Tuple<IPAddress, int>(g.Key, g.Count()))
            .ToList();
    }

    /// <summary>
    /// Computes the top 3 URLs including domain name, as well as the associated frequency for each.
    /// URLs include the domain (if known) or IP address (otherwise), as well as absolute path.
    /// Query params are ignored.
    /// Examples: http://www.example.com/foo/bar, http://192.168.1.1/foo/bar.
    /// Only GET requests with successful HTTP status codes are considered.
    /// For requests where only the IP address is available, tries to infer the domain from other log lines with the same IP.
    /// </summary>
    /// <returns>Up to three tuples of (URL, frequency) ordered from most to least frequent.</returns>
    public IList<Tuple<string, int>> ComputeTopUrlsIncludingDomain(IList<LogLine> logLines)
    {
        Dictionary<IPAddress, string> knownDomains = ComputeIpsWithKnownDomain(logLines);

        // Enrich each line with domain info if known.
        foreach (LogLine line in logLines)
        {
            if (knownDomains.TryGetValue(line.IpAddress, out string? value))
            {
                line.Domain = value!;
            }
        }

        return logLines
            .Where(line => line.HttpMethod == HttpMethod.Get && IsSuccessStatusCode(line.StatusCode))
            .GroupBy(GetUrlIncludingDomain)
            .OrderByDescending(g => g.Count())
            .Take(3)
            .Select(g => new Tuple<string, int>(g.Key, g.Count()))
            .ToList();
    }

    /// <summary>
    /// Computes the top 3 URLs (ignoring the domain name), as well as the associated frequency for each.
    /// Query params are ignored.
    /// Example: /foo/bar.
    /// Only GET requests with successful HTTP status codes are considered.
    /// </summary>
    /// <returns>Up to three tuples of (URL, frequency) ordered from most to least frequent.</returns>
    public IList<Tuple<string, int>> ComputeTopUrlsExcludingDomain(IList<LogLine> logLines)
    {
        return logLines
            .Where(line => line.HttpMethod == HttpMethod.Get && IsSuccessStatusCode(line.StatusCode))
            .GroupBy(line => line.AbsolutePath)
            .OrderByDescending(g => g.Count())
            .Take(3)
            .Select(g => new Tuple<string, int>(g.Key, g.Count()))
            .ToList();
    }
}