using System.Net;
using System.Security.Claims;
using AutoMapper;
using Domain.DTOs.User;
using Domain.Entities;
using Domain.Filters;
using Infrastructure.Caching;
using Infrastructure.Data;
using Infrastructure.Interfaces;
using Infrastructure.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Infrastructure.Services;

public class UserService(
    DataContext context,
    UserManager<AppUser> userManager,
    IMapper mapper,
    IHttpContextAccessor accessor,
    ICacheService cacheService) : IUserService
{
    private string? GetCurrentUserId() =>
        accessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    private async Task RefreshCacheAsync()
    {
        try
        {
            var allUsers = await context.Users.Where(u => !u.IsDeleted).ToListAsync();
            await cacheService.AddAsync(CacheKeys.Users, allUsers, DateTimeOffset.Now.AddMinutes(5));
            Log.Information("Refreshed cache with key {k}", CacheKeys.Users);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to refresh users cache");
        }
    }

    public async Task<PaginationResponse<List<GetUserDto>>> GetAllUsersAsync(UserFilter filter)
    {
        try
        {
            var userId = GetCurrentUserId() ?? "anonymous";
            Log.Information("User {userId} tries to get {pageSize} users", userId, filter.PageSize);

            var usersInCache = await cacheService.GetAsync<List<AppUser>>(CacheKeys.Users);
            if (usersInCache == null)
            {
                usersInCache = await context.Users.Where(u => !u.IsDeleted).ToListAsync();
                await cacheService.AddAsync(CacheKeys.Users, usersInCache, DateTimeOffset.Now.AddMinutes(5));
            }

            var query = usersInCache.AsQueryable();

            if (!string.IsNullOrEmpty(filter.NickName))
                query = query.Where(u => u.Nickname.Contains(filter.NickName, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrEmpty(filter.Email))
                query = query.Where(u => u.Email != null && u.Email.Contains(filter.Email, StringComparison.OrdinalIgnoreCase));

            if (filter.Role.HasValue)
            {
                var usersInRole = await userManager.GetUsersInRoleAsync(filter.Role.Value.ToString());
                var userIds = usersInRole.Select(u => u.Id).ToHashSet();
                query = query.Where(u => userIds.Contains(u.Id));
            }

            var totalCount = query.Count();
            var skip = (filter.PageNumber - 1) * filter.PageSize;
            var items = query.Skip(skip).Take(filter.PageSize).ToList();

            if (!items.Any())
            {
                Log.Warning("No users found for the given filter");
                return new PaginationResponse<List<GetUserDto>>(HttpStatusCode.NotFound, "Not found users");
            }

            var usersDto = mapper.Map<List<GetUserDto>>(items);

            foreach (var userDto in usersDto)
            {
                var identityUser = await userManager.FindByIdAsync(userDto.Id);
                if (identityUser != null)
                {
                    var roles = await userManager.GetRolesAsync(identityUser);
                    userDto.Roles = roles.ToList();
                }
            }

            Log.Information("Found {count} users", totalCount);
            return new PaginationResponse<List<GetUserDto>>(usersDto, totalCount, filter.PageNumber, filter.PageSize);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to get all users");
            return new PaginationResponse<List<GetUserDto>>(HttpStatusCode.InternalServerError, "Failed to get users");
        }
    }

    public async Task<Response<GetUserDto>> GetUserByIdAsync(string id)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return new Response<GetUserDto>(HttpStatusCode.Unauthorized, "Unauthorized");

            var currentUser = await context.Users.FindAsync(userId);
            var currentUserRoles = await userManager.GetRolesAsync(currentUser!);

            Log.Information("User {userId} tries to get user with id {id}", userId, id);

            var user = await context.Users.FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);
            if (user == null)
            {
                Log.Warning("User with id {id} not found", id);
                return new Response<GetUserDto>(HttpStatusCode.NotFound, $"Not found the user with id {id}");
            }
            
            var isOwner = user.Id == userId;
            var isAdmin = currentUserRoles.Contains("Admin");
            var isModerator = currentUserRoles.Contains("Moderator");

            if (!isOwner && !(isAdmin || isModerator))
                return new Response<GetUserDto>(HttpStatusCode.Forbidden, "Forbidden");

            var roles = await userManager.GetRolesAsync(user);
            var mappedUser = mapper.Map<GetUserDto>(user);
            mappedUser.Roles = roles.ToList();

            Log.Information("Successfully retrieved user with id {id}", id);
            return new Response<GetUserDto>(mappedUser);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to get user by id {id}", id);
            return new Response<GetUserDto>(HttpStatusCode.InternalServerError, "Failed to get user");
        }
    }

    public async Task<Response<string>> UpdateUserAsync(UpdateUserDto dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return new Response<string>(HttpStatusCode.Unauthorized, "Unauthorized");

            var currentUser = await context.Users.FindAsync(userId);
            var currentUserRoles = await userManager.GetRolesAsync(currentUser!);

            Log.Information("User {userId} tries to update the user with id {id}", userId, dto.Id);

            var user = await context.Users.FirstOrDefaultAsync(u => u.Id == dto.Id && !u.IsDeleted);
            if (user == null)
            {
                Log.Warning("User with id {id} not found for update", dto.Id);
                return new Response<string>(HttpStatusCode.NotFound, $"Not found the user with id {dto.Id} to update");
            }

            var isOwner = dto.Id == userId;
            var isAdmin = currentUserRoles.Contains("Admin");

            if (!isOwner && !isAdmin)
                return new Response<string>(HttpStatusCode.Forbidden, "Forbidden");
            
            mapper.Map(dto, user);
            user.UpdatedAt = DateTime.UtcNow;

            var result = await context.SaveChangesAsync();
            if (result == 0)
            {
                Log.Warning("Failed to update the user with id {id}", dto.Id);
                return new Response<string>(HttpStatusCode.BadRequest, $"Failed to update the user with id {dto.Id}");
            }

            await RefreshCacheAsync();

            Log.Information("Updated user with id {id} successfully", dto.Id);
            return new Response<string>(HttpStatusCode.OK, $"Updated user with id {dto.Id} successfully");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to update user with id {id}", dto.Id);
            return new Response<string>(HttpStatusCode.InternalServerError, "Failed to update user");
        }
    }

    public async Task<Response<string>> DeleteUserAsync(string id)
    {
        try
        {
            var userId = GetCurrentUserId() ?? "anonymous";
            Log.Information("User {userId} tries to delete the user with id {id}", userId, id);

            var user = await context.Users.FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);
            if (user == null)
            {
                Log.Warning("User with id {id} not found for deletion", id);
                return new Response<string>(HttpStatusCode.NotFound, $"Not found the user with id {id} to delete");
            }

            user.IsDeleted = true;
            var result = await context.SaveChangesAsync();
            if (result == 0)
            {
                Log.Warning("Failed to delete the user with id {id}", id);
                return new Response<string>(HttpStatusCode.BadRequest, $"Failed to delete the user with id {id}");
            }

            await RefreshCacheAsync();

            Log.Information("Deleted user with id {id} successfully", id);
            return new Response<string>(HttpStatusCode.OK, "Deleted user successfully");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to delete user with id {id}", id);
            return new Response<string>(HttpStatusCode.InternalServerError, "Failed to delete user");
        }
    }
}
