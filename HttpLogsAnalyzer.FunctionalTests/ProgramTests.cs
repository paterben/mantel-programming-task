using FluentAssertions;

namespace HttpLogsAnalyzer.FunctionalTests;

[TestClass]
public sealed class ProgramTests
{
    const string kReadAndAnalyzeLogsExpectedConsoleOutput = """
Number of unique client IP addresses: 11
Top 3 client IPs and associated request counts:
    168.41.191.40: 4
    177.71.128.21: 3
    50.112.0.11: 3
Top 3 URLs (in abs_path form) and associated request counts:
    /faq/: 2
    /docs/manage-websites/: 2
    /intranet-analytics/: 1
""";

    [TestMethod]
    public async Task ReadAndAnalyzeLogsAsync_Works()
    {
        using StringWriter console = new();
        Console.SetOut(console);

        FileInfo logFile = new(@"data\example-data.log");
        await Program.ReadAndAnalyzeLogsAsync(logFile, CancellationToken.None);

        console.ToString().Should().Contain(kReadAndAnalyzeLogsExpectedConsoleOutput);
    }
}
