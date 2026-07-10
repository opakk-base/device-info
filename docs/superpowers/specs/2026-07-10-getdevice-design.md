# GetDevice — Design Specification

## Overview

A WPF desktop application for Windows that collects device information, lets users select which fields to export via a checklist, protects settings access with a configurable password, and outputs filtered JSON. Optionally runs an HTTP endpoint on localhost to serve device info. Packaged as an MSI installer.

## Stack

- **Framework**: .NET 8 / WPF
- **Pattern**: MVVM
- **Packaging**: WiX Toolset v4 → MSI
- **Config**: JSON (`appsettings.json`) stored in `%LOCALAPPDATA%\GetDevice`
- **Device Data**: WMI via `System.Management` (Win32_ComputerSystem, Win32_NetworkAdapterConfiguration, etc.)
- **HTTP Server**: `System.Net.HttpListener` (built-in, no extra dependencies)

## Project Structure

```
GetDevice/
├── Models/
│   ├── DeviceInfo.cs
│   └── AppConfig.cs
├── Services/
│   ├── DeviceInfoService.cs      (WMI queries)
│   ├── PasswordService.cs         (hash, verify, change)
│   ├── ExportService.cs           (filtered JSON export)
│   └── HttpServerService.cs      (localhost HTTP listener)
├── ViewModels/
│   ├── MainViewModel.cs
│   └── SettingsViewModel.cs
├── Views/
│   ├── MainWindow.xaml / .xaml.cs
│   ├── LoginWindow.xaml / .xaml.cs
│   └── SettingsWindow.xaml / .xaml.cs
├── Converters/
│   └── BoolToVisibilityConverter.cs
├── appsettings.json
└── Program.cs
```

## Windows & Flow

| Window | Purpose |
|---|---|
| **LoginWindow** | Password gate on app launch. Default password `12345678`. Shows hint: "Reset by deleting appsettings.json" |
| **MainWindow** | Displays device info fields with checkboxes. Select All / Select None toggle. Export button. System tray minimize. |
| **SettingsWindow** | Change password (current + new + confirm). HTTP server toggle. Factory reset button. |

### App Launch Flow

1. App reads `%LOCALAPPDATA%\GetDevice\appsettings.json`
2. If no config exists, create with default password hash (`12345678`), all fields checked, HTTP disabled
3. Show **LoginWindow** — user enters password, app verifies hash
4. On success → show **MainWindow**
5. On first-ever launch → immediately prompt to change default password

### System Tray

- App minimizes to system tray on close/minimize
- Tray icon shows tooltip: "GetDevice — HTTP: Running/Stopped"
- Right-click menu: Show / Exit
- Tray icon indicates HTTP server status (green/red dot overlay)

## Data Model

### DeviceInfo

```
device_id:     GUID (generated once, persisted in config)
device_name:   Environment.MachineName
hostname:      Dns.GetHostName()
os:            Windows version string (e.g., "Windows 11 Pro 23H2")
arch:          Environment.Is64BitOperatingSystem → "x64" / "ARM64"
mac_address:   Primary network adapter MAC (WMI)
ip_address:    Primary IPv4 address (WMI)
client_key:    SHA-256 hash of (device_id + machine_sid), generated once
timestamp:     ISO 8601 UTC
```

### AppConfig (appsettings.json)

```json
{
  "password_hash": "sha256-of-default-12345678",
  "device_id": "3fac064c-f7ef-4bad-812d-15607a6c61ef",
  "client_key": "d8edd98a85d248633276b463415419b41f12611c393c957109e32e70b123d422",
  "http_enabled": false,
  "http_port": 8080,
  "checked_fields": [
    "device_id", "device_name", "hostname", "os",
    "arch", "mac_address", "ip_address", "client_key", "timestamp"
  ]
}
```

## Password System

- Stored as SHA-256 hash in `appsettings.json`
- Default: `12345678`
- Change: user enters current password → verify hash → write new hash
- Recovery: delete or manually edit `appsettings.json`

## Export

- User clicks Export → Save File dialog (default: `GetDevice-export.json`)
- Only fields with checked checkboxes are included in output
- Output JSON matches the shape from the user's example

## HTTP Server

- Runs on `http://localhost:8080` (port configurable in settings)
- Toggle in SettingsWindow to start/stop
- Endpoints:
  - `GET /getdevice` → filtered JSON (same checklist)
  - `GET /health` → `{"status":"ok"}`
- No authentication (localhost-only)
- Server runs in background thread, independent of GUI
- System tray icon indicates running status

## MSI Packaging

- WiX Toolset v4
- Install to `Program Files\GetDevice`
- Create shortcut in Start Menu
- App data config at `%LOCALAPPDATA%\GetDevice`
- No admin elevation required at runtime (only for install)
