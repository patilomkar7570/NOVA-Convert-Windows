# Troubleshooting Guide

This guide covers common questions and solutions for **NOVA Convert**.

---

## 1. File Extension Mismatch Warnings

**Symptom**: A warning banner appears stating: *"File is named 'xyz.jpg' with extension '.jpg', but file signature identifies it as 'Portable Network Graphics' (PNG)."*

**Explanation**: NOVA Convert inspects the true binary header bytes of incoming files. If someone renamed a file extension manually, NOVA Convert alerts you and will automatically use the correct conversion engine corresponding to the genuine file type.

---

## 2. Converting Video Formats

**Symptom**: Video conversion takes longer than image conversion.

**Explanation**: Video re-encoding (e.g. H.264, VP9) is computationally intensive.
- **Tip**: In the bottom bar or Advanced Options, choosing **Balanced** or **Smaller File** presets will optimize encoding speed.
- If you only need the audio from a video, select an audio format (such as `.mp3` or `.wav`) as the destination format, which completes much faster by extracting the audio stream.

---

## 3. PDF to Word or Document Conversion

**Symptom**: Complex layout in a converted text document looks simplified.

**Explanation**: PDF is a fixed-layout page representation language. Converting PDF to plain text or editable formats extracts text streams accurately, but intricate desktop-publishing layouts or scanned image text cannot be reconstructed as formatted word processor structures without OCR. NOVA Convert displays a notice explaining this behavior.

---

## 4. Protected E-Books (DRM)

**Symptom**: Error message stating: *"This e-book is protected by DRM (Digital Rights Management)."*

**Explanation**: In accordance with international copyright and anti-circumvention laws, NOVA Convert does not bypass DRM locks. Only DRM-free, open EPUB files can be converted.

---

## 5. File Collisions

**Symptom**: An existing file is not replaced when converting.

**Explanation**: By default, NOVA Convert protects your original files and existing converted files by appending an incrementing index (e.g. `file (1).png`). To change this behavior, open **Settings** and adjust the **File Name Collisions** policy.
