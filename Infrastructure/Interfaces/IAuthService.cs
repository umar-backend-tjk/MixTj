using Domain.DTOs.Auth;
using Domain.Entities;
using Infrastructure.Responses;

namespace Infrastructure.Interfaces;

public interface IAuthService
{
    Task<Response<string>> RegisterUserAsync(RegisterDto registerModel);
    Task<Response<string>> LoginAsync(LoginDto loginModel);
}