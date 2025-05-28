# HTTP Logs Analyzer

A basic tool for parsing a file containing HTTP logs and analyzing the contents.

Computes the number of unique IP addresses, the top 3 URLs by frequency, and the top 3 IP addresses by frequency and writes the results to the console.

## Features

Calculates the top 3 URLs in two different manners, one ignoring and one incorporating the domain name:

*   When ignoring the domain name, only the absolute path e.g. `/` or `/foo/bar` is considered.
*   When incorporating the domain name, the full domain name + path is considered. In addition, the tool tries to infer the domain name from other log lines with the same IP. For example, if it sees a request for `IP=1.1.1.1, GET http://example.net/foo/bar` and another request for `IP=1.1.1.1, GET /foo/bar`, this will count as two requests for `GET http://example.net/foo/bar`. The tool won't attempt inference for IP addresses associated with multiple different domain names in the logs.

## Assumptions and limitations

*   Assumes that the log file contains logs of **outgoing** requests, i.e. the IP address listed is that on which the URL is hosted.
*   For the purpose of calculating top URLs, non-HTTP GET requests are ignored as well as requests with failure HTTP status codes. The assumption is that these are not indicative of a URL's popularity. However, these requests are treated normally for determining IP address counts.
*   The tool can handle requests of the form `GET http://example.com/foo.bar` or `GET /foo/bar`. However, if it sees a request of the form `GET example.com/foo.bar`, it will currently treat the `example.com` as part of the URL path and not the host name.
*   The tool doesn't perform reverse-DNS lookup to determine domain names, it just looks at what it finds in the logs.
*   Ignores query params, e.g. `/foo/bar` and `/foo/bar?baz=qux` are treated the same.
*   `http://` and `https://` are treated the same (these will appear as `http://` in the URLs with domain name).
*   User agent is ignored.
*   I wasn't able to determine the meaning of some of the fields in the logs. These fields are ignored.

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