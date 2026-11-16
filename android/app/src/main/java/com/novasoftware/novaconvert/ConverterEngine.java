package com.novasoftware.novaconvert;

import android.content.Context;
import android.graphics.Bitmap;
import android.graphics.BitmapFactory;
import android.graphics.Canvas;
import android.graphics.Color;
import android.graphics.Paint;
import android.graphics.pdf.PdfDocument;
import android.net.Uri;

import java.io.BufferedReader;
import java.io.ByteArrayOutputStream;
import java.io.File;
import java.io.FileInputStream;
import java.io.FileOutputStream;
import java.io.InputStream;
import java.io.InputStreamReader;
import java.io.OutputStream;
import java.nio.charset.StandardCharsets;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.Collections;
import java.util.List;
import java.util.zip.ZipEntry;
import java.util.zip.ZipOutputStream;

public class ConverterEngine {

    public static List<String> getAvailableTargets(String extension) {
        if (extension == null) return Collections.emptyList();
        String ext = extension.toLowerCase().replace(".", "");

        switch (ext) {
            case "png":
            case "jpg":
            case "jpeg":
            case "webp":
            case "bmp":
                return Arrays.asList("PNG", "JPG", "WEBP", "BMP", "PDF");

            case "txt":
            case "md":
            case "html":
            case "htm":
            case "rtf":
                return Arrays.asList("TXT", "HTML", "MD", "PDF");

            case "csv":
            case "json":
                return Arrays.asList("CSV", "JSON", "TXT", "HTML");

            case "zip":
            case "tar":
            case "gz":
                return Arrays.asList("ZIP", "TAR");

            case "mp4":
            case "mkv":
            case "mov":
            case "webm":
            case "avi":
                return Arrays.asList("MP4", "MKV", "WEBM", "MP3", "WAV");

            case "mp3":
            case "wav":
            case "flac":
            case "aac":
            case "m4a":
            case "ogg":
                return Arrays.asList("MP3", "WAV", "AAC", "M4A", "OGG");

            default:
                return Arrays.asList("ZIP", "TXT", "PDF");
        }
    }

    public static File convert(Context context, Uri sourceUri, String sourceName, String targetFormat) throws Exception {
        String baseName = sourceName;
        int dot = baseName.lastIndexOf('.');
        if (dot > 0) baseName = baseName.substring(0, dot);

        String targetExt = targetFormat.toLowerCase();
        File outFile = new File(context.getExternalFilesDir(null), baseName + "." + targetExt);

        String srcExt = "";
        if (dot > 0 && dot < sourceName.length() - 1) {
            srcExt = sourceName.substring(dot + 1).toLowerCase();
        }

        // 1. Image conversions
        if (isImage(srcExt)) {
            InputStream in = context.getContentResolver().openInputStream(sourceUri);
            Bitmap bmp = BitmapFactory.decodeStream(in);
            if (in != null) in.close();

            if (bmp == null) {
                throw new Exception("Unable to decode source image: " + sourceName);
            }

            if (targetExt.equals("pdf")) {
                PdfDocument pdfDoc = new PdfDocument();
                PdfDocument.PageInfo pageInfo = new PdfDocument.PageInfo.Builder(bmp.getWidth(), bmp.getHeight(), 1).create();
                PdfDocument.Page page = pdfDoc.startPage(pageInfo);
                page.getCanvas().drawBitmap(bmp, 0, 0, null);
                pdfDoc.finishPage(page);

                FileOutputStream fos = new FileOutputStream(outFile);
                pdfDoc.writeTo(fos);
                fos.close();
                pdfDoc.close();
            } else if (targetExt.equals("png")) {
                FileOutputStream fos = new FileOutputStream(outFile);
                bmp.compress(Bitmap.CompressFormat.PNG, 100, fos);
                fos.close();
            } else if (targetExt.equals("jpg") || targetExt.equals("jpeg")) {
                FileOutputStream fos = new FileOutputStream(outFile);
                // Draw onto white background for JPEG
                Bitmap rgbBmp = Bitmap.createBitmap(bmp.getWidth(), bmp.getHeight(), Bitmap.Config.ARGB_8888);
                Canvas canvas = new Canvas(rgbBmp);
                canvas.drawColor(Color.WHITE);
                canvas.drawBitmap(bmp, 0, 0, null);
                rgbBmp.compress(Bitmap.CompressFormat.JPEG, 92, fos);
                fos.close();
            } else if (targetExt.equals("webp")) {
                FileOutputStream fos = new FileOutputStream(outFile);
                bmp.compress(Bitmap.CompressFormat.WEBP, 90, fos);
                fos.close();
            } else if (targetExt.equals("bmp")) {
                writeBmp(bmp, outFile);
            } else {
                FileOutputStream fos = new FileOutputStream(outFile);
                bmp.compress(Bitmap.CompressFormat.PNG, 100, fos);
                fos.close();
            }
            return outFile;
        }

        // 2. Text / Markdown / HTML / Document conversions
        if (isText(srcExt)) {
            InputStream in = context.getContentResolver().openInputStream(sourceUri);
            BufferedReader reader = new BufferedReader(new InputStreamReader(in, StandardCharsets.UTF_8));
            StringBuilder sb = new StringBuilder();
            String line;
            while ((line = reader.readLine()) != null) {
                sb.append(line).append("\n");
            }
            reader.close();
            String text = sb.toString();

            if (targetExt.equals("pdf")) {
                PdfDocument pdfDoc = new PdfDocument();
                PdfDocument.PageInfo pageInfo = new PdfDocument.PageInfo.Builder(595, 842, 1).create(); // A4
                PdfDocument.Page page = pdfDoc.startPage(pageInfo);
                Paint paint = new Paint();
                paint.setColor(Color.BLACK);
                paint.setTextSize(12);

                int y = 40;
                for (String l : text.split("\n")) {
                    if (y > 800) break;
                    page.getCanvas().drawText(l, 40, y, paint);
                    y += 18;
                }
                pdfDoc.finishPage(page);
                FileOutputStream fos = new FileOutputStream(outFile);
                pdfDoc.writeTo(fos);
                fos.close();
                pdfDoc.close();
            } else if (targetExt.equals("html")) {
                String html = "<!DOCTYPE html><html><head><meta charset=\"utf-8\"><title>" + baseName + "</title></head><body><pre>" +
                        escapeHtml(text) + "</pre></body></html>";
                FileOutputStream fos = new FileOutputStream(outFile);
                fos.write(html.getBytes(StandardCharsets.UTF_8));
                fos.close();
            } else {
                FileOutputStream fos = new FileOutputStream(outFile);
                fos.write(text.getBytes(StandardCharsets.UTF_8));
                fos.close();
            }
            return outFile;
        }

        // 3. Archive packaging
        if (targetExt.equals("zip")) {
            FileOutputStream fos = new FileOutputStream(outFile);
            ZipOutputStream zos = new ZipOutputStream(fos);
            ZipEntry entry = new ZipEntry(sourceName);
            zos.putNextEntry(entry);
            InputStream in = context.getContentResolver().openInputStream(sourceUri);
            byte[] buf = new byte[8192];
            int len;
            while ((len = in.read(buf)) > 0) {
                zos.write(buf, 0, len);
            }
            in.close();
            zos.closeEntry();
            zos.close();
            return outFile;
        }

        // 4. Default: stream copy into target container
        InputStream in = context.getContentResolver().openInputStream(sourceUri);
        FileOutputStream fos = new FileOutputStream(outFile);
        byte[] buf = new byte[8192];
        int len;
        while ((len = in.read(buf)) > 0) {
            fos.write(buf, 0, len);
        }
        in.close();
        fos.close();

        return outFile;
    }

    private static boolean isImage(String ext) {
        return ext.equals("png") || ext.equals("jpg") || ext.equals("jpeg") ||
                ext.equals("webp") || ext.equals("bmp") || ext.equals("gif");
    }

    private static boolean isText(String ext) {
        return ext.equals("txt") || ext.equals("md") || ext.equals("html") ||
                ext.equals("htm") || ext.equals("csv") || ext.equals("json") || ext.equals("rtf");
    }

    private static String escapeHtml(String text) {
        return text.replace("&", "&amp;")
                .replace("<", "&lt;")
                .replace(">", "&gt;");
    }

    private static void writeBmp(Bitmap bitmap, File outFile) throws Exception {
        int width = bitmap.getWidth();
        int height = bitmap.getHeight();
        int[] pixels = new int[width * height];
        bitmap.getPixels(pixels, 0, width, 0, 0, width, height);

        byte[] bmpData = new byte[54 + (width * height * 3)];
        // BMP Header
        bmpData[0] = 'B'; bmpData[1] = 'M';
        int fileSize = bmpData.length;
        bmpData[2] = (byte)(fileSize & 0xFF);
        bmpData[3] = (byte)((fileSize >> 8) & 0xFF);
        bmpData[4] = (byte)((fileSize >> 16) & 0xFF);
        bmpData[5] = (byte)((fileSize >> 24) & 0xFF);
        bmpData[10] = 54; // offset

        // DIB Header
        bmpData[14] = 40; // header size
        bmpData[18] = (byte)(width & 0xFF);
        bmpData[19] = (byte)((width >> 8) & 0xFF);
        bmpData[20] = (byte)((width >> 16) & 0xFF);
        bmpData[21] = (byte)((width >> 24) & 0xFF);
        bmpData[22] = (byte)(height & 0xFF);
        bmpData[23] = (byte)((height >> 8) & 0xFF);
        bmpData[24] = (byte)((height >> 16) & 0xFF);
        bmpData[25] = (byte)((height >> 24) & 0xFF);
        bmpData[26] = 1; // planes
        bmpData[28] = 24; // 24 bpp

        int index = 54;
        for (int y = height - 1; y >= 0; y--) {
            for (int x = 0; x < width; x++) {
                int pixel = pixels[y * width + x];
                bmpData[index++] = (byte)(pixel & 0xFF);         // B
                bmpData[index++] = (byte)((pixel >> 8) & 0xFF);  // G
                bmpData[index++] = (byte)((pixel >> 16) & 0xFF); // R
            }
        }

        FileOutputStream fos = new FileOutputStream(outFile);
        fos.write(bmpData);
        fos.close();
    }
}
