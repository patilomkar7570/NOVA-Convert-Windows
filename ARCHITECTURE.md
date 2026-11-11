# NOVA Convert — Architecture Documentation

## Architectural Overview

NOVA Convert is engineered following modern Clean Architecture principles, strict separation of concerns, and the MVVM (Model-View-ViewModel) design pattern.

```
┌────────────────────────────────────────────────────────┐
│                   NovaConvert.UI                       │
│        (WPF / XAML / MVVM / Minimal Apple-like UI)     │
└───────────────────────────┬────────────────────────────┘
                            │
                            ▼
┌────────────────────────────────────────────────────────┐
│                   NovaConvert.Core                     │
│  - Format Registry (Magic byte signatures & targets)   │
│  - File Inspection Service                             │
│  - Queue & Batch Conversion Manager                    │
│  - SQLite History Repository                           │
│  - Settings & Update Verification                      │
└───────────────────────────┬────────────────────────────┘
                            │
                            ▼
┌────────────────────────────────────────────────────────┐
│                  NovaConvert.Engines                   │
│  - MagickImageEngine (ImageMagick / Magick.NET)        │
│  - FFmpegEngine (Audio / Video / Audio extraction)     │
│  - PdfEngine (UglyToad.PdfPig / Magick.NET)            │
│  - DocumentEngine (OpenXml DOCX / Markdown / HTML)     │
│  - SpreadsheetEngine (XLSX / CSV / JSON tables)        │
│  - ArchiveEngine (ZIP / TAR / GZ repackager)           │
│  - EbookEngine (DRM-compliant EPUB processor)          │
│  - EngineManager (Resolution, health, selection)       │
└────────────────────────────────────────────────────────┘
```

---

## 1. Core Format Compatibility System

Unlike conventional converters that display arbitrary format matrices, NOVA Convert uses a strict compatibility registry:
- **`FormatDefinition`**: Defines exact MIME types, magic numbers, lossless capabilities, transparency support, and whitelisted destination targets.
- **`IdentifyBySignature`**: Inspects up to the first 4096 bytes of incoming files to verify binary file signatures (e.g. `PNG`, `JFIF/JPEG`, `GIF89a`, `RIFF/WEBP`, `%PDF`, `fLaC`, `ID3`, `PK`, `7z`, `ustar`).
- **Mismatch Detection**: When an extension differs from the true binary signature (e.g., a PNG file renamed to `.jpg`), a warning is displayed and the true format is safely routed.

---

## 2. Modular Engine Architecture

Each engine implements `IConversionEngine`:
- `EngineId`, `DisplayName`, `Version`, `License`, `IsAvailable`, `StatusSummary`
- `CanConvert(FormatDefinition source, FormatDefinition target)`
- `ConvertAsync(...)` with progress reporting, cancellation token support, and output validation.

`EngineManager` dynamically queries registered engines to select the optimal engine for each specific task.

---

## 3. Queue & Concurrency Management

- **`ConversionQueueManager`**: Orchestrates batch conversions using `SemaphoreSlim` to limit concurrent encoding tasks based on user settings (defaulting to system core capacity).
- **Process Isolation**: External tools (such as FFmpeg) run as independent child processes with strictly sanitised arguments, pipe redirection, and timeout/cancellation handling.

---

## 4. Storage & Persistence

- **Conversion History**: Uses SQLite via `Microsoft.Data.Sqlite` stored in `%LOCALAPPDATA%\NOVA Convert\conversion_history.db`.
- **Application Settings**: Serialized as JSON in `%LOCALAPPDATA%\NOVA Convert\settings.json`.
- **Output Safety**: By default, unique filenames are generated when collisions occur (e.g., `document (1).pdf`), preventing accidental loss of existing user files.
