using AuthApi.Models;
using AuthApi.Repositories.Implementations;
using Microsoft.EntityFrameworkCore;

namespace AuthApi.DataTests;

public class RefreshTokenRepositoryInMemoryTests
{
    [Fact]
    public async Task AddAsync_Should_Add_RefreshToken_With_User()
    {
        // Arrange
        await using var context = Helper.GetInMemoryContext();
        var user = Helper.CreateTestUser("carol", "x");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var repo = new RefreshTokenRepository(context);
        var token = new RefreshToken
        {
            Token = "secret",
            Expires = DateTime.UtcNow.AddDays(1),
            User = user
        };

        // Act
        await repo.AddAsync(token);
        await repo.SaveChangesAsync();

        // Assert
        var saved = await context.RefreshTokens.Include(r => r.User)
            .SingleOrDefaultAsync(r => r.Token == token.Token);
        Assert.NotNull(saved);
        Assert.Equal(user.Username, saved.User.Username);
    }

    [Fact]
    public async Task GetByTokenAsync_Should_Return_Token_With_User()
    {
        // Arrange
        await using var context = Helper.GetInMemoryContext();
        var user = Helper.CreateTestUser("dave","y");
        context.Users.Add(user);
        var token = new RefreshToken { Token = "tkn", Expires = DateTime.UtcNow.AddHours(1), User = user };
        context.RefreshTokens.Add(token);
        await context.SaveChangesAsync();

        var repo = new RefreshTokenRepository(context);

        // Act
        var result = await repo.GetByTokenAsync(token.Token);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(user.Username, result.User.Username);
    }
}