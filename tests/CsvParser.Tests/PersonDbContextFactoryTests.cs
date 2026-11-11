using CsvDatabase;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CsvParser.Tests;

public class PersonDbContextFactoryTests
{
    [Fact]
    public void CreateDbContext_ReturnsSqlServerContextPointingAtLocalDb_WithoutOpeningConnection()
    {
        var factory = new PersonDbContextFactory();

        using var context = factory.CreateDbContext(Array.Empty<string>());

        Assert.Equal("Microsoft.EntityFrameworkCore.SqlServer", context.Database.ProviderName);
        Assert.Contains(@"(localdb)\MSSQLLocalDB", context.Database.GetConnectionString());
    }
}
