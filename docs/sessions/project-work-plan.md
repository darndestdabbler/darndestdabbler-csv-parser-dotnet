# Project work plan — sessions

**Written by `dabbler` as a fold of `activity-log.json`.**
Hand edits are overwritten by the next append. The record is the log;
this page is one view of it.

---

## Sessions

The numbered sessions are declared from `session-plan.md`; each one's task is what its own plan step declared.

| # | Session | Releasable | Declared |
| ---: | --- | --- | --- |
| 1 | Author or import the solution plan | no | 2026-11-11 |
| 2 | Challenge the plan, then break it into numbered sessions | no | 2026-11-11 |
| 3 | Skeleton | no | 2026-11-11 |
| 4 | Domain model | no | 2026-11-11 |
| 5 | CSV deserialization | no | 2026-11-11 |
| 6 | Test hardening for the model and the CSV reader | no | 2026-11-11 |
| 7 | Database layer | no | 2026-11-11 |
| 8 | Console application | — | not declared |

### Session 1 — Author or import the solution plan

**Releasable: no.**

Author the solution plan for csv-parser-dotnet, a .NET 10 port of the csv-parser-java learning exercise. First record the person's brief in docs/planning/brief.md: the Java project's brief (a console app that reads CSV files from a folder, deserializes them into Person objects, persists them and displays every stored record), restated for .NET 10, SQL Server through Entity Framework Core instead of H2, LocalDB as the reachable server, and a sample that never goes into production. Then write docs/planning/solution-plan.md: the objective; a Production split stating there is none (one console process, the only part that talks to the database); the Handoff artifacts (the console app as `dotnet publish` output, the database as an idempotent EF Core migration script); the four modules as four .csproj projects in the root solution file reaching each other by <ProjectReference> (PersonModel, CsvDeserializer, CsvDatabase, CsvParserApp), each with responsibility, contract, dependency direction, the reason for the cut and the cuts deferred; the resolved decisions carried over from the Java plan (Person fields and CSV headers, database location, console output format); and the phases from the skeleton (session 3) to the console app (session 7).

### Session 2 — Challenge the plan, then break it into numbered sessions

**Releasable: no.**

Challenge the cuts in docs/planning/solution-plan.md (each module's existence, provability of its contract, dependency direction, deferred cuts) and record the answers as a Rationale section in that plan; then append the numbered work sessions (skeleton first, then model, CSV, database, console) to docs/sessions/session-plan.md. Writes plan documents only: no code, project, build file, test, dabbler.yaml suite or packaging.

### Session 3 — Skeleton

**Releasable: no.**

Lay down the solution skeleton: CsvParser.slnx and five SDK-style net10.0 projects (src/PersonModel, src/CsvDeserializer, src/CsvDatabase, src/CsvParserApp as an Exe with a placeholder Program.cs, tests/CsvParser.Tests with xUnit and the test SDK referencing all four), wired with the ProjectReference graph from the plan (CsvDeserializer and CsvDatabase reference PersonModel; CsvParserApp references all three libraries). Then declare the 'dotnet' test suite in dabbler.yaml under testing.suites. No packages beyond xUnit and the test SDK.

### Session 4 — Domain model

**Releasable: no.**

Add the Person domain record in src/PersonModel/Person.cs (public record Person(int Id, string Name, string Email, int Age), no packages, no EF Core attributes) and cover it with tests in tests/CsvParser.Tests/PersonTests.cs for value equality, the default Id of 0 and ToString. Both projects already exist and reference each other, so no project files change.

### Session 5 — CSV deserialization

**Releasable: no.**

Session 5 adds CSV deserialization to the CsvDeserializer project. It writes the two result types, CsvRecordError(FilePath, LineNumber, Message) and CsvReadResult(People, Errors), and the static reader CsvPersonReader.ReadFolder(folderPath), which reads every *.csv file directly in the folder in ordinal file-name order, maps the name, email and age columns (matched case-insensitively) to Person rows with Id = 0, and skips bad rows with one CsvRecordError each. The CsvHelper NuGet package is added to CsvDeserializer.csproj, and that project keeps its ProjectReference to PersonModel. CsvPersonReaderTests covers valid files, a bad age, an empty name, missing headers, an unparseable row, an empty folder and a missing folder, using temp-folder fixtures. The session follows the CsvDeserializer contract in docs/planning/solution-plan.md and the Session 5 section of docs/sessions/session-plan.md.

### Session 6 — Test hardening for the model and the CSV reader

**Releasable: no.**

Session 6 adds tests only for the contracts that sessions 4 and 5 delivered; it writes no new feature. Three steps: first, PersonTests gains an inequality test for a change in Id alone and one for a change in Email alone, a 'with' copy test that changes one property and leaves the original unchanged, and a positional deconstruction test that returns Id, Name, Email and Age in that order. Second, CsvPersonReaderTests gains the file and header cases of the CsvDeserializer contract: a .csv in a subfolder and a .txt file are not read; file order is ordinal (B.csv before a.csv); columns in another order and an extra ignored column still map; a header-only file gives no people and no errors; a UTF-8 byte-order-mark file and non-ASCII names read back exactly; errors from several files come back in file order and a bad file does not stop the ones after it. Third, the same test file gains the row cases: age 30.5, empty and 99999999999 are each skipped with one error while 0 and -5 are accepted; a whitespace-only name or email is skipped with one error; a row with too few fields is skipped with one error and the rows after it are still read; a quoted field holding a comma is one value; two bad rows give two errors in row order, each with its line number and a non-empty Message; two identical valid rows are both returned. The contract in docs/planning/solution-plan.md decides each expectation. Where a new test fails against the current source, the step makes the smallest source change under src/CsvDeserializer that meets the contract and names that file in its report; no test is weakened or dropped.

### Session 7 — Database layer

**Releasable: no.**

Add the database layer to src/CsvDatabase: a PersonDbContext mapping Person to a People table (Id key and generated on add, Name and Email required with max length 200, Age required), a design-time factory that returns a PersonDbContext on SQL Server against (localdb)\MSSQLLocalDB, the EF Core 10.0.x packages, the dotnet-ef local tool in .config/dotnet-tools.json, and an InitialCreate migration; a PersonRepository with SaveAll(IEnumerable<Person>) returning the count saved and GetAll() returning the stored people ordered by Id; and tests against per-class LocalDB databases named CsvParserTests_<guid>, migrated then dropped with EnsureDeleted() when the class finishes, covering the repository behaviour and the context, migration and factory behaviour the session plan lists. Each step is one concern and is checked by building the solution or running the filtered tests it names. No code outside src/CsvDatabase and tests/CsvParser.Tests changes, and the existing projects and their references stay as they are.
