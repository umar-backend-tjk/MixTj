using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Domain.Entities;

[Index(nameof(Nickname), IsUnique = true)]
public class AppUser : IdentityUser
{
    [MaxLength(500)]
    public string About { get; set; } = string.Empty;
    [MaxLength(30)]
    public required string Nickname { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}