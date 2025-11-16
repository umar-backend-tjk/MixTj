namespace Domain.DTOs.User;

public class GetUserDto
{
    public required string Id { get; set; }
    public required string NickName { get; set; }
    public required string Email { get; set; }
    public string About { get; set; } = string.Empty;
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public List<string> Roles { get; set; } = [];
}