using System.Net;
using FluentAssertions;
using HttpLogsAnalyzer.Models;

namespace HttpLogsAnalyzer.UnitTests;

[TestClass]
// Note: These tests use the real LogLineParser implementation.
public sealed class LogFileParserTests
{
    [TestMethod]
    public async Task ParseLogFile_Works()
    {
        var lineParser = new LogLineParser();
        var parser = new LogFileParser(lineParser);
        IList<LogLine> logLines = await parser.ParseLogFileAsync(new FileInfo(@"testdata\correct.log"), CancellationToken.None);
        var logLine1 = new LogLine
        {
            ClientIpAddress = new IPAddress([177, 71, 128, 21]),
            Timestamp = new DateTimeOffset(2018, 7, 10, 22, 21, 28, TimeSpan.FromHours(2)),
            HttpMethod = HttpMethod.Get,
            RequestUri = new Uri("/intranet-analytics/", UriKind.Relative),
            StatusCode = HttpStatusCode.OK
        };
        var logLine2 = new LogLine
        {
            ClientIpAddress = new IPAddress([168, 41, 191, 40]),
            Timestamp = new DateTimeOffset(2018, 7, 9, 10, 11, 30, TimeSpan.FromHours(2)),
            HttpMethod = HttpMethod.Get,
            RequestUri = new Uri("http://example.net/faq/"),
            StatusCode = HttpStatusCode.OK
        };
        var logLine3 = new LogLine
        {
            ClientIpAddress = new IPAddress([168, 41, 191, 41]),
            Timestamp = new DateTimeOffset(2018, 7, 11, 17, 41, 30, TimeSpan.FromHours(2)),
            HttpMethod = HttpMethod.Get,
            RequestUri = new Uri("/this/page/does/not/exist/", UriKind.Relative),
            StatusCode = HttpStatusCode.NotFound
        };
        var expectedLogLines = new LogLine[] { logLine1, logLine2, logLine3 };
        logLines.Should().BeEquivalentTo(expectedLogLines);
    }

    [TestMethod]
    public async Task ParseEmptyLogFile_ThrowsException()
    {
        var lineParser = new LogLineParser();
        var parser = new LogFileParser(lineParser);
        try
        {
            IList<LogLine> logLines = await parser.ParseLogFileAsync(new FileInfo(@"testdata\empty.log"), CancellationToken.None);
            Assert.Fail();
        }
        catch (Exception e)
        {
            e.Should().BeOfType<ArgumentException>();
            e.Message.Should().MatchRegex("Log file .+ is empty");
        }
    }

    [TestMethod]
    public async Task ParseLogFileWithLineError_ThrowsException()
    {
        var lineParser = new LogLineParser();
        var parser = new LogFileParser(lineParser);
        try
        {
            IList<LogLine> logLines = await parser.ParseLogFileAsync(new FileInfo(@"testdata\missing-url.log"), CancellationToken.None);
            Assert.Fail();
        }
        catch (Exception e)
        {
            e.Should().BeOfType<LogFileParserException>();
            e.Message.Should().Contain("Exception occurred while parsing line 2 of log file");
            e.InnerException.Should().NotBeNull();
            e.InnerException.Should().BeOfType<FormatException>();
        }
    }
}
