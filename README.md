# Evolve-GymApp
Evolve: web platform for a gym with a basketball academy. Microservices on .NET Aspire, PostgreSQL and React. Unified court reservations with overlap detection, class capacity under concurrency (optimistic vs pessimistic locking), waitlist auto promotion, constraint based tournament scheduling and live updates via SignalR. MSc thesis.

## Running locally

### First run on a new machine: warm up the Aspire CLI cache

`dotnet run` on the AppHost launches the Aspire CLI through `dnx`, pinned to the `AspireVersion` property in `Directory.Packages.props`. The build first checks the CLI with a hard 120 second timeout. On a cold cache the download can take longer (about 3 minutes for 17.0.0), and the run fails with:

```text
DNX could not restore or run aspire.cli@17.0.0 (exit code -1)
```

Run this once to download the CLI with no timeout (Windows, PowerShell):

```powershell
dotnet exec "C:\Program Files\dotnet\sdk\10.0.203\dotnet.dll" dnx --yes aspire.cli@17.0.0 -- --version
```

The SDK folder must match `global.json` and the `aspire.cli` version must match `AspireVersion`. Update this command whenever either is upgraded. The CLI ships as a small `aspire.cli` package plus a platform specific one (for example `aspire.cli.win-x64`), so the warm up is needed once per machine and per platform, including CI runners. Background: [ADR 0007](docs/adr/0007-aspire-apphost-pinning-cli-servicedefaults.md), [ADR 0008](docs/adr/0008-aspire-17-apphost-as-package.md).

### Start

```powershell
dotnet run --project platform\Evolve.AppHost
```
