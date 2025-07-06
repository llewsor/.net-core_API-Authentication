using AuthApi.Exceptions;
using AuthApi.Helpers;
using AuthApi.Models;
using AuthApi.Repositories.Interfaces;
using AuthApi.Services.Implementations;
using AuthApi.Services.Interfaces;
using FluentAssertions;
using Moq;

namespace AuthApi.UnitTests;
public class AuthServiceTests
{
    private readonly IAuthService _service;
    private readonly Mock<IUserRepository> _userRepo;
    private readonly Mock<IRefreshTokenRepository> _refreshRepo;
    private readonly Mock<ITokenHelper> _tokenHelper;

    private readonly string _ipAddress = "1.2.3.4";

    public AuthServiceTests()
    {
        _userRepo = new Mock<IUserRepository>();
        _refreshRepo = new Mock<IRefreshTokenRepository>();
        _tokenHelper = new Mock<ITokenHelper>();
        _service = new AuthService(
            _userRepo.Object,
            _tokenHelper.Object,
            _refreshRepo.Object
        );
    }

    private static User CreateTestUser(string username, string password)
    {
        PasswordHelper.CreatePasswordHash(
            password,
            out var hash,
            out var salt);

        return new User
        {
            Id = 123,
            Username = username,
            PasswordHash = hash,
            PasswordSalt = salt,
            Role = "User",
            IsBlocked = false
        };
    }

    private LoginDto GivenLoginDto(string username, string password, out User user)
    {
        var dto = new LoginDto { Username = username, Password = password };
        user = CreateTestUser(username, password);
        _userRepo.Setup(x => x.GetByUsernameAsync(username)).ReturnsAsync(user);
        return dto;
    }

    // -------------------------
    // AuthenticateAsync tests
    // -------------------------

    [Fact]
    public async Task AuthenticateAsync_WithUnknownUsername_ShouldThrowInvalidCredentialsException()
    {
        // Arrange
        var dto = new LoginDto { Username = "noone", Password = "pwd" };
        _userRepo.Setup(r => r.GetByUsernameAsync(dto.Username))
                 .ReturnsAsync((User?)null);

        // Act
        Func<Task> act = () => _service.AuthenticateAsync(dto, _ipAddress);

        // Assert
        await act.Should().ThrowAsync<InvalidCredentialsException>();
    }

    [Fact]
    public async Task AuthenticateAsync_WithIncorrectPassword_ShouldThrowInvalidCredentialsException()
    {
        // Arrange
        var dto = GivenLoginDto("alice", "P@ssw0rd", out var _);

        dto.Password = "wrongpwd"; // Simulate wrong password

        // Act
        Func<Task> act = () => _service.AuthenticateAsync(dto, _ipAddress);

        // Assert
        await act.Should().ThrowAsync<InvalidCredentialsException>();
    }

    [Fact]
    public async Task AuthenticateAsync_WithBlockedUser_ShouldThrowUserBlockedException()
    {
        // Arrange
        var dto = new LoginDto { Username = "noone", Password = "pwd" };
        var user = CreateTestUser(dto.Username, dto.Password);
        user.IsBlocked = true; // Simulate blocked user

        _userRepo
          .Setup(x => x.GetByUsernameAsync(dto.Username))
          .ReturnsAsync(user);

        // Act
        Func<Task> act = () => _service.AuthenticateAsync(dto, _ipAddress);

        // Assert
        await act.Should().ThrowAsync<UserBlockedException>();
    }

    [Theory]
    [InlineData("alice", "P@ssw0rd")]
    [InlineData("s", "S")]
    [InlineData("charlie", "1234512345Passw0rd!")]
    public async Task AuthenticateAsync_WithValidCredentials_ShouldReturnValidTokenDto(string username, string password)
    {
        // Arrange
        var dto = GivenLoginDto(username, password, out var user);

        _tokenHelper
          .Setup(x => x.CreateRefreshToken(user.Id, _ipAddress))
          .Returns(new RefreshToken { Token = "r-token", User = user });

        _tokenHelper
          .Setup(x => x.CreateToken(user))
          .Returns("jwt-token");

        // Act
        var result = await _service.AuthenticateAsync(dto, _ipAddress);

        // Assert
        result.AccessToken.Should().Be("jwt-token");
        result.RefreshToken.Should().Be("r-token");

        _refreshRepo.Verify(x => x.AddAsync(It.Is<RefreshToken>(
            t => t.Token == "r-token" && t.User.Id == user.Id)), Times.Once);
        _refreshRepo.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    // -------------------------
    // RegisterAsync tests
    // -------------------------

    [Theory]
    [InlineData("alice", "P@ssw0rd")]
    [InlineData("s", "S")]
    [InlineData("charlie", "1234512345Passw0rd!")]
    public async Task RegisterAsync_WithValidUserDto_ShouldAddUserAndSaveChanges(string username, string password)
    {
        // Arrange
        var dto = new UserDto
        {
            Username = username,
            Password = password
        };

        _userRepo
          .Setup(x => x.AddAsync(It.IsAny<User>()))
          .Returns(Task.CompletedTask);

        _userRepo
          .Setup(x => x.SaveChangesAsync())
          .Returns(Task.CompletedTask);

        // Act
        await _service.RegisterAsync(dto);

        // Assert
        _userRepo.Verify(x => x.AddAsync(It.Is<User>(
            u => u.Username == dto.Username
                 && u.PasswordHash.Length > 0
                 && u.PasswordSalt.Length > 0)), Times.Once);

        _userRepo.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_WithAlreadyUsernameUsed_ShouldThrowAlreadyRegisteredException()
    {
        // Arrange
        var dto = new UserDto() { Username = "alice", Password = "P@ssw0rd" };

        GivenLoginDto(dto.Username, dto.Password, out var _);

        // Act
        Func<Task> act = () => _service.RegisterAsync(dto);

        // Assert
        await act.Should().ThrowAsync<UsernameInUseException>();
    }

    // -------------------------
    // RefreshTokenAsync tests
    // -------------------------

    [Fact]
    public async Task RefreshTokenAsync_WithNonExistingToken_ShouldThrowRefreshTokenExpiredException()
    {
        // Arrange
        var refreshToken = "bad";

        _refreshRepo.Setup(x => x.GetByTokenAsync(refreshToken))
                    .ReturnsAsync((RefreshToken)null!);

        // Act & Assert
        await Assert.ThrowsAsync<RefreshTokenExpiredException>(() =>
            _service.RefreshTokenAsync(refreshToken, _ipAddress));

        // Verify that the repository was called
        _refreshRepo.Verify(x => x.GetByTokenAsync(refreshToken), Times.Once);
    }

    [Fact]
    public async Task RefreshTokenAsync_WithInactiveToken_ShouldThrowRefreshTokenExpiredException()
    {
        //Arrange
        var expiredToken = new RefreshToken { Token = "old", Expires = DateTime.UtcNow.AddMinutes(-5) };
        _refreshRepo.Setup(x => x.GetByTokenAsync(expiredToken.Token))
                    .ReturnsAsync(expiredToken);

        // Act & Assert
        await Assert.ThrowsAsync<RefreshTokenExpiredException>(() =>
            _service.RefreshTokenAsync("old", _ipAddress));

        // Verify that the repository was called
        _refreshRepo.Verify(x => x.GetByTokenAsync(expiredToken.Token), Times.Once);
    }

    [Fact]
    public async Task RefreshTokenAsync_WithValidActiveToken_ShouldRevokeOldTokenAndReturnNewTokenDto()
    {
        // Arrange
        var user = new User { Id = 42, Username = "carl" };
        var before = DateTime.UtcNow;
        var oldRefreshToken = new RefreshToken
        {
            User = user,
            Token = "good",
            Expires = before.AddMinutes(5),
        };

        _refreshRepo.Setup(r => r.GetByTokenAsync(oldRefreshToken.Token))
                    .ReturnsAsync(oldRefreshToken);

        var newRefreshToken = new RefreshToken { Token = "new-rt" };
        const string tk = "new-at";

        _tokenHelper.Setup(t => t.CreateRefreshToken(user.Id, _ipAddress))
                    .Returns(newRefreshToken);
        _tokenHelper.Setup(t => t.CreateToken(user))
                    .Returns(tk);

        // Act
        var dto = await _service.RefreshTokenAsync(oldRefreshToken.Token, _ipAddress);

        // Assert
        dto.AccessToken.Should().Be(tk);
        dto.RefreshToken.Should().Be(newRefreshToken.Token);

        Assert.True(oldRefreshToken.Revoked >= before && oldRefreshToken.Revoked <= DateTime.UtcNow);

        oldRefreshToken.Revoked.Should().BeCloseTo(DateTime.UtcNow, precision: TimeSpan.FromSeconds(1));
        oldRefreshToken.RevokedByIp.Should().Be(_ipAddress);

        _refreshRepo.Verify(r => r.GetByTokenAsync(oldRefreshToken.Token), Times.Once);
        _refreshRepo.Verify(r => r.AddAsync(newRefreshToken), Times.Once);
        _refreshRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }
}
