using System.ComponentModel.DataAnnotations;

namespace gateway.Models;

/// <summary>Request body for the Register endpoint.</summary>
public class RegisterRequestDto
{
    /// <summary>The desired username.</summary>
    [Required]
    public string Username { get; set; } = null!;

    /// <summary>The user's email address.</summary>
    [Required]
    [EmailAddress]
    public string Email { get; set; } = null!;

    /// <summary>The user's password.</summary>
    [Required]
    public string Password { get; set; } = null!;
}

/// <summary>Request body for the Login endpoint.</summary>
public class LoginRequestDto
{
    /// <summary>The user's email address.</summary>
    [Required]
    [EmailAddress]
    public string Email { get; set; } = null!;

    /// <summary>The user's password.</summary>
    [Required]
    public string Password { get; set; } = null!;
}

/// <summary>Response containing an issued JWT token.</summary>
public class AuthTokenDto
{
    /// <summary>The JWT bearer token.</summary>
    public string Token { get; set; } = null!;

    /// <summary>ISO 8601 UTC expiration timestamp.</summary>
    public string Expiration { get; set; } = null!;
}
