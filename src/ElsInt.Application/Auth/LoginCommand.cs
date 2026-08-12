using ElsInt.Application.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ElsInt.Application.Auth;

public record LoginCommand(string Email, string Password) : IRequest<LoginResult>;

public record LoginResult(string Token, string Email, string FullName, string Role);

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6);
    }
}

public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResult>
{
    private readonly IAppDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtTokenService _jwt;

    public LoginCommandHandler(IAppDbContext db, IPasswordHasher hasher, IJwtTokenService jwt)
    {
        _db = db;
        _hasher = hasher;
        _jwt = jwt;
    }

    public async Task<LoginResult> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _db.AdminUsers
            .FirstOrDefaultAsync(u => u.Email == request.Email && u.IsActive, cancellationToken)
            ?? throw new UnauthorizedAccessException("Invalid credentials.");

        if (!_hasher.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid credentials.");

        user.LastLoginAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        var token = _jwt.CreateToken(user.Id, user.Email, user.Role.ToString(), user.FullName);
        return new LoginResult(token, user.Email, user.FullName, user.Role.ToString());
    }
}
