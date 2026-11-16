# NOVA Convert v1.0.0 — Release Notes

## Overview
**NOVA Convert v1.0.0 (Windows Desktop Edition)** is the official stable release of NOVA Software's universal media and file format conversion workstation.

---

## What's New in v1.0.0

### 1. Fix Conversation History Panel
- Resolved the critical startup overlay glitch where the SQLite Conversation History modal opened automatically and remained uncloseable.
- Explicitly initialized the modal state to collapsed upon launch.
- Enabled reliable closure via top-right ✕ button, primary Close button, Escape key binding, and clicking the dimmed backdrop overlay.
- Full SQLite conversion history is maintained and persisted in `%LOCALAPPDATA%\NOVA Convert\conversion_history.db`.

### 2. Dynamic Automatic Theme System
- Detects the Windows system theme (`AppsUseLightTheme` registry setting) by default.
- Subscribes to `SystemEvents.UserPreferenceChanged` to update application theme instantly in real time when Windows switches between dark and light modes.
- Added a single dedicated theme selector in **Settings** supporting `Auto (System Default)`, `Dark Theme`, and `Light Theme`.
- Eliminated redundant or conflicting theme toggles elsewhere in the interface.

### 3. Comprehensive Format Support Matrix
- Added support for new video containers: **MPEG Transport Stream (`.ts`)**, **MPEG Program Stream (`.mpg`, `.mpeg`)**, **3GPP (`.3gp`)**, and **Ogg Video (`.ogv`)**.
- Added PowerPoint presentation (`.pptx`) text and slide extraction.
- Published a full compatibility matrix in `FORMAT_COMPATIBILITY_MATRIX.md` verifying inputs, outputs, quality, and limitations across 9 categories (Images, Audio, Video, Documents, PDFs, Spreadsheets, Presentations, E-books, and Archives).

### 4. Conversion Engine Hardening & Security
- **Command Injection Protection**: Enforced non-shell process execution (`UseShellExecute = false`) and argument sanitization for FFmpeg and LibreOffice invocations.
- **Path Traversal Guards**: Protected archive repacking with Zip Slip mitigation.
- **Partial File Purge**: Incomplete, 0-byte, or cancelled outputs are automatically deleted from disk immediately upon cancellation or failure.
- **Offline & Zero Telemetry**: 100% of conversion and logging operations run locally on the machine.

### 5. Automated Verification
- 57 automated tests executed and verified with 100% pass rate.
- Tested edge cases including paths containing spaces, special symbols in filenames, 0-byte files, and corrupted data streams.

---

## Installation
Download and run **`NOVA-Convert-Setup.exe`**.  
The installer deploys the 64-bit application, Start Menu shortcuts, uninstaller, and self-contained FFmpeg/FFprobe conversion engines.
