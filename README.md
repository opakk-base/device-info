# GetDevice

Windows desktop application that gathers system/device information and exports it as JSON.

## Features

- **Device Info Gathering** — collects device ID, hostname, OS version, architecture, MAC address, IPv4 address, client key, and timestamp
- **Selective JSON Export** — choose which fields to include before exporting
- **HTTP API Server** — optional embedded server exposing `GET /getdevice` for remote queries
- **Password Protection** — SHA256-hashed login gates access to the GUI
- **System Tray** — minimize to tray with quick access to show, start/stop server, and exit
- **Single Instance** — prevents multiple copies from running simultaneously

## Requirements

### Install via MSI (pre-built installer)

- Windows 10 or Windows 11 (64-bit)

The installer is self-contained — no .NET runtime required.

### Build from source

- [.NET 8.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
- [WiX Toolset v7](https://wixtoolset.org/) — install as a global dotnet tool:

  ```
  dotnet tool install --global wix
  ```

## Quick Start

```powershell
# Build the application
make build

# Run the application
make run

# Run tests
make test

# Build the MSI installer
make msi
```

Use `CONFIG=Release` with `make build` or `make run` for a release build:

```powershell
make build CONFIG=Release
```

## Project Structure

```
├── src/GetDevice/          # WPF application (.NET 8, x64)
├── tests/GetDevice.Tests/  # xUnit tests
├── installer/              # WiX v7 MSI installer
├── docs/                   # Planning documents
└── Makefile                # Build orchestration
```
