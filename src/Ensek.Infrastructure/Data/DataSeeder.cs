using CsvHelper;
using Ensek.Core.Models;
using System.Globalization;

namespace Ensek.Infrastructure.Data;

public static class DataSeeder
{
    public static void SeedAccounts(AppDbContext context, string csvPath = "Test_Accounts.csv")
    {
        if (context.Accounts.Any()) return;

        using var reader = new StreamReader(csvPath);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

        var accounts = csv.GetRecords<Account>().ToList();
        context.Accounts.AddRange(accounts);
        context.SaveChanges();
    }
}