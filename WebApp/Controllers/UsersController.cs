using Domain.DTOs.User;
using Domain.Filters;
using Infrastructure.Interfaces;
using Infrastructure.Responses;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController(IUserService userService) : ControllerBase
{
    [HttpGet]
    public async Task<PaginationResponse<List<GetUserDto>>> GetAllUsersAsync([FromQuery] UserFilter filter)
        => await userService.GetAllUsersAsync(filter);
    
    [HttpGet("{id}")]
    public async Task<Response<GetUserDto>> GetUserByIdAsync(string id)
        => await userService.GetUserByIdAsync(id);

    [HttpPut]
    public async Task<Response<string>> UpdateUserAsync(UpdateUserDto dto)
        => await userService.UpdateUserAsync(dto);

    [HttpDelete]
    public async Task<Response<string>> DeleteUserAsync(string id)
        => await userService.DeleteUserAsync(id);
}