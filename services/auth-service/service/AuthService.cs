using auth_service.Interfaces;
using auth_service.DTOs;

namespace auth_service.Services;

public class AuthService: IAuthservice {
    public Task<TokenResponseDto> LoginAsync(LoginDto loginDto)
    {
        throw new NotImplementedException();
    }

    public Task<TokenResponseDto> RegisterAsync(RegisterDto registerDto)
    {
        throw new NotImplementedException();
    }
}