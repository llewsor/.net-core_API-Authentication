using AuthApi.Data;
using AuthApi.Services.Implementations;
using AuthApi.Services.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using System.Security.Claims;

namespace AuthApi.IntegrationTests
{
    public class AuthApiFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                // Remove the app's AuthDbContext registration
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<DataContext>));

                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                // Add AuthDbContext using an in-memory database for testing
                services.AddDbContext<DataContext>(options =>
                {
                    options.UseInMemoryDatabase("InMemoryAuthDb");
                });

                // Add AuthService for DI (using the real service for integration tests)
                services.AddScoped<IAuthService, AuthService>();

                // Configure test authentication scheme
                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthHandler.AuthenticationScheme;
                    options.DefaultChallengeScheme = TestAuthHandler.AuthenticationScheme;
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                    TestAuthHandler.AuthenticationScheme, options => { });

                // Build the service provider.
                var sp = services.BuildServiceProvider();

                // Create a scope to obtain a reference to the database context (AuthDbContext)
                using (var scope = sp.CreateScope())
                {
                    var scopedServices = scope.ServiceProvider;
                    var db = scopedServices.GetRequiredService<DbContext>();
                    var logger = scopedServices.GetRequiredService<ILogger<AuthApiFactory>>();

                    // Ensure the database is created.
                    db.Database.EnsureCreated();

                    // Optional: Seed the database with test data here if needed
                    // db.Users.Add(new ApplicationUser { /* ... */ });
                    // db.SaveChanges();
                }
            });

            // Configure TestServer to use the test authentication scheme
            builder.ConfigureTestServices(services =>
            {
                services.AddAuthentication(TestAuthHandler.AuthenticationScheme)
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                        TestAuthHandler.AuthenticationScheme, options => { });
            });
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
