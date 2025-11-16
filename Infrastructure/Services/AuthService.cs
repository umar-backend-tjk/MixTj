using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;
using AutoMapper;
using Domain.DTOs.Auth;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Data;
using Infrastructure.Interfaces;
using Infrastructure.Responses;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Serilog;

namespace Infrastructure.Services;


public class AuthService(
    UserManager<AppUser> userManager,
    DataContext context,
    IMapper mapper,
    IConfiguration configuration) : IAuthService
{
    public async Task<Response<string>> RegisterUserAsync(RegisterDto registerDto)
    {
        Log.Information("Trying to register user with email {email}", registerDto.Email);
        var existingUser = await userManager.FindByEmailAsync(registerDto.Email);

        if (existingUser != null)
        {
            Log.Warning("User with email {email} already exists.", registerDto.Email);
            return new Response<string>(HttpStatusCode.BadRequest,$"User with email {registerDto.Email} already exists.");
        }
        
        var user = mapper.Map<AppUser>(registerDto);
        user.UserName = registerDto.Nickname;
        
        var createResult = await userManager.CreateAsync(user, registerDto.Password);
        if (!createResult.Succeeded)
        {
            Log.Warning("Failed to register user: {error}", createResult.Errors.First().Description);
            return new Response<string>(HttpStatusCode.BadRequest, createResult.Errors.First().Description);
        }
        
        var roleResult = await userManager.AddToRoleAsync(user, nameof(Role.User));
        if (!roleResult.Succeeded)
        {
            Log.Warning("Failed to add role to user {email}: {error}", registerDto.Email, roleResult.Errors.First().Description);
        } 
        else
        {
            Log.Information("Added role to user with email {email}", registerDto.Email);
        }

        Log.Information("User {email} registered successfully", registerDto.Email);
        return new Response<string>(HttpStatusCode.OK, "User registered successfully");
    }

    public async Task<Response<string>> LoginAsync(LoginDto loginDto)
    {
        Log.Information("Trying to login user {emailOrUsername}", loginDto.EmailOrNickName);
        var user = await context.Users.FirstOrDefaultAsync(u =>
            u.Email == loginDto.EmailOrNickName || u.Nickname == loginDto.EmailOrNickName);

        if (user is null)
        {
            Log.Warning("Not found the user {email}", loginDto.EmailOrNickName);
            return new Response<string>(HttpStatusCode.BadRequest, "Invalid email or nickname or password");
        }

        if (!await userManager.CheckPasswordAsync(user, loginDto.Password))
        {
            Log.Warning("Invalid password entered");
            return new Response<string>(HttpStatusCode.BadRequest,"Invalid email or nickname or password");
        }
        
        var jwtToken = await GenerateJwtToken(user);
        Log.Information("User {email} logged in", loginDto.EmailOrNickName);
        return new Response<string>(jwtToken);
    }

    private async Task<string> GenerateJwtToken(AppUser user)
    {
        var key = Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!);
        var security = new SymmetricSecurityKey(key);
        var credentials = new SigningCredentials(security, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>()
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email!),
            new Claim("nick-name", user.Nickname),
            new Claim("isDeleted", user.IsDeleted.ToString())
        };

        var userRoles = await userManager.GetRolesAsync(user);
        claims.AddRange(userRoles.Select(role => new Claim("role", role)));

        var tokenDescription = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"],
            audience: configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(int.Parse(configuration["Jwt:ExpiryMinutes"]!)),
            signingCredentials: credentials
            );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(tokenDescription);
        return tokenString;
    }
}