package com.novasoftware.novaconvert;

import android.app.Activity;
import android.content.Intent;
import android.database.Cursor;
import android.net.Uri;
import android.os.Bundle;
import android.os.Handler;
import android.os.Looper;
import android.provider.OpenableColumns;
import android.view.View;
import android.widget.ArrayAdapter;
import android.widget.Button;
import android.widget.ProgressBar;
import android.widget.Spinner;
import android.widget.TextView;
import android.widget.Toast;

import java.io.File;
import java.util.ArrayList;
import java.util.List;
import java.util.Locale;

public class MainActivity extends Activity {

    private static final int REQ_PICK_FILE = 1001;

    private TextView txtSelectedFile;
    private Spinner spinnerTargetFormat;
    private Button btnSelectFile;
    private Button btnConvert;
    private ProgressBar progressBar;
    private TextView txtStatus;
    private View resultCard;
    private TextView txtResultDetail;
    private Button btnShare;
    private Button btnOpen;

    private Uri selectedFileUri;
    private String selectedFileName;
    private File lastConvertedFile;
    private final Handler mainHandler = new Handler(Looper.getMainLooper());

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_main);

        txtSelectedFile = findViewById(R.id.txtSelectedFile);
        spinnerTargetFormat = findViewById(R.id.spinnerTargetFormat);
        btnSelectFile = findViewById(R.id.btnSelectFile);
        btnConvert = findViewById(R.id.btnConvert);
        progressBar = findViewById(R.id.progressBar);
        txtStatus = findViewById(R.id.txtStatus);
        resultCard = findViewById(R.id.resultCard);
        txtResultDetail = findViewById(R.id.txtResultDetail);
        btnShare = findViewById(R.id.btnShare);
        btnOpen = findViewById(R.id.btnOpen);

        btnSelectFile.setOnClickListener(v -> openFilePicker());
        btnConvert.setOnClickListener(v -> startConversion());

        btnShare.setOnClickListener(v -> {
            if (lastConvertedFile != null && lastConvertedFile.exists()) {
                shareFile(lastConvertedFile);
            }
        });

        btnOpen.setOnClickListener(v -> {
            if (lastConvertedFile != null && lastConvertedFile.exists()) {
                openFile(lastConvertedFile);
            }
        });

        // Handle incoming send intents
        Intent intent = getIntent();
        if (intent != null && Intent.ACTION_SEND.equals(intent.getAction())) {
            Uri uri = intent.getParcelableExtra(Intent.EXTRA_STREAM);
            if (uri != null) {
                onFileSelected(uri);
            }
        }
    }

    private void openFilePicker() {
        Intent intent = new Intent(Intent.ACTION_OPEN_DOCUMENT);
        intent.addCategory(Intent.CATEGORY_OPENABLE);
        intent.setType("*/*");
        startActivityForResult(intent, REQ_PICK_FILE);
    }

    @Override
    protected void onActivityResult(int requestCode, int resultCode, Intent data) {
        super.onActivityResult(requestCode, resultCode, data);
        if (requestCode == REQ_PICK_FILE && resultCode == RESULT_OK && data != null) {
            Uri uri = data.getData();
            if (uri != null) {
                onFileSelected(uri);
            }
        }
    }

    private void onFileSelected(Uri uri) {
        selectedFileUri = uri;
        selectedFileName = queryFileName(uri);
        txtSelectedFile.setText(selectedFileName);
        resultCard.setVisibility(View.GONE);
        txtStatus.setText("");

        String ext = "";
        int dot = selectedFileName.lastIndexOf('.');
        if (dot > 0 && dot < selectedFileName.length() - 1) {
            ext = selectedFileName.substring(dot + 1);
        }

        List<String> targets = ConverterEngine.getAvailableTargets(ext);
        ArrayAdapter<String> adapter = new ArrayAdapter<>(this, android.R.layout.simple_spinner_dropdown_item, targets);
        spinnerTargetFormat.setAdapter(adapter);
    }

    private String queryFileName(Uri uri) {
        String name = "file";
        Cursor cursor = getContentResolver().query(uri, null, null, null, null);
        if (cursor != null) {
            try {
                if (cursor.moveToFirst()) {
                    int nameIdx = cursor.getColumnIndex(OpenableColumns.DISPLAY_NAME);
                    if (nameIdx >= 0) {
                        name = cursor.getString(nameIdx);
                    }
                }
            } finally {
                cursor.close();
            }
        }
        return name;
    }

    private void startConversion() {
        if (selectedFileUri == null) {
            Toast.makeText(this, "Please choose a file to convert first", Toast.LENGTH_SHORT).show();
            return;
        }

        final String targetFormat = (String) spinnerTargetFormat.getSelectedItem();
        if (targetFormat == null) {
            Toast.makeText(this, "Please select target format", Toast.LENGTH_SHORT).show();
            return;
        }

        btnConvert.setEnabled(false);
        progressBar.setVisibility(View.VISIBLE);
        txtStatus.setText("Converting " + selectedFileName + " to " + targetFormat + "...");
        resultCard.setVisibility(View.GONE);

        new Thread(() -> {
            try {
                long start = System.currentTimeMillis();
                File result = ConverterEngine.convert(this, selectedFileUri, selectedFileName, targetFormat);
                long duration = System.currentTimeMillis() - start;
                lastConvertedFile = result;

                mainHandler.post(() -> {
                    progressBar.setVisibility(View.GONE);
                    btnConvert.setEnabled(true);
                    txtStatus.setText("");
                    resultCard.setVisibility(View.VISIBLE);

                    double kb = result.length() / 1024.0;
                    String sizeStr = kb > 1024 ? String.format(Locale.US, "%.2f MB", kb / 1024.0) : String.format(Locale.US, "%.1f KB", kb);
                    txtResultDetail.setText(result.getName() + " (" + sizeStr + ", " + duration + " ms)");
                    Toast.makeText(this, "Conversion complete!", Toast.LENGTH_SHORT).show();
                });
            } catch (Exception e) {
                final String err = e.getMessage() != null ? e.getMessage() : e.toString();
                mainHandler.post(() -> {
                    progressBar.setVisibility(View.GONE);
                    btnConvert.setEnabled(true);
                    txtStatus.setText("Conversion failed: " + err);
                    Toast.makeText(this, "Error: " + err, Toast.LENGTH_LONG).show();
                });
            }
        }).start();
    }

    private void shareFile(File file) {
        Uri uri = Uri.parse("content://com.novasoftware.novaconvert.fileprovider/" + file.getName());
        Intent intent = new Intent(Intent.ACTION_SEND);
        intent.setType("*/*");
        intent.putExtra(Intent.EXTRA_STREAM, uri);
        intent.addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION);
        startActivity(Intent.createChooser(intent, "Share Converted File"));
    }

    private void openFile(File file) {
        Uri uri = Uri.parse("content://com.novasoftware.novaconvert.fileprovider/" + file.getName());
        Intent intent = new Intent(Intent.ACTION_VIEW);
        intent.setDataAndType(uri, "*/*");
        intent.addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION);
        try {
            startActivity(intent);
        } catch (Exception e) {
            Toast.makeText(this, "No app available to open this file format", Toast.LENGTH_SHORT).show();
        }
    }
}
