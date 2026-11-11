using CsvDatabase;
using CsvDeserializer;
using Microsoft.EntityFrameworkCore;

namespace CsvParserApp;

public static class CsvParserRunner
{
    public static int Run(string[] args, TextWriter stdout, TextWriter stderr, string connectionString)
    {
        if (args.Length != 1)
        {
            stderr.WriteLine("usage: CsvParserApp <csv-folder-path>");
            return 1;
        }

        var folderPath = args[0];
        if (!Directory.Exists(folderPath))
        {
            stderr.WriteLine($"error: folder not found: {folderPath}");
            return 1;
        }

        try
        {
            var options = new DbContextOptionsBuilder<PersonDbContext>()
                .UseSqlServer(connectionString)
                .Options;

            using (var context = new PersonDbContext(options))
            {
                context.Database.Migrate();
            }

            var result = CsvPersonReader.ReadFolder(folderPath);
            foreach (var error in result.Errors)
            {
                stderr.WriteLine($"warning: {error.FilePath}:{error.LineNumber}: {error.Message}");
            }

            var repository = new PersonRepository(options);
            repository.SaveAll(result.People);

            stdout.WriteLine("name | email | age");
            foreach (var person in repository.GetAll())
            {
                stdout.WriteLine($"{person.Name} | {person.Email} | {person.Age}");
            }

            return 0;
        }
        catch (Exception ex)
        {
            stderr.WriteLine($"error: {ex.Message}");
            return 2;
        }
    }
}
