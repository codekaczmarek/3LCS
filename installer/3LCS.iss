#define MyAppName "3LCS"
#define MyAppPublisher "Dawid Kaczmarek"
#define MyAppURL "https://github.com/codekaczmarek/3LCS"
#define MyAppExeName "3LCS.exe"

; Version is injected at build time via /DMyAppVersion=x.y.z
#ifndef MyAppVersion
  #define MyAppVersion "0.0.0"
#endif

[Setup]
AppId={{F3A1B2C4-D5E6-7F8A-9B0C-1D2E3F4A5B6C}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}/issues
AppUpdatesURL={#MyAppURL}/releases
; Per-user installation into %LOCALAPPDATA%\3LCS
DefaultDirName={localappdata}\{#MyAppName}
DisableDirPage=no
; No admin rights required
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=commandline
; Start-menu group
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
; Output
OutputDir=..\installer-output
OutputBaseFilename=3LCS-{#MyAppVersion}-Setup
SetupIconFile=..\3LCS\Assets\favicon-white.ico
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
LicenseFile=..\LICENSE
InfoBeforeFile=intro.txt
DisableWelcomePage=no
; Uninstall info stored per-user
UninstallDisplayIcon={app}\{#MyAppExeName}
UninstallDisplayName={#MyAppName}
; Version info on the installer exe
VersionInfoVersion={#MyAppVersion}
VersionInfoCompany={#MyAppPublisher}
VersionInfoDescription={#MyAppName} Installer
VersionInfoProductName={#MyAppName}
VersionInfoProductVersion={#MyAppVersion}
; Create uninstaller
CreateUninstallRegKey=yes
; Allow running setup without elevation
ChangesAssociations=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Messages]
WelcomeLabel1=Welcome to the {#MyAppName} Setup Wizard
WelcomeLabel2=This wizard will install {#MyAppName} {#MyAppVersion} on your computer.%n%n{#MyAppName} is a free, open-source desktop client for Microsoft Dynamics 365 Lifecycle Services. It lets you manage environments, packages, diagnostics, and more — right from your Windows desktop.%n%nNo administrator rights are required. {#MyAppName} installs for your user account only.%n%nClick Next to continue.

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; Main application files (produced by dotnet publish -o publish/framework-dependent)
Source: "..\publish\framework-dependent\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
; Remove settings/cache left in %APPDATA%\3LCS when uninstalling
Type: filesandordirs; Name: "{userappdata}\{#MyAppName}"
