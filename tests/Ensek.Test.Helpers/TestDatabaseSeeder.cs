using Ensek.Core.Models;
using Ensek.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Ensek.Test.Helpers;

public static class TestDatabaseSeeder
{
    public static async Task SeedTestDataAsync(AppDbContext context)
    {
        if (await context.Accounts.AnyAsync()) return;

        var accounts = new List<Account>
        {
            new Account { AccountId = 123, FirstName = "Mohamed", LastName = "Salah" },
            new Account { AccountId = 456, FirstName = "Aseel", LastName = "Salah" },
            new Account { AccountId = 600, FirstName = "Dalida", LastName = "Salah" }
        };

        await context.Accounts.AddRangeAsync(accounts);
        await context.SaveChangesAsync();
    }
}