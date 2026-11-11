using Microsoft.EntityFrameworkCore;
using PersonModel;

namespace CsvDatabase;

public class PersonRepository
{
    private readonly DbContextOptions<PersonDbContext> _options;

    public PersonRepository(DbContextOptions<PersonDbContext> options)
    {
        _options = options;
    }

    public int SaveAll(IEnumerable<Person> people)
    {
        using var context = new PersonDbContext(_options);
        var rows = people.Select(p => new Person(0, p.Name, p.Email, p.Age)).ToList();
        if (rows.Count == 0)
        {
            return 0;
        }

        context.People.AddRange(rows);
        context.SaveChanges();
        return rows.Count;
    }

    public List<Person> GetAll()
    {
        using var context = new PersonDbContext(_options);
        return context.People
            .AsNoTracking()
            .OrderBy(p => p.Id)
            .ToList();
    }
}
