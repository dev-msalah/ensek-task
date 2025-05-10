using Ensek.Infrastructure.Data;
using Ensek.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Ensek.Api.Extensions;
public static class AppDatabaseExtensions
{
    public static IApplicationBuilder UseDatabaseMigrationAndSeeding(this IApplicationBuilder app)
    {
        using (var scope = app.ApplicationServices.CreateScope())
        {
            var services = scope.ServiceProvider;
            try
            {
                var context = services.GetRequiredService<AppDbContext>();
                context.Database.Migrate();

                var env = services.GetRequiredService<IWebHostEnvironment>();
                var csvPath = Path.Combine(env.ContentRootPath, "Data", "Test_Accounts.csv");

                DataSeeder.SeedAccounts(context, csvPath);
            }
            catch (Exception ex)
            {
                var logger = services.GetRequiredService<ILogger<Program>>();
                logger.LogError(ex, "An error occurred while seeding the database.");
            }
        }

        return app;
    }
}