@echo off
chcp 65001 >nul
title نظام تسيير المبيعات ونقاط البيع - Taseer POS
echo ==========================================================
echo        نظام تسيير المبيعات ونقاط البيع الحديث (POS)
echo ==========================================================
echo.

:: تحقق من وجود Node.js
where node >nul 2>nul
if %errorlevel% neq 0 (
    echo [تنبيه] لم يتم العثور على Node.js في النظام.
    echo لتشغيل النسخة الخفيفة المباشرة عبر الطرفية، جاري تشغيل dist\taseer.exe...
    start "" dist\taseer.exe
    exit /b
)

echo [1/2] جاري تجهيز النظام وتشغيل الخادم المحلي...
cd /d "%~dp0\web"
if not exist "node_modules" (
    echo جاري تثبيت الحزم لأول مرة...
    call npm install --silent
)

start "" node server.js
timeout /t 2 /nobreak >nul

echo [2/2] جاري فتح واجهة نقطة البيع في المتصفح...
start http://localhost:3000

echo.
echo ==========================================================
echo  النظام يعمل الآن على: http://localhost:3000
echo  يمكن لأي جهاز على نفس شبكة Wi-Fi الوصول إليه عبر IP الجهاز.
echo  لإيقاف النظام، أغلق هذه النافذة.
echo ==========================================================
pause >nul
