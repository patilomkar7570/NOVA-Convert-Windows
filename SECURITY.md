# Security Policy

## Reporting Security Vulnerabilities

NOVA Software takes software security and user privacy seriously. If you discover a vulnerability or potential security flaw in NOVA Convert, please contact our security team directly:

- **Email**: `security@novasoftware.internal`
- **Response Time**: We acknowledge receipt of reports within 48 business hours.

---

## Security Architecture & Best Practices

NOVA Convert is engineered according to the principle of least privilege and strict defensive programming:

1. **Local Isolation**: All file conversions take place entirely locally on the user's host machine. No external file uploads or remote processing occurs.
2. **Execution Shield**: NOVA Convert will never execute, run, or invoke converted binary files. Converted files are written as passive data streams.
3. **External Process Sandboxing**: Third-party conversion engines (such as FFmpeg) are executed as child processes with strictly sanitised arguments, quoted paths, and `UseShellExecute = false`, preventing argument injection.
4. **Input Validation**: Before any conversion is dispatched, the incoming file header is validated against binary signatures. Malformed headers or corrupt streams are flagged before feeding them to conversion pipelines.
5. **Path Traversal Protection**: Filenames are sanitised and output files are written exclusively to validated user-designated target folders.
6. **Automatic Cleanup**: Intermediate temporary files and extraction directories are purged on completion or cancellation.
7. **Hash Verification**: Future automatic update mechanisms enforce HTTPS and SHA-256 cryptographic checksum verification prior to staging any executable.
