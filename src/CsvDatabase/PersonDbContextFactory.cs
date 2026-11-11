using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CsvDatabase;

public class PersonDbContextFactory : IDesignTimeDbContextFactory<PersonDbContext>
{
    private const string ConnectionString =
        @"Server=(localdb)\MSSQLLocalDB;Database=CsvParser;Integrated Security=true;TrustServerCertificate=true";

    public PersonDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<PersonDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;

        return new PersonDbContext(options);
    }
}
