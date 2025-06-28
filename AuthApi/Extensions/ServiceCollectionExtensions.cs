using AuthApi.Helpers;
using AuthApi.Repositories.Implementations;
using AuthApi.Repositories.Interfaces;
using AuthApi.Services.Implementations;
using AuthApi.Services.Interfaces;

namespace AuthApi.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddAuthServices(this IServiceCollection services)
        {
            // Register repositories
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
            // Register services
            services.AddScoped<IAuthService, AuthService>();
            // Register token helper
            services.AddSingleton<ITokenHelper, JTWTokenHelper>();

            return services;
        }
    }
}
