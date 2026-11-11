# NOVA Convert
**Every format. One place.**

*A product of NOVA Software.*

![NOVA Convert Logo](assets/brand/nova-logo.png)

NOVA Convert is an offline-first Windows desktop conversion workstation designed to convert a broad range of real-world file formats across images, audio, video, documents, PDFs, spreadsheets, e-books, and archives.

Built with Apple-like minimalism and clarity, the interface stays clean and intuitive while providing a modular engine underneath.

---

## Key Features

- **Effortless Drag-and-Drop Workflow**: Simply drag files or folders into the application, select an output format, and convert with one click.
- **Deep Signature & Header Inspection**: Inspects true binary magic bytes instead of blindly trusting file extensions. Alerts users if an incoming file was mislabeled or renamed.
- **Strict Format Compatibility**: Never displays impossible or doomed conversion targets. Only genuinely supported destinations are offered.
- **Multi-Engine Modular Architecture**:
  - **FFmpeg**: Hardware-accelerated audio/video transcoding, audio extraction, and video-to-GIF generation.
  - **ImageMagick (Embedded Magick.NET)**: Raster format conversions (PNG, JPEG, WebP, TIFF, BMP, GIF, ICO, AVIF, SVG), quality controls, aspect-ratio scaling, and alpha preservation.
  - **UglyToad.PdfPig & Magick.NET**: High-fidelity PDF rendering and structured text stream extraction.
  - **OpenXML Engine**: DOCX extraction, conversion to HTML, Markdown, and TXT; XLSX tabular data parsing to CSV, JSON, and HTML.
  - **Native Archive Repackager**: Repackaging between ZIP, TAR, and GZ with compression controls.
  - **DRM-Compliant E-Book Adapter**: Parses standard EPUB structures into clean HTML or plain text while strictly respecting Digital Rights Management boundaries.
- **Batch Processing**: Convert single files, multiple files from different directories, or entire folder trees with independent or unified target formats.
- **100% Offline & Private**: Zero cloud uploads, zero telemetry, zero background network pings. All conversions happen entirely on your computer.
- **Persistent Conversion History**: Lightweight SQLite database logs conversion timestamps, source/destination file sizes, duration, and engines used.
- **Native Windows Installer**: Distributed as a single standard `NOVA-Convert-Setup.exe` package with Start Menu integration, desktop shortcut option, and clean uninstaller.

---

## Supported Formats

| Category | Formats | Common Conversions |
| :--- | :--- | :--- |
| **Images** | PNG, JPG, JPEG, WebP, TIFF, BMP, GIF, ICO, AVIF, SVG | PNG ↔ JPG, WebP ↔ PNG, SVG → PNG, Image ↔ PDF, ICO generation |
| **Audio** | MP3, WAV, FLAC, AAC, M4A, OGG, Opus, WMA | WAV → MP3/FLAC/OGG, M4A → MP3, Bitrate & sample-rate adjustment |
| **Video** | MP4, MKV, MOV, WebM, AVI, WMV | MP4 ↔ WebM, MKV → MP4, Video → GIF, Audio extraction (Video → MP3) |
| **Documents** | DOCX, TXT, HTML, Markdown (MD), RTF | DOCX → TXT/HTML/MD/PDF, Markdown → HTML, HTML → TXT |
| **Spreadsheets** | XLSX, CSV, JSON, HTML | XLSX → CSV/JSON/HTML, CSV → JSON/HTML table |
| **PDF** | PDF | PDF → PNG/JPG/TIFF images, PDF → TXT text stream |
| **E-Books** | EPUB | EPUB → HTML, EPUB → TXT (DRM-free only) |
| **Archives** | ZIP, TAR, GZ | Repackaging ZIP ↔ TAR ↔ GZ |

---

## Installation

Download the official setup package:

```
NOVA-Convert-Setup.exe
```

Double-click to install. The installer will register NOVA Convert under Program Files, create Start Menu shortcuts, and bundle all conversion engines.

---

## System Requirements

- **Operating System**: Windows 10 (version 1809 or newer) or Windows 11.
- **Architecture**: 64-bit x64 processor.
- **Disk Space**: ~350 MB free disk space for bundled engines and dependencies.
- **Memory**: 4 GB RAM minimum (8 GB recommended for video conversions).

---

## License

Copyright © 2026 NOVA Software. All rights reserved.
Third-party component licenses are detailed in [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).
