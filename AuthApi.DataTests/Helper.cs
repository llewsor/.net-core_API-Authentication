using AuthApi.Data;
using AuthApi.Helpers;
using AuthApi.Models;
using Microsoft.EntityFrameworkCore;

namespace AuthApi.DataTests;

public static class Helper
{
    public static DataContext GetInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<DataContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
            .Options;
        return new DataContext(options);
    }
    
    public static User CreateTestUser(string username, string password)
    {
        PasswordHelper.CreatePasswordHash(
            password,
            out var hash,
            out var salt);

        return new User
        {
            Username = username,
            PasswordHash = hash,
            PasswordSalt = salt,
            Role = "User",
            IsBlocked = false
        };
    }
}