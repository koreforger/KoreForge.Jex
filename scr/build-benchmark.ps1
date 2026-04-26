[CmdletBinding()]
param()

Push-Location (Resolve-Path "$PSScriptRoot\..")
try {
    dotnet run --project tst/KF.Jex.Benchmarks/KF.Jex.Benchmarks.csproj -c Release
} finally {
    Pop-Location
}