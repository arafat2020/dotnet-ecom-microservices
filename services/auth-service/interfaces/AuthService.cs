using auth_service.DTOs;

namespace auth_service.Interfaces;

public  interface IAuthservice
{
    Task<TokenResponseDto> RegisterAsync(RegisterDto registerDto);
    Task<TokenResponseDto> LoginAsync(LoginDto loginDto);
}