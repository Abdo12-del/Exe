using System.Text.Json;
using SalesManagement.Application.Interfaces;
using SalesManagement.Domain.Entities;
using SalesManagement.Infrastructure.Data;

namespace SalesManagement.Infrastructure.Services;

public class AuditService : IAuditService
{
    private readonly AppDbContext _context;

    public AuditService(AppDbContext context)
    {
        _context = context;
    }

    public async Task LogAsync(int? userId, string username, string action, string entityName, string? entityId, object? oldValues, object? newValues)
    {
        try
        {
            var log = new AuditLog
            {
                UserId = userId,
                Username = username,
                Action = action,
                EntityName = entityName,
                EntityId = entityId,
                Timestamp = DateTime.UtcNow,
                ComputerName = Environment.MachineName,
                OldValues = oldValues != null ? JsonSerializer.Serialize(oldValues) : null,
                NewValues = newValues != null ? JsonSerializer.Serialize(newValues) : null
            };

            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync();
        }
        catch
        {
            // Logging failure should not abort main business transactions
        }
    }
}
