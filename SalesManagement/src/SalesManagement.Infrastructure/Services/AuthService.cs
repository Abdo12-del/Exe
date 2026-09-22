using Microsoft.EntityFrameworkCore;
using SalesManagement.Application.DTOs;
using SalesManagement.Application.Interfaces;
using SalesManagement.Infrastructure.Data;
using SalesManagement.Infrastructure.Security;

namespace SalesManagement.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _context;
    private readonly IAuditService _auditService;

    public AuthService(AppDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public async Task<LoginResponse> LoginAsync(string username, string password)
    {
        var user = await _context.Users
            .Include(u => u.Role)
            .ThenInclude(r => r.Permissions)
            .FirstOrDefaultAsync(u => u.Username == username && u.IsActive);

        if (user == null || !PasswordHasher.VerifyPassword(password, user.PasswordHash))
        {
            await _auditService.LogAsync(null, username, "FailedLogin", "User", null, null, null);
            return new LoginResponse(false, "اسم المستخدم أو كلمة المرور غير صحيحة.");
        }

        user.LastLogin = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(user.Id, user.Username, "Login", "User", user.Id.ToString(), null, null);

        var permissions = user.Role.Permissions.Select(p => p.Code).ToList();
        var userDto = new UserDto(
            user.Id,
            user.Username,
            user.FullName,
            user.Role.Name,
            user.Role.DisplayNameAr,
            permissions
        );

        return new LoginResponse(true, "تم تسجيل الدخول بنجاح", userDto);
    }

    public async Task<bool> ChangePasswordAsync(int userId, string oldPassword, string newPassword)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return false;

        if (!PasswordHasher.VerifyPassword(oldPassword, user.PasswordHash))
            return false;

        user.PasswordHash = PasswordHasher.HashPassword(newPassword);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(user.Id, user.Username, "ChangePassword", "User", user.Id.ToString(), null, null);
        return true;
    }
}
