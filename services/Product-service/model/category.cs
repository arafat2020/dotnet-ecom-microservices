using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace product_service.Model;
 
 [Index(nameof(Name), IsUnique = true)]
public class CategoryModel
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public string Name { get; set; } = null!;

    // Self reference
    public Guid? ParentId { get; set; }
    public CategoryModel? Parent { get; set; }

    public ICollection<CategoryModel> Children { get; set; } = new List<CategoryModel>();
}