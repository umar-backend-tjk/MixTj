using System.ComponentModel.DataAnnotations;

namespace Domain.DTOs.Auth;

public class LoginDto
{
    [Required]
    public required string EmailOrNickName { get; set; }
    
    [Required]
    public required string Password { get; set; }
}