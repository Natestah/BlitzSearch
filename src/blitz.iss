; Script generated by the Inno Setup Script Wizard.
; SEE THE DOCUMENTATION FOR DETAILS ON CREATING INNO SETUP SCRIPT FILES!

#define PublishDir "Blitz\bin\Release\net8.0\win-x64\publish\"
#define MyAppVersion GetStringFileInfo(PublishDir+"\blitz.exe", "FileVersion")

[Setup]
; NOTE: The value of AppId uniquely identifies this application. Do not use the same AppId value in installers for other applications.
; (To generate a new GUID, click Tools | Generate GUID inside the IDE.)
AppId={{CF3CD737-B971-4A7D-B0B7-4A07D5BD119D}
AppName=Blitz
AppVersion={#MyAppVersion}
;AppVersion={#AppVerText}

;AppVerName=Blitz 0.5
AppPublisher=Nathan Silvers
AppPublisherURL=https://www.linkedin.com/in/nathan-silvers-a17308a8/
AppSupportURL=https://www.linkedin.com/in/nathan-silvers-a17308a8/
AppUpdatesURL=https://www.linkedin.com/in/nathan-silvers-a17308a8/
PrivilegesRequired=admin
ArchitecturesInstallIn64BitMode=x64compatible
ArchitecturesAllowed=x64compatible
DefaultDirName={autopf}\Blitz
DisableDirPage=yes
DisableProgramGroupPage=yes
; Uncomment the following line to run in non administrative install mode (install for current user only.)
;PrivilegesRequired=lowest
OutputBaseFilename=SetupBlitz_win-x64_{#MyAppVersion}
Compression=zip
WizardStyle=modern


[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[InstallDelete]
Type: files; Name: "{app}\*"
Type: files; Name: "{app}\Documentation\*"

[Files]
Source: "{#PublishDir}\*.*"; Excludes:"*.pdb"; DestDir: "{app}"; Flags: recursesubdirs 
; NOTE: Don't use "Flags: ignoreversion" on any shared system files

[Icons]
Name: "{autoprograms}\Blitz"; Filename: "{app}\Blitz.exe"
Name: "{autodesktop}\Blitz"; Filename: "{app}\Blitz.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\Blitz.exe"; Description: "{cm:LaunchProgram,Blitz}"; Flags: nowait postinstall

