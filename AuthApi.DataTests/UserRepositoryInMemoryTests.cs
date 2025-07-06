using AuthApi.Repositories.Implementations;
using Microsoft.EntityFrameworkCore;

namespace AuthApi.DataTests;

public class UserRepositoryInMemoryTests
{
    [Fact]
    public async Task AddAsync_Should_Add_User()
    {
        // Arrange
        await using var context = Helper.GetInMemoryContext();
        const string username = "alice";
        var repo = new UserRepository(context);
        var user = Helper.CreateTestUser(username,"hash");

        // Act
        await repo.AddAsync(user);
        await repo.SaveChangesAsync();

        // Assert
        var saved = await context.Users.SingleOrDefaultAsync(u => u.Username == username);
        Assert.NotNull(saved);
        Assert.Equal(username, saved.Username);
    }

    [Fact]
    public async Task GetByUsernameAsync_Should_Return_User_If_Exists()
    {
        // Arrange
        await using var context = Helper.GetInMemoryContext();
        const string username = "alice";
        context.Users.Add(Helper.CreateTestUser(username,"hash"));
        await context.SaveChangesAsync();
        var repo = new UserRepository(context);

        // Act
        var result = await repo.GetByUsernameAsync(username);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(username, result.Username);
    }

    [Theory]
    [InlineData("ghost")]
    [InlineData("not-existing")]
    public async Task GetByUsernameAsync_Should_Return_Null_If_NotExists(string username)
    {
        await using var context = Helper.GetInMemoryContext();
        var repo = new UserRepository(context);

        var result = await repo.GetByUsernameAsync(username);

        Assert.Null(result);
    }
}