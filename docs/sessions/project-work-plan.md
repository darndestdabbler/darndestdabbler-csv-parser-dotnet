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
| 5 | CSV deserialization | — | not declared |
| 6 | Database layer | — | not declared |
| 7 | Console application | — | not declared |

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
