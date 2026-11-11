using CsvDatabase;
using CsvParserApp;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CsvParser.Tests;

public sealed class CsvParserRunnerFixture : IDisposable
{
    public CsvParserRunnerFixture()
    {
        ConnectionString = ConnectionStringFor($"CsvParserTests_{Guid.NewGuid():N}");
        Options = new DbContextOptionsBuilder<PersonDbContext>().UseSqlServer(ConnectionString).Options;

        using var context = new PersonDbContext(Options);
        context.Database.Migrate();
    }

    public string ConnectionString { get; }

    public DbContextOptions<PersonDbContext> Options { get; }

    public static string ConnectionStringFor(string database) =>
        $@"Server=(localdb)\MSSQLLocalDB;Database={database};Integrated Security=true;TrustServerCertificate=true";

    public void Dispose()
    {
        using var context = new PersonDbContext(Options);
        context.Database.EnsureDeleted();
    }
}

public class CsvParserRunnerTests : IClassFixture<CsvParserRunnerFixture>, IDisposable
{
    private const string Header = "name | email | age";

    private readonly CsvParserRunnerFixture _fixture;
    private readonly string _folder;

    public CsvParserRunnerTests(CsvParserRunnerFixture fixture)
    {
        _fixture = fixture;
        _folder = Path.Combine(Path.GetTempPath(), $"CsvParserRunnerTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_folder);

        using var context = new PersonDbContext(fixture.Options);
        context.People.ExecuteDelete();
    }

    public void Dispose()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, true);
        }
    }

    [Fact]
    public void ValidFile_PrintsHeaderAndRowsInIdOrder_ExitsZero_EmptyStderr()
    {
        WriteCsv("people.csv", "Ada,ada@example.com,36", "Grace,grace@example.com,85");

        var result = Run(_folder);

        Assert.Equal(0, result.Exit);
        Assert.Equal(
            Lines(Header, "Ada | ada@example.com | 36", "Grace | grace@example.com | 85"),
            result.Stdout);
        Assert.Equal(string.Empty, result.Stderr);
    }

    [Fact]
    public void ValidAndBadRows_WarnOncePerSkippedRow_SaveAndPrintValidRows_ExitsZero()
    {
        WriteCsv("good.csv", "Ada,ada@example.com,36");
        WriteCsv("bad.csv", "Bob,bob@example.com,notanumber", "Cy,cy@example.com,40");

        var result = Run(_folder);

        Assert.Equal(0, result.Exit);
        var stderrLines = SplitLines(result.Stderr);
        var warning = Assert.Single(stderrLines);
        Assert.Matches(@"^warning: .*bad\.csv:\d+: .+", warning);

        var stdoutLines = SplitLines(result.Stdout);
        Assert.Equal(Header, stdoutLines[0]);
        Assert.Equal(3, stdoutLines.Length);
        Assert.Contains("Ada | ada@example.com | 36", stdoutLines);
        Assert.Contains("Cy | cy@example.com | 40", stdoutLines);
        Assert.DoesNotContain(stdoutLines, line => line.StartsWith("Bob "));
    }

    [Fact]
    public void EmptyFolder_PrintsHeaderOnly_ExitsZero()
    {
        var result = Run(_folder);

        Assert.Equal(0, result.Exit);
        Assert.Equal(Lines(Header), result.Stdout);
        Assert.Equal(string.Empty, result.Stderr);
    }

    [Fact]
    public void SecondRunOverSameFolder_PrintsRowsOfBothRuns()
    {
        WriteCsv("people.csv", "Ada,ada@example.com,36");

        Run(_folder);
        var second = Run(_folder);

        Assert.Equal(0, second.Exit);
        Assert.Equal(Lines(Header, "Ada | ada@example.com | 36", "Ada | ada@example.com | 36"), second.Stdout);
    }

    [Fact]
    public void MissingDatabase_IsCreatedAndMigratedByRun()
    {
        var database = $"CsvParserTests_{Guid.NewGuid():N}";
        var connectionString = CsvParserRunnerFixture.ConnectionStringFor(database);
        var options = new DbContextOptionsBuilder<PersonDbContext>().UseSqlServer(connectionString).Options;
        WriteCsv("people.csv", "Ada,ada@example.com,36");

        try
        {
            var stdout = new StringWriter();
            var stderr = new StringWriter();

            var exit = CsvParserRunner.Run(new[] { _folder }, stdout, stderr, connectionString);

            Assert.Equal(0, exit);
            Assert.Equal(Lines(Header, "Ada | ada@example.com | 36"), stdout.ToString());
            using var context = new PersonDbContext(options);
            Assert.True(context.Database.CanConnect());
            Assert.Equal(1, context.People.Count());
        }
        finally
        {
            using var context = new PersonDbContext(options);
            context.Database.EnsureDeleted();
        }
    }

    [Fact]
    public void NoArgument_WritesUsageToStderrOnly_ExitsOne_AndWritesNoRow()
    {
        WriteCsv("people.csv", "Ada,ada@example.com,36");

        AssertUsageFailure(Array.Empty<string>());
    }

    [Fact]
    public void TwoArguments_WritesUsageToStderrOnly_ExitsOne_AndWritesNoRow()
    {
        WriteCsv("people.csv", "Ada,ada@example.com,36");

        AssertUsageFailure(new[] { _folder, _folder });
    }

    [Fact]
    public void MissingFolder_WritesErrorToStderr_ExitsOne()
    {
        var missing = Path.Combine(_folder, "does-not-exist");

        var result = Run(missing);

        Assert.Equal(1, result.Exit);
        Assert.Equal(string.Empty, result.Stdout);
        Assert.StartsWith("error: ", result.Stderr);
    }

    [Fact]
    public void UnreachableDatabase_WritesErrorToStderr_ExitsTwo_WithoutStackTrace()
    {
        WriteCsv("people.csv", "Ada,ada@example.com,36");
        var connectionString =
            @"Server=(localdb)\NoSuchCsvParserTestsInstance;Database=CsvParserTests_Unreachable;Integrated Security=true;TrustServerCertificate=true;Connect Timeout=2";
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        var exit = CsvParserRunner.Run(new[] { _folder }, stdout, stderr, connectionString);

        Assert.Equal(2, exit);
        Assert.StartsWith("error: ", stderr.ToString());
        Assert.DoesNotContain("   at ", stderr.ToString());
        Assert.DoesNotContain("   at ", stdout.ToString());
    }

    private void AssertUsageFailure(string[] args)
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        var exit = CsvParserRunner.Run(args, stdout, stderr, _fixture.ConnectionString);

        Assert.Equal(1, exit);
        Assert.Equal(string.Empty, stdout.ToString());
        Assert.StartsWith("usage: ", stderr.ToString());

        using var context = new PersonDbContext(_fixture.Options);
        Assert.Equal(0, context.People.Count());
    }

    private (int Exit, string Stdout, string Stderr) Run(string folder)
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        var exit = CsvParserRunner.Run(new[] { folder }, stdout, stderr, _fixture.ConnectionString);

        return (exit, stdout.ToString(), stderr.ToString());
    }

    private void WriteCsv(string fileName, params string[] rows)
    {
        var lines = new[] { "name,email,age" }.Concat(rows);
        File.WriteAllLines(Path.Combine(_folder, fileName), lines);
    }

    private static string Lines(params string[] lines) =>
        string.Concat(lines.Select(line => line + Environment.NewLine));

    private static string[] SplitLines(string text) =>
        text.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
}
