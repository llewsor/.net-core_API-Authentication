using AuthApi.Data;
using AuthApi.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AuthApi.IntegrationTests
{
    // IClassFixture ensures a single instance of AuthApiFactory is created for all tests in this class
    public class AuthControllerIntegrationTests : IClassFixture<AuthApiFactory>
    {
        private readonly AuthApiFactory _factory;
        private readonly HttpClient _client;

        public AuthControllerIntegrationTests(AuthApiFactory factory)
        {
            _factory = factory;
            _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false // Prevent automatic redirects that might obscure actual responses
            });

            // Ensure a clean database for each test run
            using (var scope = _factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();
                dbContext.Database.EnsureDeleted(); // Clear the in-memory database
                dbContext.Database.EnsureCreated(); // Recreate it
            }
        }

        [Fact]
        public async Task Register_ValidUser_ReturnsOk()
        {
            // Arrange
            var userDto = new UserDto { Username = "test@example.com", Password = "Password123!" };

            // Act
            var response = await _client.PostAsJsonAsync("/api/Auth/register", userDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK); // Replace 'Should()' with FluentAssertions syntax
            var responseContent = await response.Content.ReadAsStringAsync();
            responseContent.Should().Contain("Registered");

            // Verify user is in DB
            using (var scope = _factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();
                var user = await dbContext.Users.SingleOrDefaultAsync(u => u.Username == userDto.Username);
                user.Should().NotBeNull();
                BCrypt.Net.BCrypt.Verify(userDto.Password, user.PasswordHash.ToString()).Should().BeTrue();
            }
        }

        [Fact]
        public async Task Register_DuplicateUser_ReturnsBadRequest()
        {
            // Arrange
            var userDto = new UserDto { Username = "duplicate@example.com", Password = "Password123!" };
            await _client.PostAsJsonAsync("/api/Auth/register", userDto); // Register first time

            // Act
            var response = await _client.PostAsJsonAsync("/api/Auth/register", userDto); // Register again

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest); // Replace 'Should()' with FluentAssertions syntax
            var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();
            problemDetails.Detail.Should().Contain("Email already registered");
        }

        [Theory]
        [InlineData("user@example.com", "Password123!", HttpStatusCode.OK)]
        [InlineData("nonexistent@example.com", "Password123!", HttpStatusCode.Unauthorized)]
        [InlineData("user@example.com", "WrongPassword!", HttpStatusCode.Unauthorized)]
        public async Task Login_ReturnsCorrectStatusCodeAndToken(string email, string password, HttpStatusCode expectedStatusCode)
        {
            // Arrange
            if (expectedStatusCode == HttpStatusCode.OK)
            {
                await RegisterUserAsync(email, password); // Ensure user exists for successful login
            }

            var loginDto = new LoginDto { Username = email, Password = password };

            // Act
            var response = await _client.PostAsJsonAsync("/api/Auth/login", loginDto);

            // Assert
            response.Should().HaveStatusCode(expectedStatusCode);

            if (expectedStatusCode == HttpStatusCode.OK)
            {
                var tokenDto = await response.Content.ReadFromJsonAsync<TokenDto>();
                tokenDto.Should().NotBeNull();
                tokenDto.AccessToken.Should().NotBeNullOrEmpty();
                tokenDto.RefreshToken.Should().NotBeNullOrEmpty();
            }
            else
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                responseContent.Should().Contain("Invalid credentials");
            }
        }

        [Fact]
        public async Task Refresh_ValidRefreshToken_ReturnsNewTokens()
        {
            // Arrange
            var userEmail = "refresh_user@example.com";
            var userPassword = "Password123!";
            await RegisterUserAsync(userEmail, userPassword);
            var loginResponse = await _client.PostAsJsonAsync("/api/Auth/login", new LoginDto { Username = userEmail, Password = userPassword });
            var initialTokens = await loginResponse.Content.ReadFromJsonAsync<TokenDto>();
            initialTokens.Should().NotBeNull();

            // Act
            var refreshResponse = await _client.PostAsJsonAsync("/api/Auth/refresh", new TokenDto { RefreshToken = initialTokens.RefreshToken });

            // Assert
            refreshResponse.Should().HaveStatusCode(HttpStatusCode.OK);
            var newTokens = await refreshResponse.Content.ReadFromJsonAsync<TokenDto>();
            newTokens.Should().NotBeNull();
            newTokens.AccessToken.Should().NotBeNullOrEmpty();
            newTokens.RefreshToken.Should().NotBeNullOrEmpty();
            newTokens.AccessToken.Should().NotBe(initialTokens.AccessToken, "Access token should be new");
            newTokens.RefreshToken.Should().NotBe(initialTokens.RefreshToken, "Refresh token should be new");

            // Verify old refresh token is revoked in DB
            using (var scope = _factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();
                var oldRefreshToken = await dbContext.RefreshTokens.SingleOrDefaultAsync(rt => rt.Token == initialTokens.RefreshToken);
                oldRefreshToken.Should().NotBeNull();
                oldRefreshToken.Revoked.Should().NotBeNull();
            }
        }

        [Fact]
        public async Task Refresh_InvalidRefreshToken_ReturnsForbidden()
        {
            // Arrange
            var invalidTokenDto = new TokenDto { RefreshToken = "invalid_refresh_token" };

            // Act
            var response = await _client.PostAsJsonAsync("/api/Auth/refresh", invalidTokenDto);

            // Assert
            response.Should().HaveStatusCode(HttpStatusCode.Forbidden);
            var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();
            problemDetails.Detail.Should().Contain("Invalid or expired refresh token");
        }

        [Fact]
        public async Task GetSecret_WithValidToken_ReturnsOk()
        {
            // Arrange
            var userEmail = "secret_user@example.com";
            var userPassword = "Password123!";
            await RegisterUserAsync(userEmail, userPassword);
            var loginResponse = await _client.PostAsJsonAsync("/api/Auth/login", new LoginDto { Username = userEmail, Password = userPassword });
            var tokens = await loginResponse.Content.ReadFromJsonAsync<TokenDto>();
            tokens.Should().NotBeNull();

            // Set the Authorization header with the valid access token
            _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {tokens.AccessToken}");

            // Act
            var response = await _client.GetAsync("/api/Auth/secret");

            // Assert
            response.Should().HaveStatusCode(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Be("You are authorized!");

            // Clean up header for subsequent tests
            _client.DefaultRequestHeaders.Remove("Authorization");
        }

        [Fact]
        public async Task GetSecret_WithoutToken_ReturnsUnauthorized()
        {
            // Arrange - No token set

            // Act
            var response = await _client.GetAsync("/api/Auth/secret");

            // Assert
            response.Should().HaveStatusCode(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetAdminData_WithAdminToken_ReturnsOk()
        {
            // Arrange
            var adminUserEmail = "admin@example.com";
            var adminUserPassword = "AdminPassword123!";

            // Register an admin user (directly manipulate DB for role)
            using (var scope = _factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();
                var adminUser = new User
                {
                    Username = adminUserEmail,
                    //TODO Implementation
                    // PasswordHash = BCrypt.Net.BCrypt.HashPassword(adminUserPassword),
                    Role = "Admin"
                };
                await dbContext.Users.AddAsync(adminUser);
                await dbContext.SaveChangesAsync();
            }

            // Log in as admin to get token
            var loginResponse = await _client.PostAsJsonAsync("/api/Auth/login", new LoginDto { Username = adminUserEmail, Password = adminUserPassword });
            var tokens = await loginResponse.Content.ReadFromJsonAsync<TokenDto>();
            tokens.Should().NotBeNull();

            _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {tokens.AccessToken}");

            // Act
            var response = await _client.GetAsync("/api/Auth/admin-data");

            // Assert
            response.Should().HaveStatusCode(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Be("Only admins see this");

            _client.DefaultRequestHeaders.Remove("Authorization");
        }

        [Fact]
        public async Task GetAdminData_WithUserToken_ReturnsForbidden()
        {
            // Arrange
            var regularUserEmail = "regular_user@example.com";
            var regularUserPassword = "Password123!";
            await RegisterUserAsync(regularUserEmail, regularUserPassword); // Default role is "User"

            var loginResponse = await _client.PostAsJsonAsync("/api/Auth/login", new LoginDto { Username = regularUserEmail, Password = regularUserPassword });
            var tokens = await loginResponse.Content.ReadFromJsonAsync<TokenDto>();
            tokens.Should().NotBeNull();

            _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {tokens.AccessToken}");

            // Act
            var response = await _client.GetAsync("/api/Auth/admin-data");

            // Assert
            response.Should().HaveStatusCode(HttpStatusCode.Forbidden);

            _client.DefaultRequestHeaders.Remove("Authorization");
        }

        [Fact]
        public async Task GetAdminData_WithoutToken_ReturnsUnauthorized()
        {
            // Arrange - No token set

            // Act
            var response = await _client.GetAsync("/api/Auth/admin-data");

            // Assert
            response.Should().HaveStatusCode(HttpStatusCode.Unauthorized);
        }

        // Helper method to register a user
        private async Task RegisterUserAsync(string email, string password)
        {
            var userDto = new UserDto { Username = email, Password = password };
            var response = await _client.PostAsJsonAsync("/api/Auth/register", userDto);
            response.EnsureSuccessStatusCode(); // Throws exception on non-success status
        }
    }
}
