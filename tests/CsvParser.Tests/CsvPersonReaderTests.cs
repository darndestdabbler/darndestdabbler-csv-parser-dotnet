using System.Text;
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

    [Fact]
    public void ReadFolder_IgnoresCsvInSubfolderAndNonCsvFiles()
    {
        using var folder = new TempFolder();
        var subfolder = Path.Combine(folder.FolderPath, "sub");
        Directory.CreateDirectory(subfolder);
        WriteFile(subfolder, "nested.csv", Header, "Zed,zed@example.com,70");
        WriteFile(folder.FolderPath, "notes.txt", Header, "Yan,yan@example.com,20");
        WriteFile(folder.FolderPath, "people.csv", Header, "Ann,ann@example.com,30");

        var result = CsvPersonReader.ReadFolder(folder.FolderPath);

        Assert.Equal(new[] { new Person(0, "Ann", "ann@example.com", 30) }, result.People);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void ReadFolder_ReadsFilesInOrdinalOrder()
    {
        using var folder = new TempFolder();
        WriteFile(folder.FolderPath, "a.csv", Header, "Ann,ann@example.com,30");
        WriteFile(folder.FolderPath, "B.csv", Header, "Bob,bob@example.com,40");

        var result = CsvPersonReader.ReadFolder(folder.FolderPath);

        Assert.Equal(
            new[]
            {
                new Person(0, "Bob", "bob@example.com", 40),
                new Person(0, "Ann", "ann@example.com", 30),
            },
            result.People);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void ReadFolder_MapsColumnsInAnyOrder_AndIgnoresExtraColumns()
    {
        using var folder = new TempFolder();
        WriteFile(folder.FolderPath, "a.csv", "age,name,email", "30,Ann,ann@example.com");
        WriteFile(folder.FolderPath, "b.csv", "name,email,age,city", "Bob,bob@example.com,40,Paris");

        var result = CsvPersonReader.ReadFolder(folder.FolderPath);

        Assert.Equal(
            new[]
            {
                new Person(0, "Ann", "ann@example.com", 30),
                new Person(0, "Bob", "bob@example.com", 40),
            },
            result.People);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void ReadFolder_HeaderOnlyFile_ReturnsNoPeopleAndNoErrors()
    {
        using var folder = new TempFolder();
        WriteFile(folder.FolderPath, "people.csv", Header);

        var result = CsvPersonReader.ReadFolder(folder.FolderPath);

        Assert.Empty(result.People);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void ReadFolder_ReadsUtf8WithByteOrderMark_AndNonAsciiNamesExactly()
    {
        using var folder = new TempFolder();
        var path = Path.Combine(folder.FolderPath, "people.csv");
        File.WriteAllText(
            path,
            $"{Header}\nZoë,zoe@example.com,30\n山田,yamada@example.com,40\n",
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

        var result = CsvPersonReader.ReadFolder(folder.FolderPath);

        Assert.Equal(
            new[]
            {
                new Person(0, "Zoë", "zoe@example.com", 30),
                new Person(0, "山田", "yamada@example.com", 40),
            },
            result.People);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void ReadFolder_ReportsErrorsInFileOrder_AndBadFileDoesNotStopLaterFiles()
    {
        using var folder = new TempFolder();
        var badHeaderPath = WriteFile(folder.FolderPath, "a.csv", "name,email");
        var badAgePath = WriteFile(folder.FolderPath, "b.csv", Header, "Bob,bob@example.com,thirty");
        WriteFile(folder.FolderPath, "c.csv", Header, "Cy,cy@example.com,50");

        var result = CsvPersonReader.ReadFolder(folder.FolderPath);

        Assert.Equal(new[] { new Person(0, "Cy", "cy@example.com", 50) }, result.People);
        Assert.Equal(2, result.Errors.Count);
        Assert.Equal(badHeaderPath, result.Errors[0].FilePath);
        Assert.Equal(1, result.Errors[0].LineNumber);
        Assert.Equal(badAgePath, result.Errors[1].FilePath);
        Assert.Equal(2, result.Errors[1].LineNumber);
    }

    [Fact]
    public void ReadFolder_SkipsAgesThatAreNotIntegers_WithOneErrorEach()
    {
        using var folder = new TempFolder();
        var path = WriteFile(
            folder.FolderPath,
            "people.csv",
            Header,
            "Ann,ann@example.com,30.5",
            "Bob,bob@example.com,",
            "Cy,cy@example.com,99999999999");

        var result = CsvPersonReader.ReadFolder(folder.FolderPath);

        Assert.Empty(result.People);
        Assert.Equal(3, result.Errors.Count);
        Assert.All(result.Errors, error => Assert.Equal(path, error.FilePath));
        Assert.Equal(new[] { 2, 3, 4 }, result.Errors.Select(error => error.LineNumber));
    }

    [Fact]
    public void ReadFolder_AcceptsZeroAndNegativeAges()
    {
        using var folder = new TempFolder();
        WriteFile(folder.FolderPath, "people.csv", Header, "Ann,ann@example.com,0", "Bob,bob@example.com,-5");

        var result = CsvPersonReader.ReadFolder(folder.FolderPath);

        Assert.Equal(
            new[]
            {
                new Person(0, "Ann", "ann@example.com", 0),
                new Person(0, "Bob", "bob@example.com", -5),
            },
            result.People);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void ReadFolder_SkipsWhitespaceOnlyNameOrEmail_WithOneErrorEach()
    {
        using var folder = new TempFolder();
        WriteFile(folder.FolderPath, "people.csv", Header, "   ,ann@example.com,30", "Bob,   ,40");

        var result = CsvPersonReader.ReadFolder(folder.FolderPath);

        Assert.Empty(result.People);
        Assert.Equal(2, result.Errors.Count);
        Assert.Equal(new[] { 2, 3 }, result.Errors.Select(error => error.LineNumber));
    }

    [Fact]
    public void ReadFolder_SkipsRowWithTooFewFields_AndStillReadsLaterRows()
    {
        using var folder = new TempFolder();
        var path = WriteFile(folder.FolderPath, "people.csv", Header, "Ann,ann@example.com", "Bob,bob@example.com,40");

        var result = CsvPersonReader.ReadFolder(folder.FolderPath);

        Assert.Equal(new[] { new Person(0, "Bob", "bob@example.com", 40) }, result.People);
        var error = Assert.Single(result.Errors);
        Assert.Equal(path, error.FilePath);
        Assert.Equal(2, error.LineNumber);
    }

    [Fact]
    public void ReadFolder_ReadsQuotedFieldWithCommaAsOneValue()
    {
        using var folder = new TempFolder();
        WriteFile(folder.FolderPath, "people.csv", Header, "\"Smith, Ann\",ann@example.com,30");

        var result = CsvPersonReader.ReadFolder(folder.FolderPath);

        Assert.Equal(new[] { new Person(0, "Smith, Ann", "ann@example.com", 30) }, result.People);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void ReadFolder_TwoBadRows_GiveTwoErrorsInRowOrderWithMessages()
    {
        using var folder = new TempFolder();
        var path = WriteFile(folder.FolderPath, "people.csv", Header, "Ann,,30", "Bob,bob@example.com,thirty");

        var result = CsvPersonReader.ReadFolder(folder.FolderPath);

        Assert.Empty(result.People);
        Assert.Equal(2, result.Errors.Count);
        Assert.All(result.Errors, error =>
        {
            Assert.Equal(path, error.FilePath);
            Assert.False(string.IsNullOrWhiteSpace(error.Message));
        });
        Assert.Equal(new[] { 2, 3 }, result.Errors.Select(error => error.LineNumber));
    }

    [Fact]
    public void ReadFolder_ReturnsBothCopiesOfIdenticalValidRows()
    {
        using var folder = new TempFolder();
        WriteFile(folder.FolderPath, "people.csv", Header, "Ann,ann@example.com,30", "Ann,ann@example.com,30");

        var result = CsvPersonReader.ReadFolder(folder.FolderPath);

        Assert.Equal(
            new[]
            {
                new Person(0, "Ann", "ann@example.com", 30),
                new Person(0, "Ann", "ann@example.com", 30),
            },
            result.People);
        Assert.Empty(result.Errors);
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
