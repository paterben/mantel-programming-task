using System.Net;
using FluentAssertions;
using HttpLogsAnalyzer.Models;

namespace HttpLogsAnalyzer.UnitTests;

[TestClass]
public sealed class LogsAnalyzerTests
{
    private static LogLine MakeBasicLogLine(IPAddress clientIpAddress)
    {
        return new LogLine
        {
            ClientIpAddress = clientIpAddress,
            Timestamp = new DateTimeOffset(2018, 7, 10, 22, 21, 28, TimeSpan.FromHours(2)),
            HttpMethod = HttpMethod.Get,
            RequestUri = new Uri("/", UriKind.Relative),
            StatusCode = HttpStatusCode.OK
        };
    }

    private static LogLine MakeBasicLogLine(IPAddress clientIpAddress, Uri requestUri)
    {
        return new LogLine
        {
            ClientIpAddress = clientIpAddress,
            Timestamp = new DateTimeOffset(2018, 7, 10, 22, 21, 28, TimeSpan.FromHours(2)),
            HttpMethod = HttpMethod.Get,
            RequestUri = requestUri,
            StatusCode = HttpStatusCode.OK
        };
    }

    [TestMethod]
    public void CountUniqueClientIpAddresses_Works()
    {
        var logsAnalyzer = new LogsAnalyzer();
        var logLine1 = MakeBasicLogLine(new IPAddress([1, 1, 1, 1]));
        var logLine2 = MakeBasicLogLine(new IPAddress([2, 2, 2, 2]));
        logLine2.RequestUri = new Uri("http://example.net/");
        logLine2.StatusCode = HttpStatusCode.InternalServerError;
        var logLine3 = MakeBasicLogLine(new IPAddress([1, 1, 1, 1]));
        logLine3.HttpMethod = HttpMethod.Post;
        logLine3.StatusCode = HttpStatusCode.Created;

        int numUniqueClientIpAddresses = logsAnalyzer.CountUniqueClientIpAddresses([logLine1, logLine2, logLine3]);

        numUniqueClientIpAddresses.Should().Be(2);
    }

    [TestMethod]
    public void ComputeTopClientIpAddresses_WorksWhenFewerThan3Addresses()
    {
        var logsAnalyzer = new LogsAnalyzer();
        var logLine1 = MakeBasicLogLine(new IPAddress([1, 1, 1, 1]));
        var logLine2 = MakeBasicLogLine(new IPAddress([2, 2, 2, 2]));
        logLine2.RequestUri = new Uri("http://example.net/");
        logLine2.StatusCode = HttpStatusCode.InternalServerError;
        var logLine3 = MakeBasicLogLine(new IPAddress([1, 1, 1, 1]));
        logLine3.HttpMethod = HttpMethod.Post;
        logLine3.StatusCode = HttpStatusCode.Created;

        var topClientIpAddresses = logsAnalyzer.ComputeTopClientIpAddresses([logLine1, logLine2, logLine3]);

        topClientIpAddresses.Should().BeInDescendingOrder(tup => tup.Item2);
        topClientIpAddresses.Should().BeEquivalentTo(
            [new Tuple<IPAddress, int>(new IPAddress([1, 1, 1, 1]), 2),
             new Tuple<IPAddress, int>(new IPAddress([2, 2, 2, 2]), 1)]);
    }

    [TestMethod]
    public void ComputeTopClientIpAddresses_WorksWhenMoreThan3Addresses()
    {
        var logsAnalyzer = new LogsAnalyzer();
        var logLine1 = MakeBasicLogLine(new IPAddress([1, 1, 1, 1]));
        var logLine2 = MakeBasicLogLine(new IPAddress([2, 2, 2, 2]));
        logLine2.RequestUri = new Uri("http://example.net/");
        logLine2.StatusCode = HttpStatusCode.InternalServerError;
        var logLine3 = MakeBasicLogLine(new IPAddress([3, 3, 3, 3]));
        logLine3.HttpMethod = HttpMethod.Post;
        logLine3.StatusCode = HttpStatusCode.Created;
        var logLine4 = MakeBasicLogLine(new IPAddress([4, 4, 4, 4]));
        logLine2.RequestUri = new Uri("/baz", UriKind.Relative);
        var logLine5 = MakeBasicLogLine(new IPAddress([3, 3, 3, 3]));

        var topClientIpAddresses = logsAnalyzer.ComputeTopClientIpAddresses([logLine1, logLine2, logLine3, logLine4, logLine5]);

        topClientIpAddresses.Should().BeInDescendingOrder(tup => tup.Item2);
        topClientIpAddresses.Should().BeEquivalentTo(
            [new Tuple<IPAddress, int>(new IPAddress([3, 3, 3, 3]), 2),
             new Tuple<IPAddress, int>(new IPAddress([1, 1, 1, 1]), 1),
             new Tuple<IPAddress, int>(new IPAddress([2, 2, 2, 2]), 1)]);
    }

    [TestMethod]
    public void ComputeTopUrls_WorksWhenFewerThan3Urls()
    {
        var logsAnalyzer = new LogsAnalyzer();
        var logLine1 = MakeBasicLogLine(new IPAddress([1, 1, 1, 1]), new Uri("/foo/bar", UriKind.Relative));
        var logLine2 = MakeBasicLogLine(new IPAddress([2, 2, 2, 2]), new Uri("/foo/bar", UriKind.Relative));
        var logLine3 = MakeBasicLogLine(new IPAddress([3, 3, 3, 3]), new Uri("/", UriKind.Relative));

        var topUrls = logsAnalyzer.ComputeTopUrls([logLine1, logLine2, logLine3]);

        topUrls.Should().BeInDescendingOrder(tup => tup.Item2);
        topUrls.Should().BeEquivalentTo(
            [new Tuple<string, int>("/foo/bar", 2),
             new Tuple<string, int>("/", 1)]);
    }

    [TestMethod]
    public void ComputeTopUrls_WorksWhenMoreThan3Urls()
    {
        var logsAnalyzer = new LogsAnalyzer();
        var logLine1 = MakeBasicLogLine(new IPAddress([1, 1, 1, 1]), new Uri("/foo/bar", UriKind.Relative));
        var logLine2 = MakeBasicLogLine(new IPAddress([2, 2, 2, 2]), new Uri("/bar/baz", UriKind.Relative));
        var logLine3 = MakeBasicLogLine(new IPAddress([3, 3, 3, 3]), new Uri("/", UriKind.Relative));
        var logLine4 = MakeBasicLogLine(new IPAddress([4, 4, 4, 4]), new Uri("/qux", UriKind.Relative));
        var logLine5 = MakeBasicLogLine(new IPAddress([5, 5, 5, 5]), new Uri("/bar/baz", UriKind.Relative));

        var topUrls = logsAnalyzer.ComputeTopUrls([logLine1, logLine2, logLine3, logLine4, logLine5]);

        topUrls.Should().BeInDescendingOrder(tup => tup.Item2);
        topUrls.Should().BeEquivalentTo(
            [new Tuple<string, int>("/bar/baz", 2),
             new Tuple<string, int>("/foo/bar", 1),
             new Tuple<string, int>("/", 1)]);
    }

    [TestMethod]
    public void ComputeTopUrls_ConvertsAbsoluteUrlsToRelativeUrls()
    {
        var logsAnalyzer = new LogsAnalyzer();
        var logLine1 = MakeBasicLogLine(new IPAddress([1, 1, 1, 1]), new Uri("/foo/bar", UriKind.Relative));
        var logLine2 = MakeBasicLogLine(new IPAddress([2, 2, 2, 2]), new Uri("http://example.net/foo/bar"));
        var logLine3 = MakeBasicLogLine(new IPAddress([3, 3, 3, 3]), new Uri("http://contoso.net/qux"));
        var logLine4 = MakeBasicLogLine(new IPAddress([4, 4, 4, 4]), new Uri("http://example.net/qux"));
        var logLine5 = MakeBasicLogLine(new IPAddress([5, 5, 5, 5]), new Uri("http://contoso.net/foo/bar"));

        var topUrls = logsAnalyzer.ComputeTopUrls([logLine1, logLine2, logLine3, logLine4, logLine5]);

        topUrls.Should().BeInDescendingOrder(tup => tup.Item2);
        topUrls.Should().BeEquivalentTo(
            [new Tuple<string, int>("/foo/bar", 3),
             new Tuple<string, int>("/qux", 2)]);
    }

    [TestMethod]
    public void ComputeTopUrls_IgnoresStarUrls()
    {
        var logsAnalyzer = new LogsAnalyzer();
        var logLine1 = MakeBasicLogLine(new IPAddress([1, 1, 1, 1]), new Uri("/foo/bar", UriKind.Relative));
        var logLine2 = MakeBasicLogLine(new IPAddress([2, 2, 2, 2]), new Uri("*", UriKind.Relative));
        logLine2.HttpMethod = HttpMethod.Options;

        var topUrls = logsAnalyzer.ComputeTopUrls([logLine1, logLine2]);

        topUrls.Should().BeInDescendingOrder(tup => tup.Item2);
        topUrls.Should().BeEquivalentTo(
            [new Tuple<string, int>("/foo/bar", 1)]);
    }

    [TestMethod]
    // Query params should not normally be present, however the analyzer should drop them anyway.
    public void ComputeTopUrls_IgnoresQueryParams()
    {
        var logsAnalyzer = new LogsAnalyzer();
        var logLine1 = MakeBasicLogLine(new IPAddress([1, 1, 1, 1]), new Uri("/foo/bar?qux=1", UriKind.Relative));
        var logLine2 = MakeBasicLogLine(new IPAddress([2, 2, 2, 2]), new Uri("http://example.net/foo/bar"));
        var logLine3 = MakeBasicLogLine(new IPAddress([3, 3, 3, 3]), new Uri("http://contoso.net/qux?qux=3"));
        var logLine4 = MakeBasicLogLine(new IPAddress([4, 4, 4, 4]), new Uri("/foo/bar?qux=4", UriKind.Relative));

        var topUrls = logsAnalyzer.ComputeTopUrls([logLine1, logLine2, logLine3, logLine4]);

        topUrls.Should().BeInDescendingOrder(tup => tup.Item2);
        topUrls.Should().BeEquivalentTo(
            [new Tuple<string, int>("/foo/bar", 3),
             new Tuple<string, int>("/qux", 1)]);
    }

    [TestMethod]
    public void ComputeTopUrls_ExcludesFailureStatusCodes()
    {
        var logsAnalyzer = new LogsAnalyzer();
        var logLine1 = MakeBasicLogLine(new IPAddress([1, 1, 1, 1]), new Uri("http://example.net/foo/bar"));
        var logLine2 = MakeBasicLogLine(new IPAddress([2, 2, 2, 2]), new Uri("/foo/bar", UriKind.Relative));
        logLine2.StatusCode = HttpStatusCode.InternalServerError;
        var logLine3 = MakeBasicLogLine(new IPAddress([1, 1, 1, 1]), new Uri("http://example.net/not/found"));
        logLine3.StatusCode = HttpStatusCode.NotFound;

        var topUrls = logsAnalyzer.ComputeTopUrls([logLine1, logLine2, logLine3]);

        topUrls.Should().BeInDescendingOrder(tup => tup.Item2);
        topUrls.Should().BeEquivalentTo(
            [new Tuple<string, int>("/foo/bar", 1)]);
    }

    [TestMethod]
    public void ComputeTopUrls_ExcludesNonHttpGetRequests()
    {
        var logsAnalyzer = new LogsAnalyzer();
        var logLine1 = MakeBasicLogLine(new IPAddress([1, 1, 1, 1]), new Uri("http://example.net/foo/bar"));
        var logLine2 = MakeBasicLogLine(new IPAddress([2, 2, 2, 2]), new Uri("/foo/bar", UriKind.Relative));
        logLine2.HttpMethod = HttpMethod.Post;
        logLine2.StatusCode = HttpStatusCode.Created;
        var logLine3 = MakeBasicLogLine(new IPAddress([1, 1, 1, 1]), new Uri("http://example.net/foo/bar"));
        logLine3.HttpMethod = HttpMethod.Post;
        logLine3.StatusCode = HttpStatusCode.Created;

        var topUrls = logsAnalyzer.ComputeTopUrls([logLine1, logLine2, logLine3]);

        topUrls.Should().BeInDescendingOrder(tup => tup.Item2);
        topUrls.Should().BeEquivalentTo(
            [new Tuple<string, int>("/foo/bar", 1)]);
    }
}
