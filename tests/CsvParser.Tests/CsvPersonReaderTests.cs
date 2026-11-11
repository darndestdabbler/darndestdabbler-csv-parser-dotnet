using CsvDeserializer;
using PersonModel;
using Xunit;

namespace CsvParser.Tests;

public class CsvPersonReaderTests
{
    private const string Header = "name,email,age";

    [Fact]
    public void ReadFolder_ReadsValidFilesInFileNameOrder_WithIdZero()
    {
        using var folder = new TempFolder();
        WriteFile(folder.FolderPath, "b.csv", Header, "Bob,bob@example.com,40");
        WriteFile(folder.FolderPath, "a.csv", Header, "Ann,ann@example.com,30", "Cy,cy@example.com,50");

        var result = CsvPersonReader.ReadFolder(folder.FolderPath);

        Assert.Equal(
            new[]
            {
                new Person(0, "Ann", "ann@example.com", 30),
                new Person(0, "Cy", "cy@example.com", 50),
                new Person(0, "Bob", "bob@example.com", 40),
            },
            result.People);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void ReadFolder_SkipsRowWithBadAge_WithOneError()
    {
        using var folder = new TempFolder();
        var path = WriteFile(folder.FolderPath, "people.csv", Header, "Ann,ann@example.com,thirty", "Bob,bob@example.com,40");

        var result = CsvPersonReader.ReadFolder(folder.FolderPath);

        Assert.Equal(new[] { new Person(0, "Bob", "bob@example.com", 40) }, result.People);
        var error = Assert.Single(result.Errors);
        Assert.Equal(path, error.FilePath);
        Assert.Equal(2, error.LineNumber);
    }

    [Fact]
    public void ReadFolder_SkipsRowWithEmptyName_WithOneError()
    {
        using var folder = new TempFolder();
        var path = WriteFile(folder.FolderPath, "people.csv", Header, ",ann@example.com,30", "Bob,bob@example.com,40");

        var result = CsvPersonReader.ReadFolder(folder.FolderPath);

        Assert.Equal(new[] { new Person(0, "Bob", "bob@example.com", 40) }, result.People);
        var error = Assert.Single(result.Errors);
        Assert.Equal(path, error.FilePath);
        Assert.Equal(2, error.LineNumber);
    }

    [Fact]
    public void ReadFolder_MissingHeader_YieldsOneErrorAtLineOneAndNoRows()
    {
        using var folder = new TempFolder();
        var path = WriteFile(folder.FolderPath, "people.csv", "name,email", "Ann,ann@example.com");

        var result = CsvPersonReader.ReadFolder(folder.FolderPath);

        Assert.Empty(result.People);
        var error = Assert.Single(result.Errors);
        Assert.Equal(path, error.FilePath);
        Assert.Equal(1, error.LineNumber);
    }

    [Fact]
    public void ReadFolder_SkipsUnparseableRow_AndKeepsValidRows()
    {
        using var folder = new TempFolder();
        var path = WriteFile(folder.FolderPath, "people.csv", Header, "Ann,ann@example.com,30", "\"Bob,bob@example.com,40");

        var result = CsvPersonReader.ReadFolder(folder.FolderPath);

        Assert.Equal(new[] { new Person(0, "Ann", "ann@example.com", 30) }, result.People);
        var error = Assert.Single(result.Errors);
        Assert.Equal(path, error.FilePath);
        Assert.Equal(3, error.LineNumber);
    }

    [Fact]
    public void ReadFolder_UnreadableHeader_YieldsErrorAndStillReadsOtherFiles()
    {
        using var folder = new TempFolder();
        var badPath = WriteFile(folder.FolderPath, "a.csv", "\"name,email,age");
        WriteFile(folder.FolderPath, "b.csv", Header, "Bob,bob@example.com,40");

        var result = CsvPersonReader.ReadFolder(folder.FolderPath);

        Assert.Equal(new[] { new Person(0, "Bob", "bob@example.com", 40) }, result.People);
        var error = Assert.Single(result.Errors);
        Assert.Equal(badPath, error.FilePath);
        Assert.Equal(1, error.LineNumber);
    }

    [Fact]
    public void ReadFolder_EmptyFile_YieldsMissingHeaderErrorAtLineOne()
    {
        using var folder = new TempFolder();
        var path = WriteFile(folder.FolderPath, "empty.csv");

        var result = CsvPersonReader.ReadFolder(folder.FolderPath);

        Assert.Empty(result.People);
        var error = Assert.Single(result.Errors);
        Assert.Equal(path, error.FilePath);
        Assert.Equal(1, error.LineNumber);
        Assert.Contains("name", error.Message);
        Assert.Contains("email", error.Message);
        Assert.Contains("age", error.Message);
    }

    [Fact]
    public void ReadFolder_MatchesHeadersCaseInsensitively()
    {
        using var folder = new TempFolder();
        WriteFile(folder.FolderPath, "people.csv", "NAME,Email,AGE", "Ann,ann@example.com,30");

        var result = CsvPersonReader.ReadFolder(folder.FolderPath);

        Assert.Equal(new[] { new Person(0, "Ann", "ann@example.com", 30) }, result.People);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void ReadFolder_SkipsRowWithEmptyEmail_WithOneError()
    {
        using var folder = new TempFolder();
        var path = WriteFile(folder.FolderPath, "people.csv", Header, "Ann,,30");

        var result = CsvPersonReader.ReadFolder(folder.FolderPath);

        Assert.Empty(result.People);
        var error = Assert.Single(result.Errors);
        Assert.Equal(path, error.FilePath);
        Assert.Equal(2, error.LineNumber);
    }

    [Fact]
    public void ReadFolder_EmptyFolder_ReturnsNoPeopleAndNoErrors()
    {
        using var folder = new TempFolder();

        var result = CsvPersonReader.ReadFolder(folder.FolderPath);

        Assert.Empty(result.People);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void ReadFolder_MissingFolder_ThrowsDirectoryNotFoundException()
    {
        var missing = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        Assert.Throws<DirectoryNotFoundException>(() => CsvPersonReader.ReadFolder(missing));
    }

    private static string WriteFile(string folder, string name, params string[] lines)
    {
        var path = Path.Combine(folder, name);
        File.WriteAllLines(path, lines);
        return path;
    }

    private sealed class TempFolder : IDisposable
    {
        public TempFolder()
        {
            FolderPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(FolderPath);
        }

        public string FolderPath { get; }

        public void Dispose()
        {
            if (Directory.Exists(FolderPath))
            {
                Directory.Delete(FolderPath, recursive: true);
            }
        }
    }
}
