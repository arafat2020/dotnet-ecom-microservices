namespace auth_service.DTOs;

public class RegisterDto
{
    public string Username { get; set; } = null!;
    public string Password { get; set; } = null!;
}

public class LoginDto
{
    public string Username { get; set; } = null!;
    public string Password { get; set; } = null!;
}

public class TokenResponseDto
{
    public string Token { get; set; } = null!;
    public DateTime Expiration { get; set; }
}