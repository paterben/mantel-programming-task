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
        int numUniqueClientIpAddresses = logsAnalyzer.CountUniqueClientIpAddresses(logLines);
        IList<Tuple<IPAddress, int>> topClientIpAddresses = logsAnalyzer.ComputeTopClientIpAddresses(logLines);
        IList<Tuple<string, int>> topUrls = logsAnalyzer.ComputeTopUrls(logLines);
        Console.WriteLine($"Number of unique client IP addresses: {numUniqueClientIpAddresses}");
        Console.WriteLine($"Top 3 client IPs and associated request counts:");
        foreach ((IPAddress ipAddress, int frequency) in topClientIpAddresses)
        {
            Console.WriteLine($"    {ipAddress}: {frequency}");
        }
        Console.WriteLine($"Top 3 URLs (in abs_path form) and associated request counts:");
        foreach ((string url, int frequency) in topUrls)
        {
            Console.WriteLine($"    {url}: {frequency}");
        }
    }
}
