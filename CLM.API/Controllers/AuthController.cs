using System.Security.Claims;
using AutoMapper;
using BCrypt.Net;
using CLM.Core.DTOs.Auth;
using CLM.Core.Entities;
using CLM.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CLM.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IUserRepository _userRepo;
    private readonly ITokenService _tokenService;
    private readonly IAuditService _audit;

    public AuthController(IUserRepository userRepo, ITokenService tokenService, IAuditService audit)
    {
        _userRepo = userRepo;
        _tokenService = tokenService;
        _audit = audit;
    }

    /// <summary>Register a new user</summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var existing = await _userRepo.GetByEmailAsync(request.Email);
        if (existing != null)
            return Conflict(new { message = "Email already registered" });

        var role = await _userRepo.GetRoleByNameAsync(request.Role);
        if (role == null)
            return BadRequest(new { message = $"Role '{request.Role}' does not exist" });

        var user = new User
        {
            Name = request.Name,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            RoleId = role.Id,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _userRepo.CreateAsync(user);
        // Reload with role
        var full = await _userRepo.GetByIdAsync(created.Id);

        await _audit.LogAsync(created.Id, "Register", "User", created.Id, $"User '{request.Email}' registered");

        var token = _tokenService.GenerateToken(full!);
        return Ok(new AuthResponse
        {
            Token = token,
            Name = full!.Name,
            Email = full.Email,
            Role = full.Role.Name,
            UserId = full.Id,
            Expiry = DateTime.UtcNow.AddHours(8)
        });
    }

    /// <summary>Login and get JWT token</summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _userRepo.GetByEmailAsync(request.Email);
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Unauthorized(new { message = "Invalid email or password" });

        await _audit.LogAsync(user.Id, "Login", "User", user.Id, $"User '{user.Email}' logged in");

        var token = _tokenService.GenerateToken(user);
        return Ok(new AuthResponse
        {
            Token = token,
            Name = user.Name,
            Email = user.Email,
            Role = user.Role.Name,
            UserId = user.Id,
            Expiry = DateTime.UtcNow.AddHours(8)
        });
    }
}
