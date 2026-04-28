using System.Security.Claims;
using auth_service.DTOs;

namespace auth_service.interfaces;

public interface IAuth
{
    TokenResponseDto GenerateToken(string username, Guid userId, IEnumerable<string> roles);
    ClaimsPrincipal? VerifyToken(string token);
}
