using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using PersonModel;

namespace CsvDeserializer;

public static class CsvPersonReader
{
    private static readonly string[] RequiredHeaders = ["name", "email", "age"];

    public static CsvReadResult ReadFolder(string folderPath)
    {
        if (!Directory.Exists(folderPath))
        {
            throw new DirectoryNotFoundException($"Folder not found: {folderPath}");
        }

        var people = new List<Person>();
        var errors = new List<CsvRecordError>();

        var files = Directory.EnumerateFiles(folderPath, "*.csv", SearchOption.TopDirectoryOnly)
            .OrderBy(file => Path.GetFileName(file), StringComparer.Ordinal);

        foreach (var file in files)
        {
            ReadFile(file, people, errors);
        }

        return new CsvReadResult(people, errors);
    }

    private static void ReadFile(string filePath, List<Person> people, List<CsvRecordError> errors)
    {
        using var reader = new StreamReader(filePath, Encoding.UTF8);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture));

        try
        {
            csv.Read();
            csv.ReadHeader();
        }
        catch (CsvHelperException ex)
        {
            errors.Add(new CsvRecordError(filePath, 1, ex.Message));
            return;
        }

        var headers = (csv.HeaderRecord ?? []).Select(header => header.Trim().ToLowerInvariant()).ToList();

        var missing = RequiredHeaders.Where(required => !headers.Contains(required)).ToList();
        if (missing.Count > 0)
        {
            errors.Add(new CsvRecordError(filePath, 1, $"Missing required header(s): {string.Join(", ", missing)}"));
            return;
        }

        var nameIndex = headers.IndexOf("name");
        var emailIndex = headers.IndexOf("email");
        var ageIndex = headers.IndexOf("age");

        while (true)
        {
            try
            {
                if (!csv.Read())
                {
                    break;
                }

                var line = csv.Context.Parser.Row;
                var name = csv.GetField(nameIndex);
                var email = csv.GetField(emailIndex);
                var ageText = csv.GetField(ageIndex);

                if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email))
                {
                    errors.Add(new CsvRecordError(filePath, line, "Name and email must not be empty."));
                    continue;
                }

                if (!int.TryParse(ageText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var age))
                {
                    errors.Add(new CsvRecordError(filePath, line, $"Age '{ageText}' is not an integer."));
                    continue;
                }

                people.Add(new Person(0, name, email, age));
            }
            catch (CsvHelperException ex)
            {
                errors.Add(new CsvRecordError(filePath, csv.Context.Parser.Row, ex.Message));
            }
        }
    }
}
