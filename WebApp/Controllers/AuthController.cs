using Domain.DTOs.Auth;
using Infrastructure.Interfaces;
using Infrastructure.Responses;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("register")]
    public async Task<Response<string>> Register(RegisterDto model)
        => await authService.RegisterUserAsync(model);
    
    [HttpPost("login")]
    public async Task<Response<string>> Login(LoginDto model)
        => await authService.LoginAsync(model);
}