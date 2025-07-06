using AuthApi.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AuthApi.InMemoryIntegrationTests
{
    public class AuthApiFactory : WebApplicationFactory<Program>
    {
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
                    options.UseInMemoryDatabase($"IntegrationTestDb"));
            });
        }
    }
}
