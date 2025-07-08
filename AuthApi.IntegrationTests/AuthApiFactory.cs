using AuthApi.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;
using Testcontainers.MsSql;

namespace AuthApi.IntegrationTests
{
    public class AuthApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
    {
        private readonly MsSqlContainer _msSqlContainer = new MsSqlBuilder()
                .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
                .WithName($"authapi-test-{Guid.NewGuid()}")
                .WithPassword("Your_strong(!)Password")
                .Build();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureTestServices(services =>
            {
                // Remove ALL traces of context and options!
                // Remove EVERYTHING that even references DataContext or IDataContext
                var toRemove = services.Where(s =>
                    (s.ServiceType.FullName?.Contains("DataContext") ?? false) ||
                    (s.ServiceType.FullName?.Contains("IDataContext") ?? false) ||
                    (s.ImplementationType?.FullName?.Contains("DataContext") ?? false) ||
                    (s.ImplementationType?.FullName?.Contains("IDataContext") ?? false) ||
                    (s.ServiceType.FullName?.Contains("DbContextOptions") ?? false)).ToList();

                foreach (var s in toRemove)
                    services.Remove(s);

                services.AddDbContext<IDataContext, DataContext>(options =>
                {
                    options.UseSqlServer(_msSqlContainer.GetConnectionString());
                });

                //services.AddAuthentication(TestAuthHandler.AuthenticationScheme)
                //    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                //        TestAuthHandler.AuthenticationScheme, _ => { });
            });

            //builder.ConfigureAppConfiguration((ctx, cfg) =>
            //{
            //    // clear other sources and add in-memory overrides
            //    cfg.Sources.Clear();

            //    cfg.AddInMemoryCollection(new Dictionary<string, string>
            //    {
            //        ["Jwt:Key"] = "baf975688386a7a4022421c32bd7d0fe01546b05e9a97b080dcecb6f8b0202c6",
            //        ["Jwt:Issuer"] = "TestIssuer",
            //        ["Jwt:Audience"] = "TestAudience",
            //        ["Jwt:AccessTokenExpirationMinutes"] = "15",
            //        ["Jwt:RefreshTokenExpirationDays"] = "7"
            //    });
            //});
        }

        public async Task InitializeAsync()
        {
            await _msSqlContainer.StartAsync();

            var options = new DbContextOptionsBuilder<DataContext>()
                .UseSqlServer(_msSqlContainer.GetConnectionString())
                .Options;

            using var context = new DataContext(options);
            // Use either:
            await context.Database.EnsureCreatedAsync();
            //await context.Database.MigrateAsync();
        }

        Task IAsyncLifetime.DisposeAsync()
        {
            return _msSqlContainer.StopAsync();
        }
    }

    // Extension method to simplify setting the test user for HttpClient
    public static class HttpClientExtensions
    {
        public static void SetTestUser(this HttpClient client, ClaimsPrincipal user)
        {
            client.DefaultRequestHeaders.Remove("X-Test-User"); // Clear previous
            client.DefaultRequestHeaders.Add("X-Test-User", "true"); // Signal the TestAuthHandler
            client.DefaultRequestHeaders.TryAddWithoutValidation("X-Test-User-Email", user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value);
            client.DefaultRequestHeaders.TryAddWithoutValidation("X-Test-User-Role", user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value);
        }
    }
}
