using System.Diagnostics;
using CsvDatabase;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CsvParser.Tests;

public sealed class ProgramDatabaseFixture : IDisposable
{
    public ProgramDatabaseFixture()
    {
        ConnectionString =
            $@"Server=(localdb)\MSSQLLocalDB;Database=CsvParserTests_{Guid.NewGuid():N};Integrated Security=true;TrustServerCertificate=true";
        Options = new DbContextOptionsBuilder<PersonDbContext>().UseSqlServer(ConnectionString).Options;

        using var context = new PersonDbContext(Options);
        context.Database.Migrate();
    }

    public string ConnectionString { get; }

    public DbContextOptions<PersonDbContext> Options { get; }

    public void Dispose()
    {
        using var context = new PersonDbContext(Options);
        context.Database.EnsureDeleted();
    }
}

public class ProgramTests : IClassFixture<ProgramDatabaseFixture>, IDisposable
{
    private static readonly TimeSpan ProcessTimeLimit = TimeSpan.FromSeconds(60);

    private readonly ProgramDatabaseFixture _fixture;
    private readonly string _folder;

    public ProgramTests(ProgramDatabaseFixture fixture)
    {
        _fixture = fixture;
        _folder = Path.Combine(Path.GetTempPath(), $"ProgramTests_{Guid.NewGuid():N}");
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
    public void ConnectionStringVariable_IsReadByProgram_ExitsZero_AndPrintsHeaderAndRows()
    {
        File.WriteAllLines(
            Path.Combine(_folder, "people.csv"),
            new[] { "name,email,age", "Ada,ada@example.com,36" });

        var result = RunProgram(
            new[] { _folder },
            new Dictionary<string, string?> { ["CSV_PARSER_CONNECTION_STRING"] = _fixture.ConnectionString });

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("name | email | age", result.Stdout);
        Assert.Contains("Ada | ada@example.com | 36", result.Stdout);
    }

    [Fact]
    public void NoArgument_ExitsOne_WritesToStderrOnly()
    {
        var result = RunProgram(Array.Empty<string>(), new Dictionary<string, string?>());

        Assert.Equal(1, result.ExitCode);
        Assert.Equal(string.Empty, result.Stdout);
        Assert.NotEqual(string.Empty, result.Stderr);
    }

    [Fact]
    public void DefaultConnectionString_HasTheLocalDbCsvParserValue()
    {
        Assert.Equal(
            @"Server=(localdb)\MSSQLLocalDB;Database=CsvParser;Trusted_Connection=True;TrustServerCertificate=True",
            Program.DefaultConnectionString);
    }

    private static ProgramResult RunProgram(string[] args, Dictionary<string, string?> environment)
    {
        var appPath = Path.Combine(AppContext.BaseDirectory, "CsvParserApp.dll");
        var startInfo = new ProcessStartInfo("dotnet")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        startInfo.ArgumentList.Add(appPath);
        foreach (var arg in args)
        {
            startInfo.ArgumentList.Add(arg);
        }

        foreach (var (key, value) in environment)
        {
            startInfo.Environment[key] = value;
        }

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Could not start the CsvParserApp process.");

        try
        {
            var stdoutTask = process.StandardOutput.ReadToEndAsync();
            var stderrTask = process.StandardError.ReadToEndAsync();

            if (!process.WaitForExit(ProcessTimeLimit))
            {
                process.Kill(entireProcessTree: true);
                throw new TimeoutException($"CsvParserApp did not exit within {ProcessTimeLimit}.");
            }

            return new ProgramResult(process.ExitCode, stdoutTask.Result, stderrTask.Result);
        }
        finally
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
    }

    private sealed record ProgramResult(int ExitCode, string Stdout, string Stderr);
}
