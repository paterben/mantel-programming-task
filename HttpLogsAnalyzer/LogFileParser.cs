using System.Diagnostics;
using HttpLogsAnalyzer.Models;

namespace HttpLogsAnalyzer;

// Simple exception wrapper for LogFileParser.
public class LogFileParserException : Exception
{
    public LogFileParserException()
    {
    }

    public LogFileParserException(string message)
        : base(message)
    {
    }

    public LogFileParserException(string message, Exception inner)
        : base(message, inner)
    {
    }
}

public class LogFileParser
{
    private ILogLineParser _logLineParser;

    public LogFileParser(ILogLineParser logLineParser)
    {
        _logLineParser = logLineParser;
    }

    /// <summary>
    /// Parses the given log file and returns a list of structured log lines, one per line in the file.
    /// </summary>
    /// <returns>The log lines.</returns>
    /// <exception cref="ArgumentException">If the log file is empty.</exception>
    /// <exception cref="LogFileParserException">If an exception was thrown while parsing any line in the log file. The inner exception will contain more information.</exception>
    public async Task<IList<LogLine>> ParseLogFileAsync(FileInfo logFile, CancellationToken cancellationToken)
    {
        using StreamReader logFileReader = logFile.OpenText();
        LogLineParser parser = new();
        List<LogLine> logLines = [];
        int numLines = 0;
        string? line;
        while ((line = await logFileReader.ReadLineAsync(cancellationToken)) != null)
        {
            numLines++;
            try
            {
                LogLine logLine = _logLineParser.ParseLogLine(line);
                logLines.Add(logLine);
            }
            catch (Exception exc)
            {
                throw new LogFileParserException($"Exception occurred while parsing line {numLines} of log file {logFile.FullName}", exc);
            }
        }
        if (numLines == 0)
        {
            throw new ArgumentException($"Log file {logFile.FullName} is empty.");
        }
        Debug.WriteLine($"Num lines in log file: {numLines}");
        return logLines;
    }
}
