@echo off
chcp 65001 >nul
setlocal EnableDelayedExpansion
REM =====================================================================
REM  create_proxy.bat — يصنع نسخة مضغوطة (بروكسي) من فيديو طويل لرفعها
REM  الاستخدام: اسحب ملف الفيديو وأفلته على أيقونة هذا الملف
REM  الناتج:  <اسم_الفيديو>_proxy_360p.mp4  (وصغير بما يكفي للرفع)
REM  إن كان الناتج أكبر من 95 ميجابايت يجزّئه إلى أجزاء 90 ميجابايت بـ 7-Zip
REM =====================================================================

if "%~1"=="" (
  echo.
  echo   لم تمرر أي ملف.
  echo   اسحب ملف الفيديو وأفلته على أيقونة هذا الملف ^(create_proxy.bat^)
  echo.
  pause
  exit /b 1
)

set "SRC=%~1"
set "FF="

where ffmpeg >nul 2>nul
if not errorlevel 1 set "FF=ffmpeg"

if not defined FF (
  if exist "%~dp0ffmpeg.exe" set "FF=%~dp0ffmpeg.exe"
)

if not defined FF (
  echo.
  echo   ffmpeg غير موجود — جارٍ تنزيله مرة واحدة ^(~80 ميجابايت^)...
  echo.
  powershell -NoProfile -Command "$ProgressPreference='SilentlyContinue'; Invoke-WebRequest -Uri 'https://github.com/GyanD/codexffmpeg/releases/download/7.1/ffmpeg-7.1-essentials_build.zip' -OutFile '%~dp0ffmpeg.zip'; Expand-Archive -Force -Path '%~dp0ffmpeg.zip' -DestinationPath '%~dp0'"
  if exist "%~dp0ffmpeg.zip" del "%~dp0ffmpeg.zip"
  for /R "%~dp0" %%F in (ffmpeg.exe) do (
    if not defined FF set "FF=%%F"
  )
)

if not defined FF (
  echo.
  echo   تعذّر الحصول على ffmpeg تلقائياً.
  echo   نزّله يدوياً من: https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip
  echo   ثم فك الضغط وضع ملف ffmpeg.exe بجانب هذا الملف.
  echo.
  pause
  exit /b 1
)

set "OUT=%~dpn1_proxy_360p.mp4"
echo.
echo   المدخل   : %SRC%
echo   المخرج   : %OUT%
echo   ffmpeg   : %FF%
echo.
echo   جارٍ إنشاء النسخة المضغوطة... ^(قد يستغرق بضع دقائق حسب طول الفيديو^)
echo.

"%FF%" -hide_banner -loglevel error -stats -i "%SRC%" ^
  -vf "scale=-2:360" ^
  -c:v libx264 -preset veryfast -crf 32 -pix_fmt yuv420p ^
  -c:a aac -b:a 48k -ac 1 -ar 44100 ^
  -movflags +faststart "%OUT%"

if errorlevel 1 (
  echo.
  echo   فشل التحويل. تأكد من أن الملف فيديو سليم.
  pause
  exit /b 1
)

set "SIZE=0"
for %%A in ("%OUT%") do set "SIZE=%%~zA"
echo.
echo   حجم النسخة المضغوطة: !SIZE! بايت  ^(الحد المسموح للرفع: 99000000 بايت^)
echo.

if !SIZE! GTR 99000000 (
  echo   الحجم أكبر من 95 ميجابايت — نجزّئه إلى أجزاء 90 ميجابايت...
  where 7z >nul 2>nul
  if not errorlevel 1 (
    7z a -v90m -mx0 "%OUT%.7z" "%OUT%"
    echo.
    echo   تم التجزئة: %OUT%.7z.001 , .002 , ...
    echo   ^(ملاحظة: صيغة 7z تُدار تلقائياً عند فك الضغط بـ 7-Zip^)
  ) else (
    echo   7-Zip غير مثبت. نزّله من https://www.7-zip.org ثم أعد تشغيل هذا الملف،
    echo   أو قلّل الجودة بفتح هذا الملف بالمفكرة وتغيير 360 إلى 240 و 32 إلى 36.
  )
)

echo.
echo   ✅ انتهى. ارفع الملف الناتج في المحادثة أو على GitHub.
echo.
pause
