# Android APK Build Script for NOVA Convert
$ErrorActionPreference = "Stop"

$sdk = "C:\Users\dudha\AppData\Local\Android\Sdk"
$buildTools = "$sdk\build-tools\34.0.0"
$androidJar = "$sdk\platforms\android-34\android.jar"
$aapt2 = "$buildTools\aapt2.exe"
$d8 = "$buildTools\d8.bat"
$zipalign = "$buildTools\zipalign.exe"
$apksigner = "$buildTools\apksigner.bat"

$projectRoot = "C:\Users\dudha\OneDrive\Documents\Day Projects\Nova File Converter\android"
$appRoot = "$projectRoot\app"
$srcMain = "$appRoot\src\main"
$buildDir = "$projectRoot\build"

# Clean build directory
if (Test-Path $buildDir) { Remove-Item -Recurse -Force $buildDir }
$compiledRes = "$buildDir\compiled_res"
$genDir = "$buildDir\gen"
$classesDir = "$buildDir\classes"
$outDir = "C:\Users\dudha\OneDrive\Documents\Day Projects\Nova File Converter\dist"

New-Item -ItemType Directory -Force -Path $compiledRes | Out-Null
New-Item -ItemType Directory -Force -Path $genDir | Out-Null
New-Item -ItemType Directory -Force -Path $classesDir | Out-Null
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

Write-Host "1. Compiling Android Resources with AAPT2..."
& $aapt2 compile --dir "$srcMain\res" -o "$compiledRes\resources.zip"

Write-Host "2. Linking Resources and Generating R.java..."
$unalignedApk = "$buildDir\app-unaligned.apk"
& $aapt2 link -I $androidJar `
    --manifest "$srcMain\AndroidManifest.xml" `
    --java $genDir `
    -o $unalignedApk `
    --auto-add-overlay `
    "$compiledRes\resources.zip"

Write-Host "3. Compiling Java Source Files with javac..."
$javaFiles = Get-ChildItem -Recurse "$srcMain\java\*.java", "$genDir\*.java" | Select-Object -ExpandProperty FullName
& javac -source 11 -target 11 -cp $androidJar -d $classesDir $javaFiles

Write-Host "4. Generating classes.dex with D8..."
$classFiles = (Get-ChildItem -Recurse "$classesDir\*.class").FullName
$quotedClassFiles = ($classFiles | ForEach-Object { "`"$_`"" }) -join " "
& cmd.exe /c "$d8 --lib ""$androidJar"" --output ""$buildDir"" $quotedClassFiles"

if (-not (Test-Path "$buildDir\classes.dex")) {
    throw "classes.dex was not generated!"
}

Write-Host "5. Packaging classes.dex into APK..."
python -c "
import zipfile
apk_path = r'$unalignedApk'
dex_path = r'$buildDir\classes.dex'
with zipfile.ZipFile(apk_path, 'a', zipfile.ZIP_DEFLATED) as z:
    z.write(dex_path, 'classes.dex')
print('Packaged classes.dex successfully')
"

Write-Host "6. Aligning APK with zipalign..."
$alignedApk = "$buildDir\app-aligned.apk"
& $zipalign -f -v -p 4 $unalignedApk $alignedApk | Out-Null

Write-Host "7. Signing APK with apksigner..."
$keystore = "$projectRoot\nova-convert-release.jks"
if (-not (Test-Path $keystore)) {
    Write-Host "Generating release keystore..."
    & keytool -genkey -v -keystore $keystore -alias novaconvert -keyalg RSA -keysize 2048 -validity 10000 -storepass novapass123 -keypass novapass123 -dname "CN=NOVA Software, OU=NOVA Convert, O=NOVA Software, L=San Francisco, ST=CA, C=US"
}

$finalApk = "$outDir\NOVA-Convert.apk"
if (Test-Path $finalApk) { Remove-Item -Force $finalApk }

& cmd.exe /c "$apksigner sign --ks ""$keystore"" --ks-pass pass:novapass123 --ks-key-alias novaconvert --key-pass pass:novapass123 --out ""$finalApk"" ""$alignedApk"""

Write-Host "8. Verifying APK signature..."
& cmd.exe /c "$apksigner verify ""$finalApk"""

Write-Host "`nBUILD SUCCESSFUL! Output: $finalApk"
Get-Item $finalApk | Select-Object Name, Length, LastWriteTime
