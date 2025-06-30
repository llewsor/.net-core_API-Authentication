using AuthApi.Data;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using System.Net;
using System.Net.Http.Json;

namespace AuthApi.UnitTests;

public class AuthApiIntegrationTests
    : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly WebApplicationFactory<Program> _factory;

    public AuthApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            // Replace the real DbContext with InMemory for tests
            builder
                .UseContentRoot(Path.Combine(Directory.GetCurrentDirectory(), "..", "YourApiProject"))
                .ConfigureServices(services =>
                {
                    var descriptor = services
                      .Single(d => d.ServiceType == typeof(DbContextOptions<DataContext>));
                    services.Remove(descriptor);

                    services.AddDbContext<DataContext>(opts =>
                        opts.UseInMemoryDatabase("TestDb"));

                    // Ensure DB is created
                    var sp = services.BuildServiceProvider();
                    using var scope = sp.CreateScope();
                    scope.ServiceProvider.GetRequiredService<DataContext>().Database.EnsureCreated();
                });
        });
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task Register_Login_RefreshFlow_Works()
    {
        // 1) Register a new user
        var registerResp = await _client.PostAsJsonAsync("/auth/register", new
        {
            Username = "bob",
            Password = "P@ssw0rd"
        });
        registerResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // 2) Login
        var loginResp = await _client.PostAsJsonAsync("/auth/login", new
        {
            Username = "bob",
            Password = "P@ssw0rd"
        });
        loginResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var loginJson = await loginResp.Content.ReadFromJsonAsync<LoginResult>();
        loginJson!.Token.Should().NotBeNullOrEmpty();
        loginJson.RefreshToken.Should().NotBeNullOrEmpty();

        // 3) Use the refresh token
        var refreshResp = await _client.PostAsJsonAsync("/auth/refresh", new
        {
            Token = loginJson.Token,
            RefreshToken = loginJson.RefreshToken
        });
        refreshResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var refreshJson = await refreshResp.Content.ReadFromJsonAsync<LoginResult>();
        refreshJson!.Token.Should().NotBe(loginJson.Token);
    }

    record LoginResult(string Token, string RefreshToken);
}
