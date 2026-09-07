; ============================================================
;  LabelFlow - Installateur Inno Setup
; ============================================================

#define MyAppName "LabelFlow"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "AikFlow"
#define MyAppExeName "LabelFlow.exe"

; Dossier des fichiers compilés (Release)
#define MyAppSource "G:\Rina\LabelFlow\bin\Release\net8.0-windows"

[Setup]
AppId={{A1B2C3D4-E5F6-7890-ABCD-EF1234567890}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
OutputDir=G:\Rina\LabelFlow\installer
OutputBaseFilename=LabelFlow_Setup
; Active cette ligne seulement si app.ico existe à la racine du projet
; SetupIconFile=G:\Rina\LabelFlow\app.ico
Compression=lzma
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayIcon={app}\{#MyAppExeName}

[Languages]
Name: "french"; MessagesFile: "compiler:Languages\French.isl"

[Tasks]
Name: "desktopicon"; Description: "Créer une icône sur le Bureau"; GroupDescription: "Icônes supplémentaires :"; Flags: unchecked

[Files]
Source: "{#MyAppSource}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\Désinstaller {#MyAppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Lancer LabelFlow"; Flags: nowait postinstall skipifsilent
