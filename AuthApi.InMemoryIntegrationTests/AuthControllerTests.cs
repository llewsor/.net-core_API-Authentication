using AuthApi.Data;
using AuthApi.Helpers;
using AuthApi.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;

namespace AuthApi.InMemoryIntegrationTests
{
    public class AuthControllerTests : IClassFixture<AuthApiFactory>
    {
        private readonly HttpClient _client;
        private readonly IServiceScope _scope;

        public AuthControllerTests(AuthApiFactory factory)
        {
            _scope = factory.Services.CreateScope();
            this._client = factory.CreateClient();
        }


        [Fact]
        public async Task Register_ValidUser_ReturnsOk()
        {
            // Arrange
            var userDto = new UserRequest { Username = "test@example.com", Password = "Password123!" };

            // Act
            var response = await _client.PostAsJsonAsync("/api/Auth/register", userDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var responseContent = await response.Content.ReadAsStringAsync();
        }

        [Fact]
        public async Task Register_DuplicateUser_ReturnsBadRequest()
        {
            // Arrange
            var userDto = new UserRequest { Username = "duplicate@example.com", Password = "Password123!" };
            var response1 = await _client.PostAsJsonAsync("/api/Auth/register", userDto); // Register first time

            // Act
            var response2 = await _client.PostAsJsonAsync("/api/Auth/register", userDto); // Register again

            // Assert
            response2.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var problemDetails = await response2.Content.ReadFromJsonAsync<ProblemDetails>();
            problemDetails!.Detail.Should().Contain("Username already in use.");
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

            var loginDto = new LoginRequest { Username = email, Password = password };

            // Act
            var response = await _client.PostAsJsonAsync("/api/Auth/login", loginDto);

            // Assert
            response.Should().HaveStatusCode(expectedStatusCode);

            if (expectedStatusCode == HttpStatusCode.OK)
            {
                var tokenDto = await response.Content.ReadFromJsonAsync<TokenDto>();
                tokenDto.Should().NotBeNull();
                tokenDto!.AccessToken.Should().NotBeNullOrEmpty();
                tokenDto!.RefreshToken.Should().NotBeNullOrEmpty();
            }
            else
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                responseContent.Should().Contain("Username or password is incorrect.");
            }
        }

        [Fact]
        public async Task Refresh_ValidRefreshToken_ReturnsNewTokens()
        {
            // Arrange
            var userEmail = "refresh_user@example.com";
            var userPassword = "Password123!";
            await RegisterUserAsync(userEmail, userPassword);
            var loginResponse = await _client.PostAsJsonAsync("/api/Auth/login", new LoginRequest { Username = userEmail, Password = userPassword });
            var initialTokens = await loginResponse.Content.ReadFromJsonAsync<TokenDto>();
            initialTokens.Should().NotBeNull();

            // Act
            var refreshResponse = await _client.PostAsJsonAsync("/api/Auth/refresh", new RefreshRequest() { RefreshToken = initialTokens!.RefreshToken });

            // Assert
            refreshResponse.Should().HaveStatusCode(HttpStatusCode.OK);
            var newTokens = await refreshResponse.Content.ReadFromJsonAsync<TokenDto>();
            newTokens.Should().NotBeNull();
            newTokens!.AccessToken.Should().NotBeNullOrEmpty();
            newTokens.RefreshToken.Should().NotBeNullOrEmpty();
            newTokens.AccessToken.Should().NotBe(initialTokens.AccessToken, "Access token should be new");
            newTokens.RefreshToken.Should().NotBe(initialTokens.RefreshToken, "Refresh token should be new");

            // Verify old refresh token is revoked in DB
            using (var scope = _scope)
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();
                var oldRefreshToken = await dbContext.RefreshTokens.SingleOrDefaultAsync(rt => rt.Token == initialTokens.RefreshToken);
                oldRefreshToken.Should().NotBeNull();
                oldRefreshToken!.Revoked.Should().NotBeNull();
            }
        }

        [Fact]
        public async Task Refresh_InvalidRefreshToken_ReturnsForbidden()
        {
            // Arrange
            var request = new RefreshRequest { RefreshToken = "invalid_refresh_token" };

            // Act
            var response = await _client.PostAsJsonAsync("/api/Auth/refresh", request);

            // Assert
            response.Should().HaveStatusCode(HttpStatusCode.BadRequest);
            var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();
            problemDetails!.Detail.Should().Contain("Your session has expired. Please log in again.");
        }

        [Fact]
        public async Task GetSecret_WithValidToken_ReturnsOk()
        {
            // Arrange
            var userEmail = "secret_user@example.com";
            var userPassword = "Password123!";
            await RegisterUserAsync(userEmail, userPassword);
            var loginResponse = await _client.PostAsJsonAsync("/api/Auth/login", new LoginRequest { Username = userEmail, Password = userPassword });
            var tokens = await loginResponse.Content.ReadFromJsonAsync<TokenDto>();
            tokens.Should().NotBeNull();

            // Set the Authorization header with the valid access token
            _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {tokens!.AccessToken}");

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
            using (var scope = _scope)
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();

                PasswordHelper.CreatePasswordHash(adminUserPassword, out var passwordHash, out var passwordSalt);
                var adminUser = new User
                {
                    Username = adminUserEmail,
                    PasswordHash = passwordHash,
                    PasswordSalt = passwordSalt,
                    Role = "Admin"
                };
                await dbContext.Users.AddAsync(adminUser);
                await dbContext.SaveChangesAsync();
            }

            // Log in as admin to get token
            var loginResponse = await _client.PostAsJsonAsync("/api/Auth/login", new LoginRequest { Username = adminUserEmail, Password = adminUserPassword });
            var tokens = await loginResponse.Content.ReadFromJsonAsync<TokenDto>();
            tokens.Should().NotBeNull();

            _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {tokens!.AccessToken}");

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

            var loginResponse = await _client.PostAsJsonAsync("/api/Auth/login", new LoginRequest { Username = regularUserEmail, Password = regularUserPassword });
            var tokens = await loginResponse.Content.ReadFromJsonAsync<TokenDto>();
            tokens.Should().NotBeNull();

            _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {tokens!.AccessToken}");

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
            var userDto = new UserRequest { Username = email, Password = password };
            var response = await _client.PostAsJsonAsync("/api/Auth/register", userDto);
            response.EnsureSuccessStatusCode(); // Throws exception on non-success status
        }
    }
}
