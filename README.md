# Evolve-GymApp
Evolve: web platform for a gym with a basketball academy. Microservices on .NET Aspire, PostgreSQL and React. Unified court reservations with overlap detection, class capacity under concurrency (optimistic vs pessimistic locking), waitlist auto promotion, constraint based tournament scheduling and live updates via SignalR. MSc thesis.

## Running locally

### First run on a new machine: warm up the Aspire CLI cache

`dotnet run` on the AppHost launches the Aspire CLI through `dnx`, pinned to the `Aspire.AppHost.Sdk` version in `global.json`. The build first checks the CLI with a hard 120 second timeout. On a cold cache the download can take longer, and the run fails with:

```text
DNX could not restore or run aspire.cli@13.6.5 (exit code -1)
```

Run this once to download the CLI with no timeout (Windows, PowerShell):

```powershell
dotnet exec "C:\Program Files\dotnet\sdk\10.0.203\dotnet.dll" dnx --yes aspire.cli@13.6.5 -- --version
```

The SDK folder and the `aspire.cli` version must match `global.json`. Update this command whenever either is upgraded. Background: [ADR 0007](docs/adr/0007-aspire-apphost-pinning-cli-servicedefaults.md).

### Start

```powershell
dotnet run --project platform\Evolve.AppHost
```
