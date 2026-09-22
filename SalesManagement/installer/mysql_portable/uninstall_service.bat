@echo off
chcp 65001 > nul
echo =====================================================================
echo    إيقاف وإزالة خدمة قاعدة البيانات (SalesManagement_DB)
echo =====================================================================

net session >nul 2>&1
if %errorLevel% neq 0 (
    echo [خطأ] يتطلب تشغيل هذا الملف كمسؤول (Run as administrator).
    pause
    exit /b 1
)

echo إيقاف الخدمة...
net stop SalesManagement_DB

echo إزالة الخدمة من النظام...
"%~dp0bin\mysqld.exe" --remove SalesManagement_DB

echo تم إزالة الخدمة بنجاح.
