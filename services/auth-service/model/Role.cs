using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace auth_service.Model;

[Index(nameof(Name), IsUnique = true)]
public class Role
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public string Name { get; set; } = null!;
    public List<UserRole> UserRoles { get; set; } = new List<UserRole>();
}