# NOVA Convert — Security Audit & Hardening Report

**Audit Date**: November 2026  
**Target Release**: v1.0.0 (Windows Desktop Edition)  
**Security Posture**: Hardened, 100% Offline & Sandboxed Local Execution

---

## 1. Threat Analysis & Audited Vectors

### 1.1 Command Injection & Process Execution
- **Risk**: Process arguments containing shell metacharacters or quotation breakouts could allow arbitrary command execution via child processes (`ffmpeg.exe`, `soffice.exe`).
- **Mitigation & Fixes**:
  - `ProcessStartInfo.UseShellExecute` is explicitly set to `false` for every external process invocation.
  - File paths (`sourcePath`, `outputPath`, `targetDir`) are sanitized by escaping embedded double quotes (`\"`).
  - Video resolution strings, codec identifiers, and bitrate parameters are strictly validated using whitelist regex (`^[a-zA-Z0-9_\-]+$` and `^[0-9xX:\-]+$`) to eliminate command flag injection.

### 1.2 Path Traversal & Zip Slip (Archives)
- **Risk**: Archives containing paths like `../../Windows/System32/evil.exe` could overwrite arbitrary filesystem targets upon decompression.
- **Mitigation & Fixes**:
  - `ZipFile.ExtractToDirectory` and `TarFile.ExtractToDirectory` (built into modern .NET runtime) employ strict Zip Slip guards and raise `IOException` if any entry target escapes the destination folder.
  - SharpCompress 7z extraction uses `ExtractFullPath = true` with destination bounding verification.
  - Temporary extraction folders are created under unique GUID subdirectories in `%TEMP%` and purged immediately in a guaranteed `finally` block.

### 1.3 Incomplete / Corrupted File Exposure
- **Risk**: User cancellations or conversion engine failures leaving 0-byte or corrupt files in destination folders.
- **Mitigation & Fixes**:
  - `ConversionQueueManager` catches both `OperationCanceledException` and generic `Exception` blocks and deletes any partially generated output file immediately.
  - Post-conversion output validation asserts `File.Exists(output)` and `FileInfo.Length > 0` before declaring completion.

### 1.4 Unsafe File Handling & Directory Traversal
- **Risk**: Specially crafted file paths or malicious relative path queries.
- **Mitigation & Fixes**:
  - Path normalization via `Path.GetFullPath`, `Path.Combine`, and `Directory.CreateDirectory`.
  - Windows naming collisions handled automatically via `SettingsService.ResolveOutputPath` with `(1)`, `(2)` incrementing, preventing accidental overwriting.

### 1.5 Malicious Input Files & Memory Safety
- **Risk**: Malformed image, audio, or video headers crafted to cause memory corruption in parsers.
- **Mitigation & Fixes**:
  - Managed memory boundaries in C# (.NET 10.0 runtime) prevent buffer overflows in core code.
  - External engines run in isolated child process spaces (`ffmpeg.exe`) with execution timeouts and cancellation tokens.
  - Native ImageMagick Q8 runs with strict format validators and managed wrapper memory boundaries.

### 1.6 Offline Privacy & Zero Telemetry
- **Risk**: Leaks of sensitive files, document contents, or metadata to external cloud servers.
- **Mitigation & Fixes**:
  - NOVA Convert has **zero telemetry**, **zero cloud dependencies**, and **zero network data transmission**.
  - All conversions, format analyses, and SQLite database logging run 100% locally on the host computer.

---

## 2. Residual Risks & Best Practices
- **External Office Converters**: If LibreOffice is installed and utilized, ensure the host machine's LibreOffice distribution is regularly patched by the system administrator.
- **Untrusted Files**: Users should exercise standard Windows caution when handling files from unverified online sources.
