; ============================================================
;  LabelFlow - Script d'installation Inno Setup
;  Source : G:\Rina\LabelFlow\bin\Release\net8.0-windows
; ============================================================

#define MyAppName "LabelFlow"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "AikFlow"
#define MyAppURL "https://github.com/Fiainana/LabelFlow"
#define MyAppExeName "LabelFlow.exe"
#define MyAppSource "G:\Rina\LabelFlow\bin\Release\net8.0-windows"

[Setup]
AppId={{8F3A2B1C-9D4E-4F6A-B7C8-1E2D3A4B5C6D}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
OutputDir=G:\Rina\LabelFlow\installer
OutputBaseFilename=LabelFlow_Setup
; Décommente la ligne suivante si app.ico est présent à la racine du projet
; SetupIconFile=G:\Rina\LabelFlow\app.ico
Compression=lzma
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayIcon={app}\{#MyAppExeName}
VersionInfoVersion=1.0.0
VersionInfoCompany={#MyAppPublisher}
VersionInfoProductName={#MyAppName}

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
Filename: "{app}\{#MyAppExeName}"; Description: "Lancer {#MyAppName}"; Flags: nowait postinstall skipifsilent

[Messages]
WelcomeLabel1=Bienvenue dans l'assistant d'installation de [name]
WelcomeLabel2=Cet assistant va installer [name/ver] sur votre ordinateur.%n%nIl est recommandé de fermer toutes les autres applications avant de continuer.
