using CsvDatabase;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using PersonModel;
using Xunit;

namespace CsvParser.Tests;

public sealed class PersonDbContextFixture : IDisposable
{
    public PersonDbContextFixture()
    {
        var connectionString =
            $@"Server=(localdb)\MSSQLLocalDB;Database=CsvParserTests_{Guid.NewGuid():N};Integrated Security=true;TrustServerCertificate=true";
        Options = new DbContextOptionsBuilder<PersonDbContext>().UseSqlServer(connectionString).Options;
    }

    public DbContextOptions<PersonDbContext> Options { get; }

    public void Dispose()
    {
        using var context = new PersonDbContext(Options);
        context.Database.EnsureDeleted();
    }
}

public class PersonDbContextTests : IClassFixture<PersonDbContextFixture>
{
    private readonly PersonDbContextFixture _fixture;

    public PersonDbContextTests(PersonDbContextFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void Model_MapsPersonToPeopleTableWithConstraints()
    {
        using var context = new PersonDbContext(_fixture.Options);

        var entity = context.Model.FindEntityType(typeof(Person));

        Assert.NotNull(entity);
        Assert.Equal("People", entity!.GetTableName());
        var key = entity.FindPrimaryKey();
        Assert.NotNull(key);
        Assert.Equal(new[] { "Id" }, key!.Properties.Select(p => p.Name));
        Assert.Equal(ValueGenerated.OnAdd, entity.FindProperty("Id")!.ValueGenerated);
        Assert.False(entity.FindProperty("Name")!.IsNullable);
        Assert.Equal(200, entity.FindProperty("Name")!.GetMaxLength());
        Assert.False(entity.FindProperty("Email")!.IsNullable);
        Assert.Equal(200, entity.FindProperty("Email")!.GetMaxLength());
        Assert.False(entity.FindProperty("Age")!.IsNullable);
    }

    [Fact]
    public void Migrate_AppliesInitialCreate_LeavesNoPendingMigrationsOrModelChanges()
    {
        using var context = new PersonDbContext(_fixture.Options);

        context.Database.Migrate();

        Assert.Contains(context.Database.GetAppliedMigrations(), m => m.EndsWith("_InitialCreate", StringComparison.Ordinal));
        Assert.Empty(context.Database.GetPendingMigrations());
        Assert.False(context.Database.HasPendingModelChanges());
    }

    [Fact]
    public void Migrate_CalledTwice_ChangesNothingAndDoesNotThrow()
    {
        using var context = new PersonDbContext(_fixture.Options);
        context.Database.Migrate();
        var before = context.Database.GetAppliedMigrations().ToList();

        var exception = Record.Exception(() => context.Database.Migrate());

        Assert.Null(exception);
        Assert.Equal(before, context.Database.GetAppliedMigrations().ToList());
    }
}
