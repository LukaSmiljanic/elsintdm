using ElsInt.Application.Auth;
using ElsInt.Application.Interfaces;
using ElsInt.Domain.Entities;
using ElsInt.Domain.Enums;
using ElsInt.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace ElsInt.Application.Tests;

public class LoginCommandTests
{
    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsToken()
    {
        await using var db = CreateDb();
        var hasher = Substitute.For<IPasswordHasher>();
        var jwt = Substitute.For<IJwtTokenService>();
        hasher.Verify("Admin123!", Arg.Any<string>()).Returns(true);
        jwt.CreateToken(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>()).Returns("test-token");

        db.AdminUsers.Add(new AdminUser
        {
            Email = "admin@elsint.rs",
            FullName = "Admin",
            PasswordHash = "hash",
            Role = AdminRole.Admin,
            IsActive = true
        });
        await db.SaveChangesAsync();

        var handler = new LoginCommandHandler(db, hasher, jwt);
        var result = await handler.Handle(new LoginCommand("admin@elsint.rs", "Admin123!"), CancellationToken.None);

        result.Token.Should().Be("test-token");
        result.Email.Should().Be("admin@elsint.rs");
        result.Role.Should().Be("Admin");
    }

    [Fact]
    public async Task Login_WithInvalidPassword_Throws()
    {
        await using var db = CreateDb();
        var hasher = Substitute.For<IPasswordHasher>();
        var jwt = Substitute.For<IJwtTokenService>();
        hasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(false);

        db.AdminUsers.Add(new AdminUser
        {
            Email = "admin@elsint.rs",
            FullName = "Admin",
            PasswordHash = "hash",
            Role = AdminRole.Admin,
            IsActive = true
        });
        await db.SaveChangesAsync();

        var handler = new LoginCommandHandler(db, hasher, jwt);
        var act = () => handler.Handle(new LoginCommand("admin@elsint.rs", "wrong"), CancellationToken.None);
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }
}
