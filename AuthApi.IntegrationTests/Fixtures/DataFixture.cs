using AuthApi.Models;
using Bogus;
using System.Text;

namespace AuthApi.IntegrationTests.Fixtures
{
    public static class DataFixture
    {
        public static List<User> GetUsers(int count, bool useNewSeed = false)
        {
            return GetUserFaker(useNewSeed).Generate(count);
        }

        public static User GetUser(bool useNewSeed = false)
        {
            return GetUsers(1, useNewSeed)[0];
        }

        private static Faker<User> GetUserFaker(bool useNewSeed)
        {
            var faker = new Faker<User>()
                .RuleFor(u => u.Id, f => 0)
                .RuleFor(u => u.Username, f => f.Internet.UserName())
                .RuleFor(u => u.PasswordHash, f => Encoding.UTF8.GetBytes(f.Internet.Password()))
                .RuleFor(u => u.PasswordSalt, f => Encoding.UTF8.GetBytes(f.Internet.Password()))
                .RuleFor(u => u.IsBlocked, f => f.Random.Bool())
                .RuleFor(u => u.Role, f => f.PickRandom(new[] { "User", "Admin" }))
                .RuleFor(u => u.RefreshTokens, _ => new List<RefreshToken>());
            if (useNewSeed)
            {
                faker = faker.UseSeed(new Random().Next());
            }
            return faker;
        }
    }
}
