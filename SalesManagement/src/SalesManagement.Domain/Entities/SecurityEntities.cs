namespace SalesManagement.Domain.Entities;

public class Role
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DisplayNameAr { get; set; } = string.Empty;
    public string? DisplayNameFr { get; set; }
    public string? Description { get; set; }
    public bool IsSystem { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<Permission> Permissions { get; set; } = new List<Permission>();
}

public class Permission
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string? NameFr { get; set; }
    public string? Description { get; set; }

    public ICollection<Role> Roles { get; set; } = new List<Role>();
}

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public int RoleId { get; set; }
    public Role Role { get; set; } = null!;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLogin { get; set; }

    public bool HasPermission(string permissionCode)
    {
        if (Role == null || Role.Permissions == null) return false;
        if (Role.Name == "Administrator") return true;
        return Role.Permissions.Any(p => p.Code == permissionCode);
    }
}
