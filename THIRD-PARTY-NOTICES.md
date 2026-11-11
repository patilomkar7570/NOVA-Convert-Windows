# Third-Party Notices & Component Inventory

NOVA Convert incorporates and interfaces with open-source software and libraries. This document provides an inventory of these components, their licensing models, and their respective project details.

---

## Bundled & Referenced Components

### 1. FFmpeg & FFprobe
- **Component**: FFmpeg multimedia processing suite (win64 build)
- **Version**: 7.x
- **License**: GNU General Public License (GPL) v3 / LGPL v2.1+
- **Usage**: Audio and video transcoding, stream demuxing, video-to-GIF rendering, and audio track extraction.
- **Distribution**: Bundled in `engines\ffmpeg\` as an independent external binary executable with isolated process boundaries.
- **Website**: https://ffmpeg.org

### 2. Magick.NET (ImageMagick)
- **Component**: Magick.NET-Q8-AnyCPU and Magick.Native
- **Version**: 15.1.0
- **License**: ImageMagick License (Apache 2.0 compatible, permissive)
- **Usage**: High-fidelity image manipulation, format transcoding, resizing, and PDF rendering.
- **Website**: https://github.com/dlemstra/Magick.NET

### 3. UglyToad.PdfPig
- **Component**: UglyToad.PdfPig
- **Version**: 1.7.0
- **License**: Apache License 2.0
- **Usage**: Document text extraction from PDF files.
- **Website**: https://github.com/UglyToad/PdfPig

### 4. DocumentFormat.OpenXml
- **Component**: DocumentFormat.OpenXml
- **Version**: 3.5.1
- **License**: MIT License
- **Usage**: Parsing DOCX documents and XLSX spreadsheets.
- **Website**: https://github.com/dotnet/Open-XML-SDK

### 5. Microsoft.Data.Sqlite & SQLitePCLRaw
- **Component**: SQLite Database Engine wrapper for .NET
- **Version**: 10.0 / 3.x
- **License**: MIT License / Public Domain (SQLite)
- **Usage**: Persistent local conversion history logging.
- **Website**: https://github.com/dotnet/efcore

### 6. SharpCompress
- **Component**: SharpCompress compression library
- **Version**: 0.50.8
- **License**: MIT License
- **Usage**: Archive decompression and repackaging.
- **Website**: https://github.com/adamhathcock/sharpcompress

### 7. Optional Adapters (External)
- **LibreOffice**: LGPL / MPL (when installed on host machine, headless document conversion is supported).
- **Pandoc**: GPL v2+ (when installed on host machine, document markup conversion is supported).
