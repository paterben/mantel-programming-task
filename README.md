# HTTP Logs Analyzer

A basic tool for parsing a file containing HTTP logs and analyzing the contents.

Computes:
*   the number of unique client IP addresses
*   the top 3 IP addresses by request count
*   the top 3 URLs by request count

Writes results to the console output.

## Assumptions and limitations

*   Assumes that the log file contains logs of **incoming** requests, i.e. the IP address listed is that of the client.
*   For the purpose of calculating top URLs:
    *   Requests with failure HTTP status codes are ignored. The assumption is that these are not indicative of a URL's popularity.
    *   Only GET requests are considered. This is debatable however certain methods like OPTIONS and CONNECT allow special kinds of URLs which would complicate things.
    *   Only the abs_path (e.g. `/foo/bar` is considered). Absolute URLs (e.g. `http://example.com/foo/bar`) are first converted to abs_path format. The HTTP spec allows servers to differentiate resources by Host (see https://datatracker.ietf.org/doc/html/rfc2616#section-5.2), but we assume this is not the case here.
    *   Star URLs (`*`), which are technically allowed by the HTTP spec, are ignored.
*   Log timestamp is parsed but ignored in the analysis.
*   User agent is ignored.
*   I wasn't able to determine the meaning of some of the other fields in the logs. These fields are ignored.
*   Testing was only done on limited sample data so not all edge cases may have been anticipated correctly.

## Cloning the repo

To clone, run:

```shell
git clone https://github.com/paterben/mantel-programming-task.git
```

## Building and running

Building the repo requires the .NET 8 SDK, available at https://dotnet.microsoft.com/en-us/download/dotnet/8.0.

You can open the solution in Visual Studio (untested) or VSCode, or run the following from the top-level directory:

```shell
dotnet run --project HttpLogsAnalyzer -- --logFile "data\example-data.log"
```

To see all possible command-line arguments, run `dotnet run --project HttpLogsAnalyzer -- --help`.

You can run all tests with `dotnet test`.

## Future work

*   Other output options (e.g. JSON).
*   Allow differentiating URLs based on host.
*   Collect more data to ensure log parsing is resilient.
