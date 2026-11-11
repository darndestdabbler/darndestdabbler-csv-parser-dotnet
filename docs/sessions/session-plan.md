# Session plan

> **Purpose:** the numbered sessions this repository runs, in order. The
> first two set the project up; everything after them is the work.
> **Workflow:** Full

---

## Sessions

### Session 1: Author or import the solution plan

1. Register.
2. The brief is the prompt this session was started with, or
   `docs/planning/brief.md`: what the solution is — its purpose, who uses
   it, what it must do and what is out of scope, what success looks like,
   and whether a plan or notes already exist. With neither, ask the person
   who started the session for it and write their answer to that path
   before any work. The plan's substance is theirs: do not search
   neighbouring directories for one, and do not draft one from the folder
   name. Before any module is proposed, ask them how production is split:
   what runs separately in production, and which part may talk to the
   database? Offer the default -- an application tier that calls an API
   tier, and only the API tier talks to the database. Then create — or
   import — `docs/planning/solution-plan.md`: the
   objective a reader can act on; the *Production split* they answered;
   the *Handoff artifacts*, what each separately running part is handed
   over as, with the standard command for its form -- for .NET an IIS site
   package (`dotnet publish`) or a Windows Service (a Worker Service
   published for a service host), for Java a runnable jar or a war (`mvn
   package` writing into `{output}`, which `packaging.pack` requires --
   say through a property the POM's jar or war plugin takes as its
   output directory, `-Dhandoff.dir={output}`) or an image (Spring
   Boot's `build-image`), for the database a
   SQL migration script (`dotnet ef migrations script --idempotent`); the
   real form is produced later, and a tutorial produces the easiest one to
   test; the modules, with what each is
   responsible for, the contract each exposes (what must be true going
   in, what is guaranteed coming out, how it fails), the dependency
   direction, the reason for each cut and the cuts deferred — one module
   is a fine answer, and most small solutions are one; and the phases or
   feature areas with their key deliverables, each scoped to a handful of
   focused AI sessions. A repository that already has
   `docs/planning/project-plan.md` amends that file instead.
3. Where the plan has several modules, say in it how each is a project:
   a `.csproj` listed in the root solution file for .NET, or a Maven module
   listed in the parent `pom.xml`, each reaching a sibling by project
   reference -- a `<ProjectReference>`, or a dependency at
   `${project.version}`. The first numbered session, the skeleton, writes
   the projects.

The framework then verifies the work cross-provider, runs the suites as
the run of record and closes the session. These are the framework's,
not steps to plan.

This session writes the plan and nothing else: it creates, edits or
deletes no code, project, build file, test, `dabbler.yaml` suite or
packaging.

**Creates:** `docs/planning/solution-plan.md`. A later revision is just
another plan session that amends the same file.

### Session 2: Challenge the plan, then break it into numbered sessions

1. Register.
2. Read `docs/planning/solution-plan.md` (or `docs/planning/project-plan.md`
   where that is the file this repository keeps its plan in) and challenge
   its cuts: for each
   module, whether it should exist, whether its contract is a promise
   something can prove, and whether the dependency direction hides the
   decisions most likely to change; decide each deferred cut or say why it
   stays deferred. Record the answers in the plan's rationale.
3. Break the plan into numbered sessions appended to this file. Each
   session is a focused unit of
   work one AI coding session can complete: one
   `### Session <N>: <title>` heading, and its steps as a top-level
   ordered list. Step 1 registers the session and the rest are the
   work: cross-provider verification, the run of record and the close
   are the framework's, never steps to plan. Never
   write a step that says "run the tests" without saying which run it
   means. Order sessions so earlier ones unblock later ones — a module
   before the modules that depend on it — and keep at most ~3 work steps
   per session.

   The first numbered session is the skeleton. For .NET it writes the
   solution file and every project the plan names, with their
   references; for Maven or Gradle, the parent build and every module.
   Either way it declares the test suite in `dabbler.yaml` under
   `testing.suites`, even before any tests exist, with `expensive: true`
   where its run is the run of record and a hang limit in its command --
   `dotnet test --blame-hang-timeout 5m`, or Surefire's
   `forkedProcessTimeoutInSeconds` -- so every later run inherits it,
   and adds the architecture test the plan requires -- where only the
   API tier may reach the database, that rule. Later sessions fill the
   modules in and never re-create what the skeleton made.

   Packaging and releases are sessions of their own. Where the plan
   names *Handoff artifacts*, plan one packaging session -- early, with
   project files only, or later -- that writes the `packaging:` block in
   `dabbler.yaml` and does nothing else; its check is `dabbler packaging
   --rehearse`, which runs the pack and pushes nothing. A release is a
   session with no steps, headed `### Session <N>: Release <version> (release:
   <version>)`; propose its version -- patch, minor or major by what the
   sessions it releases change -- for the person to approve with the plan.

The framework then verifies the work cross-provider, runs the suites as
the run of record and closes the session. These are the framework's,
not steps to plan.

This session writes the plan and nothing else: it creates, edits or
deletes no code, project, build file, test, `dabbler.yaml` suite or
packaging.

**Creates:** the numbered session list the rest of this repository runs,
starting with the skeleton session.

> Do NOT hand-author `sessions.json`. The first `session start`
> bootstraps it from this plan — state files are the writers' job.

### Session 3: Skeleton

1. Register.
2. Write `CsvParser.slnx` and the five SDK-style projects, all `net10.0`:
   `src/PersonModel`, `src/CsvDeserializer`, `src/CsvDatabase`,
   `src/CsvParserApp` (`OutputType=Exe`, with a placeholder `Program.cs`) and
   `tests/CsvParser.Tests` (xUnit, referencing all four). Add the
   `<ProjectReference>` graph from the plan's Dependency Graph section. Add
   no package other than xUnit and the test SDK. Check: `dotnet build
   CsvParser.slnx`.
3. Declare the suite in `dabbler.yaml` under `testing.suites`: name
   `dotnet`, command `dotnet test CsvParser.slnx --blame-hang-timeout 5m`,
   `expensive: true`, covers `src` and `tests`, test root `tests`, test glob
   `*Tests.cs`. No architecture test is added (see the plan's Rationale).
   Check: `dotnet test CsvParser.slnx --blame-hang-timeout 5m` passes with
   no tests.

### Session 4: Domain model

1. Register.
2. Write `public record Person(int Id, string Name, string Email, int Age)`
   in `src/PersonModel/Person.cs`, with no packages and no EF Core
   attributes.
3. Write `tests/CsvParser.Tests/PersonTests.cs` covering value equality, the
   default `Id` of `0` and `ToString`. Check: `dotnet test CsvParser.slnx
   --blame-hang-timeout 5m --filter PersonTests`.

### Session 5: CSV deserialization

1. Register.
2. Write `CsvRecordError`, `CsvReadResult` and `CsvPersonReader.ReadFolder`
   in `src/CsvDeserializer`, using the `CsvHelper` package, as the plan's
   CsvDeserializer contract states.
3. Write `tests/CsvParser.Tests/CsvPersonReaderTests.cs` with temp-folder
   fixtures: valid files, a bad `age`, an empty `name`, missing headers, an
   unparseable row, an empty folder and a folder that does not exist. Check:
   `dotnet test CsvParser.slnx --blame-hang-timeout 5m --filter
   CsvPersonReaderTests`.

### Session 6: Test hardening for the model and the CSV reader

This session adds tests for the work sessions 4 and 5 landed, and writes no
new feature. It was inserted ahead of the database layer, so the two
sessions after it moved up one number; neither had started.

1. Register.
2. Extend `tests/CsvParser.Tests/PersonTests.cs`: inequality when only `Id`
   differs and when only `Email` differs (the session 4 review's open nit);
   a `with` copy changes one property and leaves the original unchanged;
   positional deconstruction returns `Id`, `Name`, `Email` and `Age` in that
   order. Check: `dotnet test CsvParser.slnx --blame-hang-timeout 5m
   --filter PersonTests`.
3. Extend `tests/CsvParser.Tests/CsvPersonReaderTests.cs` with the file and
   header cases of the CsvDeserializer contract:
   - a `*.csv` file in a subfolder is not read, and a file with another
     extension (`notes.txt`) is not read;
   - file order is ordinal, not culture order: `B.csv` is read before
     `a.csv`;
   - columns in another order (`age,name,email`) and an extra column
     (`name,email,age,city`) still map correctly, and the extra column is
     ignored;
   - a file with a header row and no data rows gives no people and no
     errors;
   - a UTF-8 file with a byte-order mark, and non-ASCII names (`Zoë`,
     `山田`), read back exactly;
   - errors from several files come back in file order, and one bad file
     does not stop the files after it.

   Check: `dotnet test CsvParser.slnx --blame-hang-timeout 5m --filter
   CsvPersonReaderTests`.
4. Extend the same file with the row cases:
   - `age` values `30.5`, empty and `99999999999` are each skipped with one
     error; `0` and `-5` are integers and are accepted;
   - a whitespace-only `name` or `email` is skipped with one error;
   - a row with too few fields is skipped with one error, and the rows
     after it are still read;
   - a quoted field holding a comma (`"Smith, Ann"`) is read as one value;
   - two bad rows in one file give two errors, in row order, each with its
     own line number and a non-empty `Message`;
   - two identical valid rows are both returned (nothing de-duplicates).

   Check: `dotnet test CsvParser.slnx --blame-hang-timeout 5m --filter
   CsvPersonReaderTests`.

The contract in `docs/planning/solution-plan.md` decides what each test
expects. Where a new test fails against the code as it stands, fix the
source under `src/` with the smallest change that meets the contract and
name that file in the report; never weaken or drop the test. Quoted fields
that span lines are left out: the contract does not say which line number
such a row reports.

### Session 7: Database layer

1. Register.
2. Write `PersonDbContext`, its `People` mapping, the design-time factory and
   `PersonRepository` in `src/CsvDatabase`, with the EF Core 10.x packages.
   Add the `dotnet-ef` local tool in `.config/dotnet-tools.json` and generate
   the `InitialCreate` migration. Check: `dotnet build CsvParser.slnx`.
3. Write `tests/CsvParser.Tests/PersonRepositoryTests.cs` against a
   per-class LocalDB database (`CsvParserTests_<guid>`, migrated, then
   dropped), with every test starting from an empty `People` table:
   - `SaveAll` returns the count saved, and every saved person has a
     distinct `Id` greater than `0`;
   - `GetAll` from a new context returns the same `Name`, `Email` and `Age`
     values, ordered by `Id`, in the order they were passed to `SaveAll`;
   - `GetAll` on an empty table returns an empty list, and `SaveAll` of an
     empty sequence returns `0` and writes nothing;
   - a second `SaveAll` adds to the first, and saving the same values twice
     stores two rows;
   - a 200-character `Name` and `Email` and non-ASCII text (`Zoë`, `山田`)
     are stored and read back exactly;
   - a batch holding one 201-character `Name` throws `DbUpdateException`
     and leaves the table with the rows it had before (a failed `SaveAll`
     writes no rows).

   Check: `dotnet test CsvParser.slnx --blame-hang-timeout 5m --filter
   PersonRepositoryTests`.
4. Write `tests/CsvParser.Tests/PersonDbContextTests.cs`, and a test file
   named after the design-time factory's source file:
   - the model maps `Person` to table `People`, with `Id` as the key and
     generated on add, `Name` and `Email` required with a maximum length of
     `200`, and `Age` required;
   - after `Migrate()` on a new LocalDB database, `InitialCreate` is among
     the applied migrations, none is pending, and
     `HasPendingModelChanges()` is `false`, so the migration matches the
     model;
   - a second `Migrate()` on the same database changes nothing and does not
     throw;
   - the design-time factory returns a `PersonDbContext` on the SQL Server
     provider whose connection string names `(localdb)\MSSQLLocalDB`; the
     test opens no connection.

   Check: `dotnet test CsvParser.slnx --blame-hang-timeout 5m --filter
   "PersonDbContext"`.

Every test database is `CsvParserTests_<guid>` on LocalDB and is dropped
with `EnsureDeleted()` when its class finishes, including when a test
fails. No in-memory provider stands in for SQL Server.

### Session 8: Console application

1. Register.
2. Write `CsvParserRunner.Run` and `Program.cs` in `src/CsvParserApp`, as
   the plan's CsvParserApp contract states: arguments, the
   `CSV_PARSER_CONNECTION_STRING` variable, migrations, stderr warnings,
   stdout lines and exit codes `0`, `1` and `2`. Check: `dotnet build
   CsvParser.slnx`.
3. Write `tests/CsvParser.Tests/CsvParserRunnerTests.cs` running the whole
   pipeline through `CsvParserRunner.Run` against a temp folder and a
   per-class LocalDB test database, with every test starting from an empty
   `People` table:
   - valid files: exit code `0`, stdout is exactly `name | email | age`
     followed by one `<name> | <email> | <age>` line per person in `Id`
     order, and stderr is empty;
   - valid and bad rows together: one `warning: <file>:<line>: <message>`
     line on stderr per skipped row or file, the valid rows saved and
     printed, exit code `0`;
   - an empty folder: the header line only, exit code `0`;
   - a second run over the same folder prints the rows of both runs;
   - a database that does not exist yet is created and migrated by `Run`
     (this test uses its own database name and drops it);
   - no argument, and two arguments: a usage line on stderr, nothing on
     stdout, exit code `1`, and no row written;
   - a folder that does not exist: an error line on stderr, exit code `1`;
   - a connection string that cannot connect (a LocalDB instance that does
     not exist, with a short `Connect Timeout`): one `error: <message>`
     line on stderr, exit code `2`, and no stack-trace line (`   at `) on
     either writer.

   Check: `dotnet test CsvParser.slnx --blame-hang-timeout 5m --filter
   CsvParserRunnerTests`.
4. Write `tests/CsvParser.Tests/ProgramTests.cs`, which starts the built
   `CsvParserApp` as a process (`dotnet <path to CsvParserApp.dll>`, found
   beside the test assembly) so the parts `Run` does not cover are tested:
   - with `CSV_PARSER_CONNECTION_STRING` set to a LocalDB test database and
     a temp folder of valid files, the process exits with `0` and prints the
     header and the rows, which proves `Program.cs` reads the variable;
   - with no argument, the process exits with `1` and writes to stderr
     only.

   Each started process is given a time limit and is killed when it passes
   it. The default connection string is not run against, so no test touches
   the `CsvParser` database; a test asserts its value where `Program.cs`
   exposes it. Check: `dotnet test CsvParser.slnx --blame-hang-timeout 5m
   --filter ProgramTests`.

Every test database is `CsvParserTests_<guid>` on LocalDB and is dropped
with `EnsureDeleted()` when its class finishes, including when a test
fails.
