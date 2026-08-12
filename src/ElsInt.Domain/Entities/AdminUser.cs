namespace ElsInt.Domain.Entities;

using ElsInt.Domain.Common;
using ElsInt.Domain.Enums;

public class AdminUser : BaseEntity
{
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public AdminRole Role { get; set; } = AdminRole.Admin;
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginAtUtc { get; set; }
}
