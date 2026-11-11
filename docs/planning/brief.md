# Brief: CSV Parser .NET Dabbler Learning Exercise

This is a .NET 10 port of the
[csv-parser-java brief](https://github.com/darndestdabbler/csv-parser-java/blob/master/docs/planning/brief.md),
with SQL Server through Entity Framework Core in place of H2.

## Purpose

This solution demonstrates the Dabbler AI Orchestration platform through a
practical learning exercise. The primary goal is to exercise
dabbler-ai-orchestration: to see how Dabbler manages and orchestrates
AI-driven development sessions and project structure.

## Who Uses It

This is a learning/sample application. Its database is a sample database for
exercising dabbler-ai-orchestration, and the application never goes into
production.

## What It Must Do

The .NET 10 console application must:

1. Read CSV files from a designated folder
2. Deserialize CSV data into Person objects
3. Persist Person objects to a SQL Server database using Entity Framework Core
   (SQL Server LocalDB where it is reachable)
4. Query and display all records from the database in the console

## Scope

- **In scope:** CSV reading, deserialization to objects, SQL Server
  persistence through Entity Framework Core, console display
- **Out of scope:**
  - Production deployment architecture (there is no production split)
  - Release/packaging for distribution
  - REST API or web interface
  - Advanced error recovery or validation beyond basic parsing
  - Performance optimization for large datasets

## Success Criteria

1. A .NET 10 console application runs and successfully reads CSV files from a
   folder
2. CSV data is correctly deserialized into Person objects
3. Person objects are persisted to a SQL Server database through Entity
   Framework Core
4. All records in the database can be queried and displayed in the console
5. The solution is organized into four distinct projects, mirroring the Java
   solution's four Maven modules
6. The Dabbler orchestration workflow completes successfully with all sessions
