using CLM.Core.Entities;
using CLM.Core.DTOs.Auth;

namespace CLM.Core.Interfaces;

public interface ITokenService
{
    string GenerateToken(User user);
    string GetRoleFromToken(string token);
}
