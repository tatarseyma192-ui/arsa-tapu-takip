using ArsaTapu.DataAccess.Identity;
using ArsaTapu.Dto.Auth;

namespace ArsaTapu.Api.Services;

public interface IJwtTokenService
{
    Task<LoginResponseDto> TokenUretAsync(ApplicationUser kullanici, IEnumerable<string> roller);
    Task<LoginResponseDto?> RefreshTokenIleYenileAsync(string accessToken, string refreshToken);
}
