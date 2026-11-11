# Solution Plan: CSV Parser .NET Dabbler Learning Exercise

Source brief: [`brief.md`](brief.md). This is the .NET port of
[csv-parser-java](https://github.com/darndestdabbler/csv-parser-java/blob/master/docs/planning/solution-plan.md):
the same four-part split and the same resolved decisions, with SQL Server
through Entity Framework Core (EF Core) in place of H2.

## Objective

Build a .NET 10 (`net10.0`) console application that reads every CSV file in
a folder, deserializes the rows into `Person` objects, saves them to a SQL
Server database through EF Core, then reads back every stored person and
prints them to the console. The application is a sample for exercising
dabbler-ai-orchestration, and it never goes into production. Success means
the pipeline works end to end against SQL Server LocalDB, and the code is
split into the four projects below.

## Production Split

There is none. This is a sample with no production deployment. It runs as a
single console process that does everything in order: read CSV → deserialize
→ persist → display. That process is the only thing that talks to the
database, and inside it only the `CsvDatabase` project touches EF Core or SQL
Server.

## Handoff Artifacts

The brief puts release and packaging out of scope, so nothing is published.
If the parts were ever handed over, they would take these forms:

| Part | Form | Command |
|------|------|---------|
| Console application | Framework-dependent `dotnet publish` output folder, run as `CsvParserApp <csv-folder-path>` | `dotnet publish src/CsvParserApp -c Release -o {output}` |
| Database | Idempotent SQL migration script built from the EF Core migrations | `dotnet ef migrations script --idempotent --project src/CsvDatabase -o {output}/schema.sql` |

When the application runs, it applies its own migrations with
`Database.Migrate()`. The script is the same schema, in the form a DBA would
apply by hand.

## Modules

There are four projects, matching the four modules the Java solution's
brief calls for. Each is an SDK-style `.csproj` targeting `net10.0` under
`src/`, listed in the root solution file `CsvParser.slnx`. A project reaches
a sibling only through a `<ProjectReference>`. Tests live in one xUnit
project, `tests/CsvParser.Tests/CsvParser.Tests.csproj`, which is also in
the solution and references all four projects. Test files are named after
the source file they test (`Person.cs` → `PersonTests.cs`).

### 1. PersonModel (`src/PersonModel/PersonModel.csproj`)

**Responsibility:** the `Person` domain type, and nothing else.

**Contract:**
- **In:** nothing (a leaf class library).
- **Out:** `public record Person` with `int Id`, `string Name`,
  `string Email` and `int Age`. `Id` is `0` until the database assigns one.
  As a record, it has value equality and a readable `ToString()`.
- **Failure:** none at run time. A wrong shape is a compile error.

**Dependencies:** none. No NuGet packages, and no EF Core attributes; the
mapping lives in `CsvDatabase`.

**Why it's a separate project:** three projects share the type. Putting it
inside any one of them would force the others to depend on that project's
CSV or EF Core packages.

**Deferred:** extra fields and per-field validation attributes. The brief
asks for basic parsing only.

### 2. CsvDeserializer (`src/CsvDeserializer/CsvDeserializer.csproj`)

**Responsibility:** turn the CSV files in a folder into `Person` objects.

**Contract:** `CsvPersonReader.ReadFolder(string folderPath)` returns
`CsvReadResult(IReadOnlyList<Person> People, IReadOnlyList<CsvRecordError> Errors)`.
- **In:** a path to an existing folder. Every `*.csv` file in it, not
  recursive, is read in ordinal file-name order. Each file is UTF-8 with a
  header row.
- **Out:** `People` holds every valid row, in file order and then row order,
  with `Id = 0`. `Errors` holds one `CsvRecordError(string FilePath,
  int LineNumber, string Message)` for each thing that was skipped:
  - a row whose `name` or `email` is empty, or whose `age` is not an
    integer, is skipped with one error;
  - a row CsvHelper cannot parse is skipped with one error;
  - a file missing a required header (matched case-insensitively) produces
    one error with `LineNumber = 1` and contributes no rows.

  Valid rows are returned even when the same file has bad ones.
- **Failure:** a folder that doesn't exist throws
  `DirectoryNotFoundException`. Malformed data never throws.

**Dependencies:** `PersonModel` (by `<ProjectReference>`) and the `CsvHelper`
NuGet package.

**Why it's a separate project:** parsing rules don't depend on persistence
or display, so they can be tested with temp folders and no database. The
choice of CSV library is kept inside this project.

**Deferred:** returning errors instead of logging them means no logging
framework is needed. Configurable delimiters and encodings, and recursive
folder scans, are out of scope.

### 3. CsvDatabase (`src/CsvDatabase/CsvDatabase.csproj`)

**Responsibility:** all EF Core and SQL Server code: the schema, its
migrations, saving people and reading them back.

**Contract:**
- `PersonDbContext(DbContextOptions<PersonDbContext>)` with
  `DbSet<Person> People`. It maps `Person` to table `People`: `Id` is an
  identity primary key, `Name` and `Email` are required `nvarchar(200)`,
  and `Age` is a required `int`. An `InitialCreate` migration creates the
  table.
- `PersonRepository(PersonDbContext)`:
  - `SaveAll(IEnumerable<Person>)` inserts every person in one
    `SaveChanges` call (one transaction), then returns the count saved.
    Each saved person's `Id` is assigned by the database.
  - `GetAll()` returns every stored person, ordered by `Id`, which is
    insertion order.
- **In:** a context whose connection string points at a reachable SQL
  Server, and whose migrations are applied.
- **Out:** rows in `People`. `GetAll()` reflects every completed `SaveAll`.
- **Failure:** EF Core and SQL Server exceptions (`DbUpdateException`,
  `SqlException`) propagate unchanged. A failed `SaveAll` writes no rows.
- A design-time `IDesignTimeDbContextFactory<PersonDbContext>` points at
  LocalDB, so `dotnet ef` works with this project as its own startup
  project.

**Dependencies:** `PersonModel` (by `<ProjectReference>`), plus the
`Microsoft.EntityFrameworkCore.SqlServer` and
`Microsoft.EntityFrameworkCore.Design` (`PrivateAssets="all"`) packages at
10.x. `dotnet-ef` is a local tool in `.config/dotnet-tools.json`.

**Why it's a separate project:** it confines the provider choice. Moving
from LocalDB to full SQL Server is only a connection-string change, and
moving to another provider touches only this project.

**Deferred:** de-duplicating persons across runs (a second run inserts the
same rows again, as in the Java version), a generic repository, and async
APIs. A sequential console app doesn't need them.

### 4. CsvParserApp (`src/CsvParserApp/CsvParserApp.csproj`, `OutputType=Exe`)

**Responsibility:** the entry point. It wires the other three projects
together in order.

**Contract:** `Program.cs` calls
`CsvParserRunner.Run(string[] args, TextWriter stdout, TextWriter stderr, string connectionString)`,
which returns the exit code.
- **In:** exactly one argument, the CSV folder path. The connection string
  comes from the environment variable `CSV_PARSER_CONNECTION_STRING`, and
  defaults to
  `Server=(localdb)\MSSQLLocalDB;Database=CsvParser;Trusted_Connection=True;TrustServerCertificate=True`.
- **Out:** the steps are:
  1. apply migrations;
  2. read the folder;
  3. write each `CsvRecordError` to stderr as
     `warning: <file>:<line>: <message>`;
  4. save the valid people;
  5. print `name | email | age` to stdout, then one
     `<name> | <email> | <age>` line per stored person, ordered by `Id`;
  6. exit with `0`.
- **Failure:** a missing or extra argument, or a folder that doesn't exist,
  writes a usage or error line to stderr and exits with `1`. A database
  failure writes `error: <message>` to stderr and exits with `2`. No stack
  trace reaches the console.

**Dependencies:** `PersonModel`, `CsvDeserializer` and `CsvDatabase` (by
`<ProjectReference>`). It wires them by hand, with no host or DI container.

**Why it's a separate project:** it keeps the executable concerns
(arguments, environment, exit codes) out of the libraries.
`CsvParserRunner` takes its writers and connection string as parameters,
so it can be tested end to end without starting a process.

**Deferred:** `appsettings.json` and the generic host. One environment
variable is enough configuration for a sample.

## Dependency Graph

```
CsvParserApp (console exe)
  ├─ PersonModel
  ├─ CsvDeserializer ── PersonModel
  └─ CsvDatabase ────── PersonModel
CsvParser.Tests ── all four
```

Only `CsvParserApp` depends on more than one library, and nothing depends on
it. The `<ProjectReference>` items are the dependency rule: if
`CsvDeserializer` and `CsvDatabase` don't reference each other, they can't
call each other, so no separate architecture test is needed.

## Resolved Decisions

These are carried over from the Java plan.

**(a) CSV column mapping:** the headers `name`, `email` and `age`
(case-insensitive) map to `Person.Name`, `Person.Email` and `Person.Age`
(an `int`). Other columns are ignored. Malformed rows are handled as in
CsvDeserializer's contract above.

**(b) Database location:** the application uses LocalDB database
`CsvParser` by default, overridable with `CSV_PARSER_CONNECTION_STRING`.
Tests use LocalDB too: each test class creates a uniquely named database
(`CsvParserTests_<guid>`), applies the migrations with `Migrate()`, and
drops it with `EnsureDeleted()` when it finishes. No in-memory provider
stands in for SQL Server. LocalDB (`MSSQLLocalDB`) is installed on the
development machine.

**(c) Console output format:** a header line, then one line per person,
with fields separated by ` | `:

```
name | email | age
Alice | alice@example.com | 30
Bob | bob@example.com | 25
```

**(d) Toolchain:** every project targets `net10.0`. The machine has the
.NET 11 SDK, which builds `net10.0`, so no `global.json` pins an SDK. The
EF Core packages stay on the 10.x line to match the target.

## Phases

Each phase is one focused session after session 2 (the challenge and
breakdown session).

| Session | Phase | Key deliverables |
|---------|-------|------------------|
| 3 | Skeleton | `CsvParser.slnx`; the four `src/` projects and `tests/CsvParser.Tests`, all `net10.0`, with the `<ProjectReference>` graph above and empty placeholders; a `dabbler.yaml` test suite running `dotnet test CsvParser.slnx`; the build and an empty test run pass |
| 4 | Domain model | `Person` record; `PersonTests` covering value equality, the default `Id` and `ToString` |
| 5 | CSV deserialization | `CsvPersonReader`, `CsvReadResult`, `CsvRecordError` using CsvHelper; `CsvPersonReaderTests` with temp-folder fixtures for valid files, a bad `age`, missing headers, an empty folder and a folder that doesn't exist |
| 6 | Database layer | `PersonDbContext`, its mapping, the `InitialCreate` migration, the design-time factory, `PersonRepository`, and the `dotnet-ef` local tool; `PersonRepositoryTests` against a per-class LocalDB database (save and read back, `Id` order, an empty table) |
| 7 | Console application | `CsvParserRunner` and `Program.cs`; `CsvParserRunnerTests` running the whole pipeline against a temp folder and a LocalDB test database, checking stdout lines, stderr warnings and exit codes `0`, `1` and `2` |

No release or packaging session is planned, because the brief puts
distribution out of scope. The handoff commands above are documentation
only.
