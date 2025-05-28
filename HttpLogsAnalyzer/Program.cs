using System.CommandLine;
using System.Net;
using HttpLogsAnalyzer.Models;

namespace HttpLogsAnalyzer;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        var logFileOption = new Option<FileInfo>(
                      name: "--logFile",
                      description: "The log file of HTTP requests.")
        { IsRequired = true };

        var rootCommand = new RootCommand("Analyzes a log file of HTTP requests and outputs statistics to console.");
        rootCommand.AddOption(logFileOption);

        rootCommand.SetHandler(async (logFile) =>
            {
                await ReadAndAnalyzeLogsAsync(logFile, CancellationToken.None);
            },
            logFileOption);

        return await rootCommand.InvokeAsync(args);
    }

    public static async Task ReadAndAnalyzeLogsAsync(FileInfo logFile, CancellationToken cancellationToken)
    {
        LogLineParser logLineParser = new();
        LogFileParser logFileParser = new(logLineParser);
        IList<LogLine> logLines = await logFileParser.ParseLogFileAsync(logFile, cancellationToken);
        LogsAnalyzer logsAnalyzer = new();
        int numUniqueIpAddresses = logsAnalyzer.CountUniqueIpAddresses(logLines);
        IList<Tuple<string, int>> topUrlsIncludingDomain = logsAnalyzer.ComputeTopUrlsIncludingDomain(logLines);
        IList<Tuple<string, int>> topUrlsExcludinggDomain = logsAnalyzer.ComputeTopUrlsExcludingDomain(logLines);
        IList<Tuple<IPAddress, int>> topIpAddresses = logsAnalyzer.ComputeTopIpAddresses(logLines);
        Console.WriteLine($"Number of unique IP addresses: {numUniqueIpAddresses}");
        Console.WriteLine($"Top 3 URLs (including domain) and associated frequency:");
        foreach ((string url, int frequency) in topUrlsIncludingDomain)
        {
            Console.WriteLine($"    {url}: {frequency}");
        }
        Console.WriteLine($"Top 3 URLs (excluding domain) and associated frequency:");
        foreach ((string url, int frequency) in topUrlsExcludinggDomain)
        {
            Console.WriteLine($"    {url}: {frequency}");
        }
        Console.WriteLine($"Top 3 IPs and associated frequency:");
        foreach ((IPAddress ipAddress, int frequency) in topIpAddresses)
        {
            Console.WriteLine($"    {ipAddress}: {frequency}");
        }
    }
}
