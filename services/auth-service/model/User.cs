using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace auth_service.Model;

[Index(nameof(Username), IsUnique =true )]
public class User
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    [Required]
    public string Username { get; set; } = null!;
    [Required]
    public string PasswordHash { get; set; } = null!;
    [Required]
    public List<UserRole> UserRoles { get; set; } = new List<UserRole>();
}