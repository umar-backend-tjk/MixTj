using System.ComponentModel.DataAnnotations;

namespace Domain.DTOs.User;

public class UpdateUserDto
{
    [Required]
    public required string Id { get; set; }
    public required string NickName { get; set; }
    public string About { get; set; } = string.Empty;
}