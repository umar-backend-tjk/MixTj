using Domain.DTOs.User;
using Domain.Filters;
using Infrastructure.Responses;

namespace Infrastructure.Interfaces;

public interface IUserService
{
    Task<PaginationResponse<List<GetUserDto>>> GetAllUsersAsync(UserFilter filter);
    Task<Response<GetUserDto>> GetUserByIdAsync(string id);
    Task<Response<string>> UpdateUserAsync(UpdateUserDto dto);
    Task<Response<string>> DeleteUserAsync(string id);
}