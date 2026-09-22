using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using MySqlConnector;
using SalesManagement.Application.Interfaces;
using SalesManagement.Domain.Exceptions;

namespace SalesManagement.Infrastructure.Services;

public class MySqlBackupService : IBackupService
{
    private readonly string _connectionString;
    private readonly IAuditService _auditService;

    public MySqlBackupService(IConfiguration configuration, IAuditService auditService)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? "Server=localhost;Database=sales_management;User=root;Password=;";
        _auditService = auditService;
    }

    public async Task<string> CreateBackupAsync(string targetDirectory)
    {
        if (!Directory.Exists(targetDirectory))
        {
            Directory.CreateDirectory(targetDirectory);
        }

        var builder = new MySqlConnectionStringBuilder(_connectionString);
        string dbName = builder.Database;
        string host = builder.Server;
        uint port = builder.Port;
        string user = builder.UserID;
        string password = builder.Password;

        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string backupFileName = $"backup_{dbName}_{timestamp}.sql";
        string fullPath = Path.Combine(targetDirectory, backupFileName);

        var psi = new ProcessStartInfo
        {
            FileName = "mysqldump",
            Arguments = $"--host={host} --port={port} --user={user} --password={password} --single-transaction --quick --routines --triggers --hex-blob --default-character-set=utf8mb4 {dbName} --result-file=\"{fullPath}\"",
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        try
        {
            using var process = Process.Start(psi);
            if (process == null) throw new DomainException("تعذر تشغيل أداة mysqldump في النظام.");

            string stderr = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                throw new DomainException($"فشل النسخ الاحتياطي لـ MySQL. الخطأ: {stderr}");
            }

            var fileInfo = new FileInfo(fullPath);
            if (!fileInfo.Exists || fileInfo.Length < 1024)
            {
                throw new DomainException("فشل التحقق من ملف النسخة الاحتياطية (حجم الملف غير صالح).");
            }

            await _auditService.LogAsync(null, "System", "DatabaseBackup", "Database", dbName, null, new { fullPath, size = fileInfo.Length });

            return fullPath;
        }
        catch (Exception ex) when (ex is not DomainException)
        {
            throw new DomainException($"حدث خطأ أثناء إجراء النسخ الاحتياطي: {ex.Message}", ex);
        }
    }

    public async Task<bool> RestoreBackupAsync(string backupFilePath)
    {
        if (!File.Exists(backupFilePath))
            throw new DomainException("ملف النسخة الاحتياطية غير موجود.");

        var builder = new MySqlConnectionStringBuilder(_connectionString);
        string dbName = builder.Database;
        string host = builder.Server;
        uint port = builder.Port;
        string user = builder.UserID;
        string password = builder.Password;

        var psi = new ProcessStartInfo
        {
            FileName = "mysql",
            Arguments = $"--host={host} --port={port} --user={user} --password={password} --default-character-set=utf8mb4 {dbName}",
            RedirectStandardInput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        try
        {
            using var process = Process.Start(psi);
            if (process == null) throw new DomainException("تعذر تشغيل أداة mysql في النظام.");

            using (var fileStream = File.OpenRead(backupFilePath))
            using (var writer = process.StandardInput)
            {
                await fileStream.CopyToAsync(writer.BaseStream);
                await writer.FlushAsync();
            }

            string stderr = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                throw new DomainException($"فشل استعادة النسخة الاحتياطية لـ MySQL. الخطأ: {stderr}");
            }

            await _auditService.LogAsync(null, "System", "DatabaseRestore", "Database", dbName, null, new { backupFilePath });

            return true;
        }
        catch (Exception ex) when (ex is not DomainException)
        {
            throw new DomainException($"حدث خطأ أثناء استعادة النسخة الاحتياطية: {ex.Message}", ex);
        }
    }
}
