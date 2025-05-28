# HTTP Logs Analyzer

A basic tool for parsing a file containing HTTP logs and analyzing the contents.

Currently the tool just outputs the number of lines to the console.

## Cloning the repo

TODO

## Building and running

Building the repo requires the .NET 8 SDK, available at https://dotnet.microsoft.com/en-us/download/dotnet/8.0.

You can open the solution in Visual Studio (untested) or VSCode, or run the following from the top-level directory:

```shell
dotnet run --project HttpLogsAnalyzer -- --logFile "data\example-data.log"
```

To see all possible command-line arguments, run `dotnet run --project HttpLogsAnalyzer -- --help`.

You can run all tests with `dotnet test`.

## Assumptions

TODO

## Future work

TODO