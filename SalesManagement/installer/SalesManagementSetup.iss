; =====================================================================
; مثبت برنامج: تاجر برو - Taajer PRO
; نظام إدارة المبيعات ونقاط البيع والمخزون المحلي (مع MySQL متنقلة)
; =====================================================================

#define MyAppName "تاجر برو"
#define MyAppEnglishName "Taajer PRO"
#define MyAppPublisher "DZ Software Solutions"
#define MyAppVersion "1.0.0"
#define MyAppExeName "SalesManagement.Desktop.exe"
#define MyAppURL "https://www.taajer-pro.dz"

[Setup]
AppId={{9F82A4C1-394E-4B19-8669-760B8E2A76B8}
AppName={#MyAppName} ({#MyAppEnglishName})
AppVerName={#MyAppName} الإصدار {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\TaajerPRO
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
LicenseFile=assets\license_ar.txt
OutputDir=..\dist
OutputBaseFilename=TaajerPRO_Setup_v1.0.0
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
WizardResizable=no
PrivilegesRequired=admin
ArchitecturesInstallIn64BitMode=x64

; Visual Branding & UI Design
SetupIconFile=assets\app_icon.ico
WizardImageFile=assets\wizard_large.bmp
WizardSmallImageFile=assets\wizard_small.bmp
DisableWelcomePage=no

; Visual Color Scheme
WizardImageStretch=yes

[Languages]
Name: "arabic"; MessagesFile: "compiler:Languages\Arabic.isl"
Name: "french"; MessagesFile: "compiler:Languages\French.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Messages]
arabic.WelcomeLabel1=أهلاً بك في معالج تثبيت برنامج تاجر برو (Taajer PRO)
arabic.WelcomeLabel2=سيقوم هذا المعالج بتثبيت برنامج تاجر برو لإدارة المبيعات ونقاط البيع والمخزون مع محرك قاعدة بيانات MySQL المتنقلة على جهازك.%n%nيعمل البرنامج محلياً بالكامل (Offline) دون الحاجة للاتصال بالإنترنت.%n%nيُرجى إغلاق أي برامج أخرى مفتوحة قبل المتابعة.
arabic.WizardSelectDir=اختر مجلد تثبيت تاجر برو
arabic.SelectDirDesc=أين ترغب في تثبيت البرنامج؟
arabic.SelectDirBrowseLabel=للمتابعة، انقر فوق "التالي". إذا كنت ترغب في اختيار مجلد آخر، فانقر فوق "استعراض".
arabic.ReadyLabel1=البرنامج جاهز للبدء بعملية التثبيت على جهازك.
arabic.InstallingLabel=جاري تثبيت ملفات تاجر برو ومحرك قاعدة البيانات المتنقلة...
arabic.ClickFinish=انقر فوق "إنهاء" للخروج من معالج التثبيت والبدء باستخدام البرنامج.

[CustomMessages]
arabic.LaunchProgramDesc=تشغيل برنامج تاجر برو الآن (تسجيل الدخول: admin)
arabic.CreateDesktopIcon=إنشاء أيقونة سريعة على سطح المكتب
arabic.InstallDatabaseService=تثبيت وتشغيل خدمة قاعدة البيانات المتنقلة تلقائياً (موصى به)

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "الأيقونات الإضافية:"
Name: "installmysqlservice"; Description: "{cm:InstallDatabaseService}"; GroupDescription: "خدمات قاعدة البيانات:"; Flags: checkedonce

[Files]
; Main Application Files (WPF .NET 8)
Source: "..\src\SalesManagement.Desktop\bin\Release\net8.0-windows\win-x64\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

; Embedded Portable MySQL Engine
Source: "..\src\SalesManagement.Desktop\mysql\*"; DestDir: "{app}\mysql"; Flags: ignoreversion recursesubdirs createallsubdirs; Check: DirExists(ExpandConstant('{src}\..\src\SalesManagement.Desktop\mysql'))

; Database Initialization Scripts
Source: "..\database\01_schema.sql"; DestDir: "{app}\database"; Flags: ignoreversion
Source: "..\database\02_seed_data.sql"; DestDir: "{app}\database"; Flags: ignoreversion

; Branding & Icons
Source: "assets\app_icon.ico"; DestDir: "{app}\assets"; Flags: ignoreversion
Source: "assets\logo.png"; DestDir: "{app}\assets"; Flags: ignoreversion

; Portable MySQL Setup Scripts
Source: "mysql_portable\my.ini"; DestDir: "{app}\mysql"; Flags: ignoreversion
Source: "mysql_portable\install_service.bat"; DestDir: "{app}\mysql"; Flags: ignoreversion
Source: "mysql_portable\uninstall_service.bat"; DestDir: "{app}\mysql"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\assets\app_icon.ico"
Name: "{group}\دليل المستخدم"; Filename: "{app}\README.md"
Name: "{group}\إلغاء تثبيت {#MyAppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\assets\app_icon.ico"; Tasks: desktopicon

[Run]
; Auto-install database service silently if selected
Filename: "{app}\mysql\install_service.bat"; Description: "تهيئة وتشغيل خدمة قاعدة البيانات"; Flags: runhidden; Tasks: installmysqlservice

; Launch the App immediately after install
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgramDesc}"; Flags: nowait postinstall skipifsilent

[UninstallRun]
; Stop and remove the service gracefully during uninstallation
Filename: "{app}\mysql\uninstall_service.bat"; Flags: runhidden

[UninstallDelete]
Type: files; Name: "{app}\logs\*.log"
Type: files; Name: "{app}\mysql\data\*.err"

[Code]
function DirExists(Dir: string): Boolean;
begin
  Result := DirExists(Dir);
end;
