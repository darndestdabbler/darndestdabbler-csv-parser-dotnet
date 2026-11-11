using CsvDatabase;
using Microsoft.EntityFrameworkCore;
using PersonModel;
using Xunit;

namespace CsvParser.Tests;

public sealed class PersonRepositoryFixture : IDisposable
{
    public PersonRepositoryFixture()
    {
        var connectionString =
            $@"Server=(localdb)\MSSQLLocalDB;Database=CsvParserTests_{Guid.NewGuid():N};Integrated Security=true;TrustServerCertificate=true";
        Options = new DbContextOptionsBuilder<PersonDbContext>().UseSqlServer(connectionString).Options;

        using var context = new PersonDbContext(Options);
        context.Database.Migrate();
    }

    public DbContextOptions<PersonDbContext> Options { get; }

    public void Dispose()
    {
        using var context = new PersonDbContext(Options);
        context.Database.EnsureDeleted();
    }
}

public class PersonRepositoryTests : IClassFixture<PersonRepositoryFixture>
{
    private readonly PersonRepositoryFixture _fixture;
    private readonly PersonRepository _repository;

    public PersonRepositoryTests(PersonRepositoryFixture fixture)
    {
        _fixture = fixture;
        _repository = new PersonRepository(fixture.Options);

        using var context = new PersonDbContext(fixture.Options);
        context.People.ExecuteDelete();
    }

    private int RowCount()
    {
        using var context = new PersonDbContext(_fixture.Options);
        return context.People.Count();
    }

    [Fact]
    public void SaveAll_ReturnsCountSaved_AndAssignsDistinctPositiveIds()
    {
        var people = new[]
        {
            new Person(0, "Ada", "ada@example.com", 36),
            new Person(0, "Grace", "grace@example.com", 85),
        };

        var saved = _repository.SaveAll(people);

        Assert.Equal(2, saved);
        var stored = _repository.GetAll();
        Assert.Equal(2, stored.Count);
        Assert.All(stored, p => Assert.True(p.Id > 0));
        Assert.Equal(2, stored.Select(p => p.Id).Distinct().Count());
    }

    [Fact]
    public void GetAll_FromNewContext_ReturnsValuesOrderedByIdInSaveOrder()
    {
        var people = new[]
        {
            new Person(0, "Charlie", "charlie@example.com", 40),
            new Person(0, "Bea", "bea@example.com", 30),
            new Person(0, "Alan", "alan@example.com", 50),
        };
        _repository.SaveAll(people);

        var stored = new PersonRepository(_fixture.Options).GetAll();

        Assert.Equal(new[] { "Charlie", "Bea", "Alan" }, stored.Select(p => p.Name));
        Assert.Equal(new[] { "charlie@example.com", "bea@example.com", "alan@example.com" }, stored.Select(p => p.Email));
        Assert.Equal(new[] { 40, 30, 50 }, stored.Select(p => p.Age));
        Assert.Equal(stored.OrderBy(p => p.Id).Select(p => p.Id), stored.Select(p => p.Id));
    }

    [Fact]
    public void GetAll_OnEmptyTable_ReturnsEmptyList()
    {
        Assert.Empty(_repository.GetAll());
    }

    [Fact]
    public void SaveAll_OfEmptySequence_ReturnsZeroAndWritesNothing()
    {
        var saved = _repository.SaveAll(Array.Empty<Person>());

        Assert.Equal(0, saved);
        Assert.Equal(0, RowCount());
    }

    [Fact]
    public void SecondSaveAll_AddsToFirst_AndSameValuesTwiceStoreTwoRows()
    {
        var person = new Person(0, "Dana", "dana@example.com", 28);
        _repository.SaveAll(new[] { person });
        _repository.SaveAll(new[] { new Person(0, "Eli", "eli@example.com", 22) });
        Assert.Equal(2, RowCount());

        _repository.SaveAll(new[] { person, person });

        Assert.Equal(4, RowCount());
    }

    [Fact]
    public void MaximumLengthAndNonAsciiText_AreStoredAndReadBackExactly()
    {
        var longName = new string('n', 200);
        var longEmail = new string('e', 200);
        var people = new[]
        {
            new Person(0, longName, longEmail, 33),
            new Person(0, "Zoë", "zoe@example.com", 44),
            new Person(0, "山田", "yamada@example.com", 55),
        };

        _repository.SaveAll(people);

        var stored = _repository.GetAll();
        Assert.Equal(longName, stored[0].Name);
        Assert.Equal(longEmail, stored[0].Email);
        Assert.Equal("Zoë", stored[1].Name);
        Assert.Equal("山田", stored[2].Name);
    }

    [Fact]
    public void SaveAll_WithOverlongName_ThrowsAndLeavesExistingRowsOnly()
    {
        _repository.SaveAll(new[] { new Person(0, "Frank", "frank@example.com", 60) });
        var before = RowCount();

        var batch = new[]
        {
            new Person(0, "Gina", "gina@example.com", 31),
            new Person(0, new string('x', 201), "long@example.com", 32),
        };

        Assert.Throws<DbUpdateException>(() => _repository.SaveAll(batch));
        Assert.Equal(before, RowCount());
    }
}
