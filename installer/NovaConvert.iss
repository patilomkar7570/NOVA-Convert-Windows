; Inno Setup Script for NOVA Convert
; Version 1.0.0 — Windows Desktop Edition
; Copyright © 2026 NOVA Software. All rights reserved.

#define MyAppName "NOVA Convert"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "NOVA Software"
#define MyAppURL "https://novasoftware.internal/convert"
#define MyAppExeName "NovaConvert.exe"

[Setup]
; Basic Application Info
AppId={{9F6B8E42-2D19-4C55-A31E-F4D69B4B7E01}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}

; Destination Directories
DefaultDirName={autopf}\NOVA Software\NOVA Convert
DefaultGroupName=NOVA Software\NOVA Convert
DisableProgramGroupPage=yes

; Output Configuration
OutputDir=..\dist
OutputBaseFilename=NOVA-Convert-Setup
SetupIconFile=..\assets\brand\nova-logo.ico
UninstallDisplayIcon={app}\{#MyAppExeName}

; Compression & Architecture
Compression=lzma2/ultra64
SolidCompression=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

; UI and Appearance
WizardStyle=modern
PrivilegesRequired=lowest
CloseApplications=yes
RestartIfNeededByRun=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; Main Application and Runtime Files
Source: "..\publish\win-x64\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\assets\brand\nova-logo.ico"; DestDir: "{app}\assets\brand"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\assets\brand\nova-logo.ico"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon; IconFilename: "{app}\assets\brand\nova-logo.ico"

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Registry]
; Register Application for Windows
Root: HKCU; Subkey: "Software\NOVA Software\NOVA Convert"; ValueType: string; ValueName: "InstallPath"; ValueData: "{app}"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\NOVA Software\NOVA Convert"; ValueType: string; ValueName: "Version"; ValueData: "{#MyAppVersion}"; Flags: uninsdeletekey
