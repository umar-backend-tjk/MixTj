using System.Net;
using System.Security.Claims;
using AutoMapper;
using Domain.DTOs.User;
using Domain.Entities;
using Domain.Filters;
using Infrastructure.Data;
using Infrastructure.Interfaces;
using Infrastructure.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;
using Serilog;

namespace Infrastructure.Services;

public class UserService(
    DataContext context,
    UserManager<AppUser> userManager,
    IMapper mapper,
    IHttpContextAccessor accessor) : IUserService
{
    public async Task<PaginationResponse<List<GetUserDto>>> GetAllUsersAsync(UserFilter filter)
    {
        var userId = accessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "anonymous";
        Log.Information("User {userId} tries to get {pageSize} users", userId, filter.PageSize);
        var query = context.Users.Where(u => !u.IsDeleted).AsQueryable();

        if (!string.IsNullOrEmpty(filter.NickName))
        {
            query = query.Where(u => u.Nickname.ToLower() == filter.NickName.ToLower());
        }
        
        if (filter.Role.HasValue)
        {
            var usersInRole = await userManager.GetUsersInRoleAsync(filter.Role.Value.ToString());
            var userIds = usersInRole.Select(x => x.Id).ToList();
            query = query.Where(x => userIds.Contains(x.Id));
        }

        if (!string.IsNullOrEmpty(filter.Email))
        {
            query = query.Where(u => u.Email!.ToLower() == filter.Email.ToLower());
        }

        var totalRecord = await query.CountAsync();
        var skip = (filter.PageNumber - 1) * filter.PageSize;
        var result = await query.Skip(skip).Take(filter.PageSize).ToListAsync();
        if (!result.Any())
        {
            return new PaginationResponse<List<GetUserDto>>(HttpStatusCode.NotFound, "Not found users");
        }
        
        var usersMap = mapper.Map<List<GetUserDto>>(result);
        foreach (var userDto in usersMap)
        {
            var identityUser = await userManager.FindByIdAsync(userDto.Id);
            var roles = await userManager.GetRolesAsync(identityUser!);
            userDto.Roles = roles.ToList();
        }

        Log.Information("Got {count} the users successfully", usersMap.Count);
        return new PaginationResponse<List<GetUserDto>>(usersMap, totalRecord, filter.PageNumber, filter.PageSize);
    }

    public async Task<Response<GetUserDto>> GetUserByIdAsync(string id)
    {
        var userId = accessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "anonymous";
        Log.Information("User {userId} tries to get user with id {id}", userId, id);
        var user = await userManager.FindByIdAsync(id);

        if (user == null)
        {
            Log.Warning("Not found the user with id {id}", id);
            return new Response<GetUserDto>(HttpStatusCode.NotFound, $"Not found the user with id {id}");
        }

        var roles = await userManager.GetRolesAsync(user);
        var mappedUser = mapper.Map<GetUserDto>(user);
        mappedUser.Roles = roles.ToList();
        
        Log.Information("Got the user with id {id} successfully", id);
        return new Response<GetUserDto>(mappedUser);
    }

    public async Task<Response<string>> UpdateUserAsync(UpdateUserDto dto)
    {
        var userId = accessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "anonymous";
        Log.Information("User {userId} tries to update the user with id {id}", userId, dto.Id);
        var user = await userManager.FindByIdAsync(dto.Id);

        if (user is null)
        {
            Log.Warning("Not found the user with id {id} to update", dto.Id);
            return new Response<string>(HttpStatusCode.NotFound, $"Not found the user with id {dto.Id} to update");
        }
        
        mapper.Map(dto, user);
        user.UpdatedAt = DateTime.UtcNow;
        var result = await context.SaveChangesAsync();

        if (result == 0)
        {
            Log.Warning("Failed to update the user with id {id}", dto.Id);
            return new Response<string>(HttpStatusCode.BadRequest, $"Failed to update the user with id {dto.Id}");
        }
        
        Log.Information("Updated the user with id {id} successfully", dto.Id);
        return new Response<string>(HttpStatusCode.OK, $"Updated the user with id {dto.Id} successfully");
    }

    public async Task<Response<string>> DeleteUserAsync(string id)
    {
        var userId = accessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "anonymous";
        Log.Information("User {userId} tries to delete the user with id {id}", userId, id);
        var user = await userManager.FindByIdAsync(id);

        if (user is null)
        {
            Log.Warning("Not found the user with id {id} to delete", id);
            return new Response<string>(HttpStatusCode.NotFound, $"Not found the user with id {id} to delete");
        }

        user.IsDeleted = true;
        var result = await context.SaveChangesAsync();

        if (result == 0)
        {
            Log.Warning("Failed to update the user with id {id}", id);
            return new Response<string>(HttpStatusCode.BadRequest, $"Failed to update the user with id {userId}");
        } 
        
        Log.Information("Updated the user with id {id} successfully", id);
        return new Response<string>(HttpStatusCode.OK, $"Updated user successfully");
    }
}