# NOVA Convert — Format Compatibility Matrix (v1.0.0)

Every advertised conversion in NOVA Convert is backed by a verified conversion engine and tested for production readiness.

## 1. Image Formats

| Format | Extension | Input | Output | Engine | Quality / Fidelity | Limitations & Notes |
|---|---|---|---|---|---|---|
| PNG | `.png` | Yes | Yes | ImageMagick | Lossless / Full Alpha | Default target for lossless graphics, screenshots, transparency. |
| JPEG / JPG | `.jpg`, `.jpeg`, `.jfif` | Yes | Yes | ImageMagick | Lossy (Adjustable 1–100) | No transparency support; transparent pixels composite to white background. |
| WebP | `.webp` | Yes | Yes | ImageMagick | Lossless or Lossy | High compression efficiency; supports full alpha channel. |
| TIFF | `.tiff`, `.tif` | Yes | Yes | ImageMagick | Lossless | Multi-channel, high bit-depth raster publishing format. |
| BMP | `.bmp` | Yes | Yes | ImageMagick | Lossless | Uncompressed Windows raster format; large file size; no alpha. |
| GIF | `.gif` | Yes | Yes | ImageMagick / FFmpeg | Lossless (Indexed 256 colors) | 1-bit transparency; supports multi-frame animations. |
| ICO | `.ico` | Yes | Yes | ImageMagick | Lossless | Standard Windows icon format; supports transparency. |
| SVG | `.svg` | Yes | Yes (Raster to SVG wrapper) | ImageMagick | Vector Input | Vector paths render to crisp bitmaps at specified target DPI. |
| AVIF | `.avif` | Yes | Yes | ImageMagick | Lossless / Lossy | Next-generation image standard based on AV1 intra-frames. |
| TGA | `.tga` | Yes | Yes | ImageMagick | Lossless | Truevision format used extensively in gaming and textures. |
| PSD | `.psd` | Yes | Yes | ImageMagick | Lossless | Adobe Photoshop layered documents; layers merged on export. |

---

## 2. Audio Formats

| Format | Extension | Input | Output | Engine | Quality / Fidelity | Limitations & Notes |
|---|---|---|---|---|---|---|
| MP3 | `.mp3` | Yes | Yes | FFmpeg (libmp3lame) | Lossy (128–320 kbps) | Universal player compatibility; ID3v2 metadata supported. |
| WAV | `.wav` | Yes | Yes | FFmpeg (pcm_s16le) | Lossless PCM | Uncompressed 16-bit / 24-bit PCM audio; large file sizes. |
| FLAC | `.flac` | Yes | Yes | FFmpeg (flac) | Lossless (Bit-perfect) | Open lossless compressed standard; preserves exact waveform. |
| AAC | `.aac` | Yes | Yes | FFmpeg (aac) | Lossy (96–320 kbps) | High efficiency at lower bitrates; standard for mobile/web. |
| M4A | `.m4a` | Yes | Yes | FFmpeg (aac) | Lossy / Lossless | Apple MP4 audio container; supports AAC and ALAC codecs. |
| Ogg Vorbis | `.ogg`, `.oga` | Yes | Yes | FFmpeg (libvorbis) | Lossy | Royalty-free open codec; popular in Linux and game audio. |
| Opus | `.opus` | Yes | Yes | FFmpeg (libopus) | Lossy (Ultra-efficient) | Exceptional fidelity at low bitrates (64–192 kbps); low latency. |
| WMA | `.wma` | Yes | Yes | FFmpeg (wmav2) | Lossy | Legacy Windows Media Audio format. |
| AIFF | `.aiff`, `.aif` | Yes | Yes | FFmpeg (pcm_s16be) | Lossless PCM | Uncompressed Apple audio interchange format. |

---

## 3. Video Formats & Video-to-Audio Extraction

| Container | Extension | Input | Output | Default Codecs | Quality / Fidelity | Notes |
|---|---|---|---|---|---|---|
| MPEG-4 | `.mp4`, `.m4v` | Yes | Yes | H.264 (libx264) + AAC | Lossless CRF / Lossy | Universal video container; playback on 99.9% of devices. |
| Matroska | `.mkv` | Yes | Yes | H.264 / H.265 / AV1 + AAC | Lossless CRF / Lossy | Flexible open standard; multi-track audio and subtitle support. |
| WebM | `.webm` | Yes | Yes | VP9 (libvpx-vp9) + Opus | Lossy | HTML5 web video standard; royalty-free and open. |
| QuickTime | `.mov` | Yes | Yes | H.264 / ProRes + AAC | Lossy / Editing | Standard macOS/Apple QuickTime video format. |
| AVI | `.avi` | Yes | Yes | MPEG-4 Part 2 + MP3/PCM | Lossy | Microsoft legacy multimedia container. |
| Windows Media | `.wmv` | Yes | Yes | WMV2 + WMAv2 | Lossy | Legacy Windows Media Video container. |
| Flash Video | `.flv` | Yes | Yes | FLV1 + MP3 | Lossy | Legacy web streaming format. |
| MPEG Transport | `.ts` | Yes | Yes | H.264 + AAC | Lossy | Broadcast transport stream container. |
| MPEG Program | `.mpg`, `.mpeg` | Yes | Yes | MPEG-2 (mpeg2video) + MP2 | Lossy | DVD and legacy broadcast standard. |
| 3GPP Mobile | `.3gp` | Yes | Yes | H.263 + AMR-NB / AAC | Lossy | Compact mobile phone format. |
| Ogg Video | `.ogv` | Yes | Yes | Theora + Vorbis | Lossy | Open source video format. |
| Animated GIF | `.gif` | Yes | Yes | Palettegen + Lanczos | 256 Colors | Smooth 2-pass palette generation for video clips. |

*Audio Extraction*: Any video format can be directly converted into any supported audio format (`.mp3`, `.wav`, `.flac`, `.aac`, `.m4a`, `.ogg`, `.opus`) via high-speed `-vn` extraction.

---

## 4. Documents & Portable Document Format (PDF)

| Format | Extension | Input | Output | Engine | Fidelity | Limitations & Notes |
|---|---|---|---|---|---|---|
| PDF | `.pdf` | Yes | Yes | PdfPig / ImageMagick / LibreOffice | High / Layout-preserved | PDF to text extracts clean text content; image conversion renders high-DPI raster pages. |
| Word Document | `.docx` | Yes | Yes (from TXT/MD/HTML) | OpenXml / LibreOffice | High for text/structure | Built-in OpenXml engine converts DOCX to Markdown, HTML, and plain text. |
| Plain Text | `.txt` | Yes | Yes | Native Text Engine | Bit-perfect (UTF-8) | Clean universal plain text. |
| Markdown | `.md` | Yes | Yes | Native Markup Engine | Bit-perfect | CommonMark formatted document parsing. |
| HTML | `.html`, `.htm` | Yes | Yes | Native Web Engine | Bit-perfect | HTML structure parsed into clean Markdown, plain text, or PDF. |
| Rich Text | `.rtf` | Yes | Yes (to TXT/HTML) | Native RTF Parser | Text & Formatting | RTF control words stripped to yield clean text or formatted HTML. |

---

## 5. Presentations

| Format | Extension | Input | Output | Engine | Fidelity | Limitations & Notes |
|---|---|---|---|---|---|---|
| PowerPoint | `.pptx` | Yes | Yes (to TXT, MD, PDF) | OpenXml / LibreOffice | Text & Slide Extraction | Extracts slide titles, body text, notes, and tables into clean TXT or MD. Converts to PDF if LibreOffice is detected. |

---

## 6. Spreadsheets & Tabular Data

| Format | Extension | Input | Output | Engine | Fidelity | Limitations & Notes |
|---|---|---|---|---|---|---|
| Excel Workbook | `.xlsx` | Yes | Yes (from CSV) | OpenXml | High / Data-only | Multi-sheet export to CSV, JSON, and HTML tables. Complex formulas evaluate to values. |
| Comma-Separated | `.csv` | Yes | Yes | Native Tabular Engine | Lossless tabular | Parses RFC 4180 CSV tables into Excel XLSX, JSON arrays, and HTML tables. |
| JSON Data | `.json` | Yes | Yes | Native JSON Engine | Lossless tabular | Tabular JSON object arrays flatten directly into CSV and XLSX. |

---

## 7. E-books

| Format | Extension | Input | Output | Engine | Fidelity | Limitations & Notes |
|---|---|---|---|---|---|---|
| EPUB | `.epub` | Yes | Yes (to TXT, HTML) | Native E-Book Processor | High / Reflowable | Unpacks chapters and stylesheets into continuous HTML or plain text. DRM-protected books are strictly rejected per copyright law. |

---

## 8. Archives

| Format | Extension | Input | Output | Engine | Fidelity | Limitations & Notes |
|---|---|---|---|---|---|---|
| ZIP | `.zip` | Yes | Yes | .NET Compression / ZipFile | Lossless | Standard Deflate / Optimal compression levels; preserves folder hierarchy. |
| TAR | `.tar` | Yes | Yes | .NET Formats Tar | Lossless | POSIX tarball preserving file permissions and structure without compression. |
| GZ | `.gz` | Yes | Yes | .NET GZipStream | Lossless | Single file or tarball gzip compression (`.tar.gz`). |
| 7Z | `.7z` | Yes | Yes (to ZIP/TAR) | SharpCompress / Native Repackager | Lossless | High-compression 7-Zip archives unpacked and repackaged into ZIP or TAR. |
