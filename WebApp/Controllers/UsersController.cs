using Domain.DTOs.User;
using Domain.Filters;
using Infrastructure.Interfaces;
using Infrastructure.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController(IUserService userService) : ControllerBase
{
    [Authorize(Roles = "Admin, Moderator")]
    [HttpGet]
    public async Task<PaginationResponse<List<GetUserDto>>> GetAllUsersAsync([FromQuery] UserFilter filter)
        => await userService.GetAllUsersAsync(filter);
    
    [Authorize(Roles = "Admin, Moderator, User")]
    [HttpGet("{id}")]
    public async Task<Response<GetUserDto>> GetUserByIdAsync(string id)
        => await userService.GetUserByIdAsync(id);

    [Authorize(Roles = "Admin, User")]
    [HttpPut]
    public async Task<Response<string>> UpdateUserAsync(UpdateUserDto dto)
        => await userService.UpdateUserAsync(dto);

    [Authorize(Roles = "Admin")]
    [HttpDelete]
    public async Task<Response<string>> DeleteUserAsync(string id)
        => await userService.DeleteUserAsync(id);
}