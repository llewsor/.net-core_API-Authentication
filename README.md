/AuthApi
├──/Controllers
│ ├── AuthController.cs
├──/Services
│ ├── Interfaces
│ │ ├── IAuthService.cs ← DIP, ISP
│ ├── Implementations
│ │ ├── AuthService.cs
├──/Repositories
│ ├── Interfaces
│ │ ├── IUserRepository.cs ← DIP, ISP
│ ├── Implementations
│ │ ├── UserRepository.cs
├──/Models
│ ├── User.cs
│ ├── UserDto.cs
├──/Helpers
│ ├── ITokenHelper.cs ← DIP, ISP
│ ├── JwtTokenHelper.cs
├──/Data
│ ├── DataContext.cs
├──/Extensions
│ ├── ServiceCollectionExtensions.cs
├──appsettings.json
└──Program.cs

tests/
├── AuthApi.UnitTests/ # xUnit projects mocking IRepositories, IOptions, etc.
│ └── Services/AuthServiceTests.cs
├── AuthApi.DataTests/ # EF Core InMemory / SQLite tests for repositories
│ └── Repositories/UserRepositoryTests.cs
├── AuthApi.IntegrationTests/ # WebApplicationFactory + TestServer
│ └── AuthControllerTests.cs
│── AuthApi.E2ETests/ # Postman/Newman or Playwright scripts
│ └── auth-flows.spec.js
