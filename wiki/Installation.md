# Installation

## Prerequisites

- [.NET 8.0](https://dotnet.microsoft.com/download/dotnet/8.0), [.NET 9.0](https://dotnet.microsoft.com/download/dotnet/9.0), or [.NET 10.0](https://dotnet.microsoft.com/download/dotnet/10.0) SDK
- Supported platforms: Windows, Linux, macOS

## Install as .NET tool (recommended)

Install globally to use the `sherlock` command in your terminal:

```bash
dotnet tool install --global Sherlock.Net.Cli
```

After installation:

```bash
sherlock user123
```

### Update

```bash
dotnet tool update --global Sherlock.Net.Cli
```

### Uninstall

```bash
dotnet tool uninstall --global Sherlock.Net.Cli
```

> **Note:** Make sure `~/.dotnet/tools` (Linux/macOS) or `%USERPROFILE%\.dotnet\tools` (Windows) is in your `PATH`.

## Build from source

```bash
git clone https://github.com/totpero/Sherlock.Net.git
cd Sherlock.Net
dotnet build
```

Run without installing:

```bash
dotnet run --project src/Sherlock.Net.Cli -- <username>
```

## Publish as single executable

```bash
# Windows
dotnet publish src/Sherlock.Net.Cli -c Release -r win-x64 --self-contained

# Linux
dotnet publish src/Sherlock.Net.Cli -c Release -r linux-x64 --self-contained

# macOS (Apple Silicon)
dotnet publish src/Sherlock.Net.Cli -c Release -r osx-arm64 --self-contained
```

## Install as NuGet library

For use in your own .NET projects:

```bash
dotnet add package Sherlock.Net
```

See [[Library Usage]] for integration examples.
