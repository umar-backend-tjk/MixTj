using Domain.Enums;

namespace Domain.Filters;

public class UserFilter : BaseFilter
{
    public string? NickName { get; set; }
    public string? Email { get; set; }
    public Role? Role { get; set; }
}