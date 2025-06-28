using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AuthApi.Data
{
    public class DataContextFactory : IDesignTimeDbContextFactory<DataContext>
    {
        public DataContext CreateDbContext(string[] args)
        {
            // 1. Figure out the AuthApi folder
            //    Adjust the ".." count as needed if your projects sit deeper.
            var projectPath = Path.GetFullPath(
                Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "..",    // from AuthApi.Data
                    "AuthApi"// into AuthApi
                ));

            // 2. Build configuration off that folder
            var environment = Environment
                                .GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                              ?? "Production";

            var config = new ConfigurationBuilder()
                .SetBasePath(projectPath)
                .AddJsonFile("appsettings.json", optional: false)
                .AddJsonFile(
                   $"appsettings.{environment}.json",
                   optional: true)
                .AddEnvironmentVariables()
                .Build();

            // 3. Read the connection string
            var connStr = config.GetConnectionString("DefaultConnection");
            if (string.IsNullOrWhiteSpace(connStr))
                throw new InvalidOperationException(
                   "Design-time: DefaultConnection is not configured.");

            // 4. Create the DbContext
            var optionsBuilder = new DbContextOptionsBuilder<DataContext>();
            optionsBuilder.UseSqlServer(connStr);

            return new DataContext(optionsBuilder.Options);
        }
    }
}
