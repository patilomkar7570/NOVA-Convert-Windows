# NOVA Convert
**Every format. One place.**

*A product of NOVA Software.*

![NOVA Convert Logo](assets/brand/nova-logo.png)

NOVA Convert is an offline-first file conversion workstation built exclusively for **Windows Desktop**. It provides one clean, unified interface for converting a broad range of real-world file formats across images, audio, video, documents, PDFs, spreadsheets, presentations, e-books, and archives.

Built with minimalism and clarity, the interface stays clean and intuitive while running powerful conversion engines locally on your computer with zero telemetry and 100% privacy.

---

## Target Platform & Distribution

- **Platform**: Windows Desktop (Windows 10 version 1809+ and Windows 11, 64-bit x64).
- **Package**: `NOVA-Convert-Setup.exe` (production Windows installer built with Inno Setup 7).
- **No Portable Edition**: Distributed solely as a proper Windows installer with Start Menu integration, optional desktop shortcut, clean uninstallation, and self-contained engines.

---

## Quick Download & Installation

1. Download **`NOVA-Convert-Setup.exe`** from the latest GitHub Release.
2. Run `NOVA-Convert-Setup.exe`.
3. Follow the setup wizard to complete the installation.
4. Launch **NOVA Convert** directly from your Start Menu or Desktop.

---

## Key Features

1. **Automatic Light / Dark Theme**:
   - Automatically detects your Windows system theme by default (`AppsUseLightTheme`).
   - Updates dynamically when your Windows theme switches.
   - Dedicated single theme control located in **Settings** (`Auto`, `Dark Theme`, `Light Theme`).
2. **Fixed & Reliable Conversion History**:
   - Local conversion history stored in SQLite (`%LOCALAPPDATA%\NOVA Convert\conversion_history.db`).
   - History panel stays closed on startup and opens smoothly when requested.
   - Reliable close behavior via close button, Escape key, or clicking the background overlay.
3. **Comprehensive Format Compatibility**:
   - Every advertised format is genuinely supported and verified.
   - Automatic binary magic-byte inspection prevents corrupted conversions from mismatched file extensions.
   - Complete matrix documented in [FORMAT_COMPATIBILITY_MATRIX.md](FORMAT_COMPATIBILITY_MATRIX.md).
4. **Hardened Security & Sandboxed Child Processes**:
   - Shell command injection protected via non-shell process execution and argument sanitization.
   - Zip Slip / path traversal protection on archives.
   - Automatic cleanup of incomplete files upon cancellation or error.
   - Full security audit report documented in [SECURITY_AUDIT_REPORT.md](SECURITY_AUDIT_REPORT.md).
5. **Automated Test Suite**:
   - 57 automated tests covering all engines, special characters in filenames, paths with spaces, empty files, and error handling.
   - Full test report documented in [TEST_RESULTS.md](TEST_RESULTS.md).

---

## Supported Format Summary

| Category | Input Formats | Target Output Formats | Engines Used |
|---|---|---|---|
| **Images** | `PNG`, `JPG`, `JPEG`, `WEBP`, `BMP`, `TIFF`, `GIF`, `ICO`, `SVG`, `AVIF`, `TGA`, `PSD` | `PNG`, `JPG`, `WEBP`, `BMP`, `TIFF`, `GIF`, `ICO`, `AVIF`, `PDF`, `TGA`, `PSD` | ImageMagick Q8 (Embedded) |
| **Audio** | `MP3`, `WAV`, `FLAC`, `AAC`, `M4A`, `OGG`, `Opus`, `WMA`, `AIFF` | `MP3`, `WAV`, `FLAC`, `AAC`, `M4A`, `OGG`, `Opus`, `WMA`, `AIFF` | FFmpeg 7.x (Bundled) |
| **Video** | `MP4`, `MKV`, `WebM`, `MOV`, `AVI`, `WMV`, `FLV`, `TS`, `MPG`, `3GP`, `OGV` | `MP4`, `MKV`, `WebM`, `MOV`, `AVI`, `WMV`, `FLV`, `TS`, `MPG`, `3GP`, `OGV`, `GIF` | FFmpeg 7.x (Bundled) |
| **Audio Extraction** | Any supported video container | `MP3`, `WAV`, `FLAC`, `AAC`, `M4A`, `OGG`, `Opus` | FFmpeg 7.x (Bundled `-vn`) |
| **Documents** | `DOCX`, `TXT`, `MD`, `HTML`, `RTF` | `PDF`, `TXT`, `HTML`, `MD` | OpenXml + LibreOffice (if installed) |
| **PDF** | `PDF` | `PNG`, `JPG`, `TIFF`, `TXT` | PdfPig + ImageMagick |
| **Presentations** | `PPTX` | `TXT`, `MD`, `PDF` | OpenXml + LibreOffice (if installed) |
| **Spreadsheets** | `XLSX`, `CSV`, `JSON` | `CSV`, `XLSX`, `JSON`, `HTML` | OpenXml + Native Tabular |
| **E-Books** | `EPUB` (DRM-free) | `TXT`, `HTML` | Native EPUB Engine |
| **Archives** | `ZIP`, `TAR`, `GZ` | `ZIP`, `TAR`, `GZ` | .NET Native Compression |

---

## System Requirements

- **Operating System**: Windows 10 (version 1809 or newer) or Windows 11.
- **Architecture**: 64-bit x64 processor.
- **Disk Space**: ~350 MB free disk space for bundled conversion engines and runtimes.
- **Memory**: 4 GB RAM minimum (8 GB recommended for heavy video rendering).

---

## License

Copyright © 2026 NOVA Software. All rights reserved.  
Third-party component notices and licenses are detailed in [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).
