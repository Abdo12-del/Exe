@echo off
chcp 65001 > nul
echo =====================================================================
echo    تثبيت خدمة ماي اسكيال المتنقلة (SalesManagement_DB Service)
echo =====================================================================

set "BASE_DIR=%~dp0"
set "BIN_DIR=%BASE_DIR%bin"
set "DATA_DIR=%BASE_DIR%data"
set "INI_FILE=%BASE_DIR%my.ini"

rem فحص صلاحيات الأدمن
net session >nul 2>&1
if %errorLevel% neq 0 (
    echo [خطأ] يتطلب تشغيل هذا الملف كمسؤول (Run as administrator).
    pause
    exit /b 1
)

rem تهيئة مجلد البيانات إذا لم يكن موجوداً
if not exist "%DATA_DIR%" (
    echo [1/3] جاري تهيئة قاعدة البيانات لأول مرة (Initialize data directory)...
    "%BIN_DIR%\mysqld.exe" --defaults-file="%INI_FILE%" --initialize-insecure --basedir="%BASE_DIR%" --datadir="%DATA_DIR%"
)

echo [2/3] تسجيل الخدمة في ويندوز...
"%BIN_DIR%\mysqld.exe" --install SalesManagement_DB --defaults-file="%INI_FILE%"

echo [3/3] تشغيل الخدمة...
net start SalesManagement_DB

echo.
echo تم تثبيت وتشغيل خدمة قاعدة البيانات بنجاح!
