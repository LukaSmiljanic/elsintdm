namespace ElsInt.Application.Interfaces;

public interface IJwtTokenService
{
    string CreateToken(Guid userId, string email, string role, string fullName);
}
