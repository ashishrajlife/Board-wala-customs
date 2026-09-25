using ValousWorld.Web.Models.Entities;

namespace ValousWorld.Web.Services;

public interface ITokenService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
}