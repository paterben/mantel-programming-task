using System.Net;
using FluentAssertions;
using HttpLogsAnalyzer.Models;

namespace HttpLogsAnalyzer.UnitTests;

[TestClass]
public sealed class LogLineParserTests
{
    [TestMethod]
    public void ParseLogLine_WhenAbsoluteUrl_Works()
    {
        var parser = new LogLineParser();
        LogLine logLine = parser.ParseLogLine("""168.41.191.40 - - [09/Jul/2018:22:21:38 +0200] "GET http://example.net/blog/category/meta/ HTTP/1.1" 200 3574""");
        logLine.Should().BeEquivalentTo(new LogLine
        {
            ClientIpAddress = new IPAddress([168, 41, 191, 40]),
            Timestamp = new DateTimeOffset(2018, 7, 9, 22, 21, 38, TimeSpan.FromHours(2)),
            HttpMethod = HttpMethod.Get,
            RequestUri = new Uri("http://example.net/blog/category/meta/"),
            StatusCode = HttpStatusCode.OK
        });
    }

    [TestMethod]
    public void ParseLogLine_WhenRelativeUrl_Works()
    {
        var parser = new LogLineParser();
        LogLine logLine = parser.ParseLogLine("""168.41.191.40 - - [09/Jul/2018:10:10:38 +0200] "GET /blog/category/meta/ HTTP/1.1" 200 3574""");
        logLine.Should().BeEquivalentTo(new LogLine
        {
            ClientIpAddress = new IPAddress([168, 41, 191, 40]),
            Timestamp = new DateTimeOffset(2018, 7, 9, 10, 10, 38, TimeSpan.FromHours(2)),
            HttpMethod = HttpMethod.Get,
            RequestUri = new Uri("/blog/category/meta/", UriKind.Relative),
            StatusCode = HttpStatusCode.OK
        });
    }

    [TestMethod]
    public void ParseLogLine_WhenPostHttpMethod_Works()
    {
        var parser = new LogLineParser();
        LogLine logLine = parser.ParseLogLine("""168.41.191.40 - - [09/Jul/2018:10:10:38 +0200] "POST /blog/category/meta/ HTTP/1.1" 201 3574""");
        logLine.Should().BeEquivalentTo(new LogLine
        {
            ClientIpAddress = new IPAddress([168, 41, 191, 40]),
            Timestamp = new DateTimeOffset(2018, 7, 9, 10, 10, 38, TimeSpan.FromHours(2)),
            HttpMethod = HttpMethod.Post,
            RequestUri = new Uri("/blog/category/meta/", UriKind.Relative),
            StatusCode = HttpStatusCode.Created
        });
    }

    [TestMethod]
    public void ParseLogLine_WhenStarUrlAndOptionsHttpMethod_Works()
    {
        var parser = new LogLineParser();
        LogLine logLine = parser.ParseLogLine("""168.41.191.40 - - [09/Jul/2018:10:10:38 +0200] "OPTIONS * HTTP/1.1" 200 3574""");
        logLine.Should().BeEquivalentTo(new LogLine
        {
            ClientIpAddress = new IPAddress([168, 41, 191, 40]),
            Timestamp = new DateTimeOffset(2018, 7, 9, 10, 10, 38, TimeSpan.FromHours(2)),
            HttpMethod = HttpMethod.Options,
            RequestUri = new Uri("*", UriKind.Relative),
            StatusCode = HttpStatusCode.OK
        });
    }

    [TestMethod]
    public void ParseLogLine_WhenIPv6Address_Works()
    {
        var parser = new LogLineParser();
        LogLine logLine = parser.ParseLogLine("""2607:f8b0:400d:c06::69 - - [09/Jul/2018:10:10:38 +0200] "GET /blog/category/meta/ HTTP/1.1" 200 3574""");
        logLine.Should().BeEquivalentTo(new LogLine
        {
            ClientIpAddress = new IPAddress([0x26, 0x07, 0xf8, 0xb0, 0x40, 0x0d, 0x0c, 0x06, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x69]),
            Timestamp = new DateTimeOffset(2018, 7, 9, 10, 10, 38, TimeSpan.FromHours(2)),
            HttpMethod = HttpMethod.Get,
            RequestUri = new Uri("/blog/category/meta/", UriKind.Relative),
            StatusCode = HttpStatusCode.OK
        });
    }

    [TestMethod]
    public void ParseLogLine_WhenExtraParts_Works()
    {
        var parser = new LogLineParser();
        LogLine logLine = parser.ParseLogLine("""168.41.191.40 - - [09/Jul/2018:22:21:38 +0200] "GET http://example.net/blog/category/meta/ HTTP/1.1" 200 3574 "-" "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_6_7) AppleWebKit/534.24 (KHTML, like Gecko) RockMelt/0.9.58.494 Chrome/11.0.696.71 Safari/534.24" and even more garbage""");
        logLine.Should().BeEquivalentTo(new LogLine
        {
            ClientIpAddress = new IPAddress([168, 41, 191, 40]),
            Timestamp = new DateTimeOffset(2018, 7, 9, 22, 21, 38, TimeSpan.FromHours(2)),
            HttpMethod = HttpMethod.Get,
            RequestUri = new Uri("http://example.net/blog/category/meta/"),
            StatusCode = HttpStatusCode.OK
        });
    }

    [TestMethod]
    // HTTP request URIs shouldn't normally contain query params, however we handle them anyway.
    public void ParseLogLine_WhenQueryParams_Works()
    {
        var parser = new LogLineParser();
        LogLine logLine = parser.ParseLogLine("""168.41.191.40 - - [09/Jul/2018:10:10:38 +0200] "GET /blog/category/meta?foo=bar HTTP/1.1" 200 3574""");
        logLine.Should().BeEquivalentTo(new LogLine
        {
            ClientIpAddress = new IPAddress([168, 41, 191, 40]),
            Timestamp = new DateTimeOffset(2018, 7, 9, 10, 10, 38, TimeSpan.FromHours(2)),
            HttpMethod = HttpMethod.Get,
            RequestUri = new Uri("/blog/category/meta?foo=bar", UriKind.Relative),
            StatusCode = HttpStatusCode.OK
        });
    }

    [TestMethod]
    public void ParseLogLine_WhenEmptyLine_ThrowsException()
    {
        var parser = new LogLineParser();
        try
        {
            LogLine logLine = parser.ParseLogLine("");
            Assert.Fail();
        }
        catch (Exception e)
        {
            e.Should().BeOfType<FormatException>();
            e.Message.Should().Contain("Log line has too few parts");
        }
    }

    [TestMethod]
    public void ParseLogLine_WhenTooFewParts_ThrowsException()
    {
        var parser = new LogLineParser();
        try
        {
            LogLine logLine = parser.ParseLogLine("""168.41.191.40 - - [09/Jul/2018:10:10:38 +0200] "GET /blog/category/meta/ HTTP/1.1" """);
            Assert.Fail();
        }
        catch (Exception e)
        {
            e.Should().BeOfType<FormatException>();
            e.Message.Should().Contain("Log line has too few parts");
        }
    }

    [TestMethod]
    public void ParseLogLine_WhenInvalidIPAddress_ThrowsException()
    {
        var parser = new LogLineParser();
        try
        {
            LogLine logLine = parser.ParseLogLine("""256.1.2.3 - - [09/Jul/2018:10:10:38 +0200] "POST /blog/category/meta/ HTTP/1.1" 201 3574""");
            Assert.Fail();
        }
        catch (Exception e)
        {
            e.Should().BeOfType<FormatException>();
            e.Message.Should().Contain("invalid IP address");
        }
    }

    [TestMethod]
    public void ParseLogLine_WhenInvalidTimestamp_ThrowsException()
    {
        var parser = new LogLineParser();
        try
        {
            LogLine logLine = parser.ParseLogLine("""168.41.191.40 - - [09/13/2018:10:10:38 +0200] "POST /blog/category/meta/ HTTP/1.1" 201 3574""");
            Assert.Fail();
        }
        catch (Exception e)
        {
            e.Should().BeOfType<FormatException>();
            e.Message.Should().Contain("not recognized as a valid DateTime");
        }
    }

    [TestMethod]
    public void ParseLogLine_WhenInvalidHttpRequestSummary_ThrowsException()
    {
        var parser = new LogLineParser();
        try
        {
            LogLine logLine = parser.ParseLogLine("""168.41.191.40 - - [09/Jul/2018:10:10:38 +0200] "GET 1 2 3 4 5" 201 3574""");
            Assert.Fail();
        }
        catch (Exception e)
        {
            e.Should().BeOfType<FormatException>();
            e.Message.Should().Contain("Failed to parse HTTP request summary into its components");
        }
    }
}
