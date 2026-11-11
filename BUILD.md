# Building NOVA Convert

This document describes how to build, test, and package **NOVA Convert** from source on Windows.

---

## Prerequisites

1. **Windows 10 / Windows 11 (64-bit)**
2. **.NET 10 SDK** (version 10.0.100 or newer)
3. **Inno Setup 6.x or 7.x** (with `iscc` available on PATH or default Program Files)
4. **Git**

---

## Repository Structure

```
Nova File Converter/
├── assets/brand/           # Official NOVA logo and icon variants
├── engines/ffmpeg/         # Bundled FFmpeg & FFprobe binaries
├── installer/              # Inno Setup compilation script (NovaConvert.iss)
├── NovaConvert.Core/       # Format registry, inspection, models, SQLite repository
├── NovaConvert.Engines/    # ImageMagick, FFmpeg, PDF, OpenXml, Archive, Ebook adapters
├── NovaConvert.UI/         # WPF MVVM Minimalist Desktop Application
├── NovaConvert.Tests/      # Automated xUnit unit and integration tests
├── dist/                   # Output folder for NOVA-Convert-Setup.exe
└── publish/                # Staging folder for compiled binaries
```

---

## 1. Compile and Run Automated Tests

To verify and execute all 24 unit and integration tests:

```powershell
dotnet test NovaConvert.sln --verbosity normal
```

All tests should pass.

---

## 2. Publish Application Binaries

To produce an optimized Release build for 64-bit Windows:

```powershell
dotnet publish NovaConvert.UI/NovaConvert.UI.csproj -c Release -r win-x64 --self-contained false -o publish\win-x64
```

Copy the bundled FFmpeg binaries to the publish destination:

```powershell
New-Item -ItemType Directory -Path publish\win-x64\engines\ffmpeg -Force | Out-Null
Copy-Item "engines\ffmpeg\*" publish\win-x64\engines\ffmpeg -Force
```

---

## 3. Generate Public Installer

Compile the Inno Setup script to generate `NOVA-Convert-Setup.exe`:

```powershell
& "ISCC.exe" installer\NovaConvert.iss
```

The resulting setup executable will be located at:

```
dist\NOVA-Convert-Setup.exe
```

No portable release or zip package should be generated.
