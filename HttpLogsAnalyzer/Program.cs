using System.CommandLine;

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
        using StreamReader logFileReader = logFile.OpenText();
        int numLines = 0;
        string? line;
        while ((line = await logFileReader.ReadLineAsync(cancellationToken)) != null)
        {
            numLines++;
        }
        if (numLines == 0)
        {
            throw new ArgumentException($"Log file {logFile.FullName} is empty.");
        }
        Console.WriteLine($"Num lines in log file: {numLines}");
    }
}
