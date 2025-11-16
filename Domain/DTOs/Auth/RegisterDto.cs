using System.ComponentModel.DataAnnotations;

namespace Domain.DTOs.Auth;

public class RegisterDto
{
    [Required]
    [EmailAddress]
    public required string Email { get; set; }
    [Required]
    public required string Nickname { get; set; } = string.Empty;
    [Required]
    public required string Password { get; set; }
    
}