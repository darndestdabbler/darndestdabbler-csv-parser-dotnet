using CsvParserApp;

var connectionString = Environment.GetEnvironmentVariable("CSV_PARSER_CONNECTION_STRING");
if (string.IsNullOrEmpty(connectionString))
{
    connectionString = Program.DefaultConnectionString;
}

return CsvParserRunner.Run(args, Console.Out, Console.Error, connectionString);

public partial class Program
{
    public const string DefaultConnectionString =
        @"Server=(localdb)\MSSQLLocalDB;Database=CsvParser;Trusted_Connection=True;TrustServerCertificate=True";
}
