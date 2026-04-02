using auth_service.Interfaces;
using auth_service.DTOs;
using auth_service.Model;
using auth_service.interfaces;
using auth_service.Utils;
using db.AuthDbContext;
using Microsoft.EntityFrameworkCore;

namespace auth_service.Services;

public class AuthService : IAuthservice
{
    private readonly AuthDbContext _context;
    private readonly IAuth _auth;

    public AuthService(AuthDbContext context, IAuth auth)
    {
        _context = context;
        _auth = auth;
    }

    public async Task<TokenResponseDto> RegisterAsync(RegisterDto registerDto)
    {
        // Check if username already exists
        bool usernameExists = await _context.Users
            .AnyAsync(u => u.Username == registerDto.Username);

        if (usernameExists)
            throw new InvalidOperationException($"Username '{registerDto.Username}' is already taken.");

        // Check if email already exists
        bool emailExists = await _context.Users
            .AnyAsync(u => u.Email == registerDto.Email.ToLower());

        if (emailExists)
            throw new InvalidOperationException($"Email '{registerDto.Email}' is already registered.");

        // Hash the password
        var passwordHash = AuthUtils.HashPassword(registerDto.Password);

        // Create the new user
        var user = new User
        {
            Username = registerDto.Username,
            Email = registerDto.Email.ToLower(),
            PasswordHash = passwordHash
        };

        // Ensure the default "User" role exists, create if not
        var role = await _context.Roles
            .FirstOrDefaultAsync(r => r.Name == "User");

        if (role == null)
        {
            role = new Role { Name = "User" };
            _context.Roles.Add(role);
            await _context.SaveChangesAsync();
        }

        // Assign the role to the user
        user.UserRoles.Add(new UserRole
        {
            UserId = user.Id,
            User = user,
            RoleId = role.Id,
            Role = role
        });

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return _auth.GenerateToken(user.Username, user.Id);
    }

    public async Task<TokenResponseDto> LoginAsync(LoginDto loginDto)
    {
        // Find the user by email
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == loginDto.Email.ToLower());

        if (user == null)
            throw new UnauthorizedAccessException("Invalid email or password.");

        // Verify the password
        bool isValid = AuthUtils.VerifyPassword(loginDto.Password, user.PasswordHash);

        if (!isValid)
            throw new UnauthorizedAccessException("Invalid username or password.");

        return _auth.GenerateToken(user.Username, user.Id);
    }
}