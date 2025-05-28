using System.Net;
using FluentAssertions;
using HttpLogsAnalyzer.Models;

namespace HttpLogsAnalyzer.UnitTests;

[TestClass]
public sealed class LogsAnalyzerTests
{
    private static LogLine MakeBasicLogLine(IPAddress ipAddress)
    {
        return new LogLine
        {
            IpAddress = ipAddress,
            Timestamp = new DateTimeOffset(2018, 7, 10, 22, 21, 28, TimeSpan.FromHours(2)),
            HttpMethod = HttpMethod.Get,
            Domain = null,
            AbsolutePath = "/",
            StatusCode = HttpStatusCode.OK
        };
    }

    private static LogLine MakeBasicLogLine(IPAddress ipAddress, string? domain, string absolutePath)
    {
        return new LogLine
        {
            IpAddress = ipAddress,
            Timestamp = new DateTimeOffset(2018, 7, 10, 22, 21, 28, TimeSpan.FromHours(2)),
            HttpMethod = HttpMethod.Get,
            Domain = domain,
            AbsolutePath = absolutePath,
            StatusCode = HttpStatusCode.OK
        };
    }

    [TestMethod]
    public void CountUniqueIpAddresses_Works()
    {
        var logsAnalyzer = new LogsAnalyzer();
        var logLine1 = MakeBasicLogLine(new IPAddress([1, 1, 1, 1]));
        var logLine2 = MakeBasicLogLine(new IPAddress([2, 2, 2, 2]));
        logLine2.Domain = "example.net";
        logLine2.StatusCode = HttpStatusCode.InternalServerError;
        var logLine3 = MakeBasicLogLine(new IPAddress([1, 1, 1, 1]));
        logLine3.HttpMethod = HttpMethod.Post;
        logLine3.StatusCode = HttpStatusCode.Created;

        int numUniqueIpAddresses = logsAnalyzer.CountUniqueIpAddresses([logLine1, logLine2, logLine3]);

        numUniqueIpAddresses.Should().Be(2);
    }

    [TestMethod]
    public void ComputeTopIpAddresses_WorksWhenFewerThan3Addresses()
    {
        var logsAnalyzer = new LogsAnalyzer();
        var logLine1 = MakeBasicLogLine(new IPAddress([1, 1, 1, 1]));
        var logLine2 = MakeBasicLogLine(new IPAddress([2, 2, 2, 2]));
        logLine2.Domain = "example.net";
        logLine2.StatusCode = HttpStatusCode.InternalServerError;
        var logLine3 = MakeBasicLogLine(new IPAddress([1, 1, 1, 1]));
        logLine3.HttpMethod = HttpMethod.Post;
        logLine3.StatusCode = HttpStatusCode.Created;

        var topIpAddresses = logsAnalyzer.ComputeTopIpAddresses([logLine1, logLine2, logLine3]);

        topIpAddresses.Should().BeInDescendingOrder(tup => tup.Item2);
        topIpAddresses.Should().BeEquivalentTo(
            [new Tuple<IPAddress, int>(new IPAddress([1, 1, 1, 1]), 2),
             new Tuple<IPAddress, int>(new IPAddress([2, 2, 2, 2]), 1)]);
    }

    [TestMethod]
    public void ComputeTopIpAddresses_WorksWhenMoreThan3Addresses()
    {
        var logsAnalyzer = new LogsAnalyzer();
        var logLine1 = MakeBasicLogLine(new IPAddress([1, 1, 1, 1]));
        var logLine2 = MakeBasicLogLine(new IPAddress([2, 2, 2, 2]));
        logLine2.Domain = "example.net";
        logLine2.StatusCode = HttpStatusCode.InternalServerError;
        var logLine3 = MakeBasicLogLine(new IPAddress([3, 3, 3, 3]));
        logLine3.HttpMethod = HttpMethod.Post;
        logLine3.StatusCode = HttpStatusCode.Created;
        var logLine4 = MakeBasicLogLine(new IPAddress([4, 4, 4, 4]));
        logLine4.AbsolutePath = "/baz";
        var logLine5 = MakeBasicLogLine(new IPAddress([3, 3, 3, 3]));

        var topIpAddresses = logsAnalyzer.ComputeTopIpAddresses([logLine1, logLine2, logLine3, logLine4, logLine5]);

        topIpAddresses.Should().BeInDescendingOrder(tup => tup.Item2);
        topIpAddresses.Should().BeEquivalentTo(
            [new Tuple<IPAddress, int>(new IPAddress([3, 3, 3, 3]), 2),
             new Tuple<IPAddress, int>(new IPAddress([1, 1, 1, 1]), 1),
             new Tuple<IPAddress, int>(new IPAddress([2, 2, 2, 2]), 1)]);
    }

    [TestMethod]
    public void ComputeTopUrlsIncludingDomain_WorksWhenFewerThan3Urls()
    {
        var logsAnalyzer = new LogsAnalyzer();
        var logLine1 = MakeBasicLogLine(new IPAddress([1, 1, 1, 1]), "example.net", "/foo/bar");
        var logLine2 = MakeBasicLogLine(new IPAddress([2, 2, 2, 2]), null, "/foo/bar");
        var logLine3 = MakeBasicLogLine(new IPAddress([3, 3, 3, 3]), "example.net", "/foo/bar");

        var topUrls = logsAnalyzer.ComputeTopUrlsIncludingDomain([logLine1, logLine2, logLine3]);

        topUrls.Should().BeInDescendingOrder(tup => tup.Item2);
        topUrls.Should().BeEquivalentTo(
            [new Tuple<string, int>("http://example.net/foo/bar", 2),
             new Tuple<string, int>("http://2.2.2.2/foo/bar", 1)]);
    }

    [TestMethod]
    public void ComputeTopUrlsIncludingDomain_WorksWhenMoreThan3Urls()
    {
        var logsAnalyzer = new LogsAnalyzer();
        var logLine1 = MakeBasicLogLine(new IPAddress([1, 1, 1, 1]), "example.net", "/foo/bar");
        var logLine2 = MakeBasicLogLine(new IPAddress([2, 2, 2, 2]), null, "/foo/bar");
        var logLine3 = MakeBasicLogLine(new IPAddress([3, 3, 3, 3]), "example.net", "/foo/bar");
        var logLine4 = MakeBasicLogLine(new IPAddress([4, 4, 4, 4]), "contoso.com", "/");
        var logLine5 = MakeBasicLogLine(new IPAddress([5, 5, 5, 5]), "example.com", "/foo/bar");

        var topUrls = logsAnalyzer.ComputeTopUrlsIncludingDomain([logLine1, logLine2, logLine3, logLine4, logLine5]);

        topUrls.Should().BeInDescendingOrder(tup => tup.Item2);
        topUrls.Should().BeEquivalentTo(
            [new Tuple<string, int>("http://example.net/foo/bar", 2),
             new Tuple<string, int>("http://2.2.2.2/foo/bar", 1),
             new Tuple<string, int>("http://contoso.com/", 1)]);
    }

    [TestMethod]
    public void ComputeTopUrlsIncludingDomain_ExcludesNonHttpGetRequests()
    {
        var logsAnalyzer = new LogsAnalyzer();
        var logLine1 = MakeBasicLogLine(new IPAddress([1, 1, 1, 1]), "example.net", "/foo/bar");
        var logLine2 = MakeBasicLogLine(new IPAddress([2, 2, 2, 2]), null, "/foo/bar");
        logLine2.HttpMethod = HttpMethod.Post;
        logLine2.StatusCode = HttpStatusCode.Created;
        var logLine3 = MakeBasicLogLine(new IPAddress([1, 1, 1, 1]), "example.net", "/foo/bar");
        logLine3.HttpMethod = HttpMethod.Post;
        logLine3.StatusCode = HttpStatusCode.Created;

        var topUrls = logsAnalyzer.ComputeTopUrlsIncludingDomain([logLine1, logLine2, logLine3]);

        topUrls.Should().BeInDescendingOrder(tup => tup.Item2);
        topUrls.Should().BeEquivalentTo(
            [new Tuple<string, int>("http://example.net/foo/bar", 1)]);
    }

    [TestMethod]
    public void ComputeTopUrlsIncludingDomain_ExcludesFailureStatusCodes()
    {
        var logsAnalyzer = new LogsAnalyzer();
        var logLine1 = MakeBasicLogLine(new IPAddress([1, 1, 1, 1]), "example.net", "/foo/bar");
        var logLine2 = MakeBasicLogLine(new IPAddress([2, 2, 2, 2]), null, "/foo/bar");
        logLine2.StatusCode = HttpStatusCode.InternalServerError;
        var logLine3 = MakeBasicLogLine(new IPAddress([1, 1, 1, 1]), "example.net", "/not/found");
        logLine3.HttpMethod = HttpMethod.Post;
        logLine3.StatusCode = HttpStatusCode.NotFound;

        var topUrls = logsAnalyzer.ComputeTopUrlsIncludingDomain([logLine1, logLine2, logLine3]);

        topUrls.Should().BeInDescendingOrder(tup => tup.Item2);
        topUrls.Should().BeEquivalentTo(
            [new Tuple<string, int>("http://example.net/foo/bar", 1)]);
    }

    [TestMethod]
    public void ComputeTopUrlsIncludingDomain_InfersDomainFromOtherRequestsWithSameIpAddress()
    {
        var logsAnalyzer = new LogsAnalyzer();
        var logLine1 = MakeBasicLogLine(new IPAddress([1, 1, 1, 1]), "example.net", "/foo/bar");
        var logLine2 = MakeBasicLogLine(new IPAddress([2, 2, 2, 2]), null, "/foo/bar");
        var logLine3 = MakeBasicLogLine(new IPAddress([3, 3, 3, 3]), "example.net", "/foo/bar");
        var logLine4 = MakeBasicLogLine(new IPAddress([4, 4, 4, 4]), "contoso.com", "/");
        var logLine5 = MakeBasicLogLine(new IPAddress([2, 2, 2, 2]), "contoso.com", "/foo/bar");

        var topUrls = logsAnalyzer.ComputeTopUrlsIncludingDomain([logLine1, logLine2, logLine3, logLine4, logLine5]);

        topUrls.Should().BeInDescendingOrder(tup => tup.Item2);
        topUrls.Should().BeEquivalentTo(
            [new Tuple<string, int>("http://contoso.com/foo/bar", 2),
             new Tuple<string, int>("http://example.net/foo/bar", 2),
             new Tuple<string, int>("http://contoso.com/", 1)]);
    }

    [TestMethod]
    public void ComputeTopUrlsIncludingDomain_DoesNotInferDomainIfAmbiguous()
    {
        var logsAnalyzer = new LogsAnalyzer();
        var logLine1 = MakeBasicLogLine(new IPAddress([1, 1, 1, 1]), "example.net", "/foo/bar");
        var logLine2 = MakeBasicLogLine(new IPAddress([2, 2, 2, 2]), null, "/foo/bar");
        var logLine3 = MakeBasicLogLine(new IPAddress([3, 3, 3, 3]), "example.net", "/foo/bar");
        var logLine4 = MakeBasicLogLine(new IPAddress([4, 4, 4, 4]), "contoso.com", "/");
        var logLine5 = MakeBasicLogLine(new IPAddress([2, 2, 2, 2]), "contoso.com", "/foo/bar");
        // IP address 2.2.2.2 can mean either contoso.com or www.contoso.com.
        var logLine6 = MakeBasicLogLine(new IPAddress([2, 2, 2, 2]), "www.contoso.com", "/foo/bar");

        var topUrls = logsAnalyzer.ComputeTopUrlsIncludingDomain([logLine1, logLine2, logLine3, logLine4, logLine5, logLine6]);

        topUrls.Should().BeInDescendingOrder(tup => tup.Item2);
        topUrls.Should().BeEquivalentTo(
            [new Tuple<string, int>("http://example.net/foo/bar", 2),
             new Tuple<string, int>("http://2.2.2.2/foo/bar", 1),
             new Tuple<string, int>("http://contoso.com/", 1)]);
    }

    [TestMethod]
    public void ComputeTopUrlsExcludingDomain_WorksWhenFewerThan3Urls()
    {
        var logsAnalyzer = new LogsAnalyzer();
        var logLine1 = MakeBasicLogLine(new IPAddress([1, 1, 1, 1]), "example.net", "/foo/bar");
        var logLine2 = MakeBasicLogLine(new IPAddress([2, 2, 2, 2]), null, "/foo/bar");
        var logLine3 = MakeBasicLogLine(new IPAddress([3, 3, 3, 3]), "example.net", "/");

        var topUrls = logsAnalyzer.ComputeTopUrlsExcludingDomain([logLine1, logLine2, logLine3]);

        topUrls.Should().BeInDescendingOrder(tup => tup.Item2);
        topUrls.Should().BeEquivalentTo(
            [new Tuple<string, int>("/foo/bar", 2),
             new Tuple<string, int>("/", 1)]);
    }

    [TestMethod]
    public void ComputeTopUrlsExcludingDomain_WorksWhenMoreThan3Urls()
    {
        var logsAnalyzer = new LogsAnalyzer();
        var logLine1 = MakeBasicLogLine(new IPAddress([1, 1, 1, 1]), "example.net", "/foo/bar");
        var logLine2 = MakeBasicLogLine(new IPAddress([2, 2, 2, 2]), null, "/foo/bar");
        var logLine3 = MakeBasicLogLine(new IPAddress([3, 3, 3, 3]), "example.net", "/bar/baz");
        var logLine4 = MakeBasicLogLine(new IPAddress([4, 4, 4, 4]), "contoso.com", "/");
        var logLine5 = MakeBasicLogLine(new IPAddress([5, 5, 5, 5]), "example.com", "/qux");

        var topUrls = logsAnalyzer.ComputeTopUrlsExcludingDomain([logLine1, logLine2, logLine3, logLine4, logLine5]);

        topUrls.Should().BeInDescendingOrder(tup => tup.Item2);
        topUrls.Should().BeEquivalentTo(
            [new Tuple<string, int>("/foo/bar", 2),
             new Tuple<string, int>("/bar/baz", 1),
             new Tuple<string, int>("/", 1)]);
    }

    [TestMethod]
    public void ComputeTopUrlsExcludingDomain_ExcludesNonHttpGetRequests()
    {
        var logsAnalyzer = new LogsAnalyzer();
        var logLine1 = MakeBasicLogLine(new IPAddress([1, 1, 1, 1]), "example.net", "/foo/bar");
        var logLine2 = MakeBasicLogLine(new IPAddress([2, 2, 2, 2]), null, "/foo/bar");
        logLine2.HttpMethod = HttpMethod.Post;
        logLine2.StatusCode = HttpStatusCode.Created;
        var logLine3 = MakeBasicLogLine(new IPAddress([1, 1, 1, 1]), "example.net", "/foo/bar");
        logLine3.HttpMethod = HttpMethod.Post;
        logLine3.StatusCode = HttpStatusCode.Created;

        var topUrls = logsAnalyzer.ComputeTopUrlsExcludingDomain([logLine1, logLine2, logLine3]);

        topUrls.Should().BeInDescendingOrder(tup => tup.Item2);
        topUrls.Should().BeEquivalentTo(
            [new Tuple<string, int>("/foo/bar", 1)]);
    }

    [TestMethod]
    public void ComputeTopUrlsExcludingDomain_ExcludesFailureStatusCodes()
    {
        var logsAnalyzer = new LogsAnalyzer();
        var logLine1 = MakeBasicLogLine(new IPAddress([1, 1, 1, 1]), "example.net", "/foo/bar");
        var logLine2 = MakeBasicLogLine(new IPAddress([2, 2, 2, 2]), null, "/foo/bar");
        logLine2.StatusCode = HttpStatusCode.InternalServerError;
        var logLine3 = MakeBasicLogLine(new IPAddress([1, 1, 1, 1]), "example.net", "/not/found");
        logLine3.HttpMethod = HttpMethod.Post;
        logLine3.StatusCode = HttpStatusCode.NotFound;

        var topUrls = logsAnalyzer.ComputeTopUrlsExcludingDomain([logLine1, logLine2, logLine3]);

        topUrls.Should().BeInDescendingOrder(tup => tup.Item2);
        topUrls.Should().BeEquivalentTo(
            [new Tuple<string, int>("/foo/bar", 1)]);
    }

}