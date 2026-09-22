@echo off
chcp 65001 > nul
echo =====================================================================
echo    تنزيل وإعداد ماي اسكيال المتنقلة (Portable MySQL / MariaDB)
echo =====================================================================
echo.

set "SCRIPT_DIR=%~dp0"
set "TARGET_DIR=%SCRIPT_DIR%..\..\src\SalesManagement.Desktop\mysql"

if not exist "%TARGET_DIR%" mkdir "%TARGET_DIR%"

echo [1/3] جاري تنزيل نسخة MySQL / MariaDB المتنقلة الرسمية خفيفة الحجم...
powershell -Command "[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; $url = 'https://archive.mariadb.org/mariadb-10.11.7/winx64-packages/mariadb-10.11.7-winx64.zip'; $output = '%SCRIPT_DIR%mysql_temp.zip'; if (-not (Test-Path $output)) { Write-Host 'Downloading portable package...'; (New-Object System.Net.WebClient).DownloadFile($url, $output) }"

if not exist "%SCRIPT_DIR%mysql_temp.zip" (
    echo خطأ: فشل تنزيل الملف المضغوط. يرجى التحقق من اتصال الإنترنت.
    pause
    exit /b 1
)

echo [2/3] جاري فك الضغط واستخراج الملفات التنفيذية الأساسية (bin, share)...
powershell -Command "Expand-Archive -Path '%SCRIPT_DIR%mysql_temp.zip' -DestinationPath '%SCRIPT_DIR%temp_extract' -Force"

rem نقل الملفات الأساسية فقط لتقليص الحجم
for /d %%D in ("%SCRIPT_DIR%temp_extract\*") do (
    xcopy "%%D\bin\mysqld.exe" "%TARGET_DIR%\bin\" /Y /I > nul
    xcopy "%%D\bin\mysql.exe" "%TARGET_DIR%\bin\" /Y /I > nul
    xcopy "%%D\bin\mysqldump.exe" "%TARGET_DIR%\bin\" /Y /I > nul
    xcopy "%%D\bin\mysqladmin.exe" "%TARGET_DIR%\bin\" /Y /I > nul
    xcopy "%%D\share\english\*" "%TARGET_DIR%\share\english\" /Y /I > nul
    xcopy "%%D\share\charsets\*" "%TARGET_DIR%\share\charsets\" /Y /I > nul
)

copy "%SCRIPT_DIR%my.ini" "%TARGET_DIR%\my.ini" /Y > nul

echo [3/3] تنظيف الملفات المؤقتة...
del /q "%SCRIPT_DIR%mysql_temp.zip"
rmdir /s /q "%SCRIPT_DIR%temp_extract"

echo.
echo =====================================================================
echo    تم تجهيز ماي اسكيال المتنقلة بنجاح داخل مجلد:
echo    %TARGET_DIR%
echo =====================================================================
pause
