$ErrorActionPreference = "Stop";

dotnet tool restore
dotnet run --project build -- $args
