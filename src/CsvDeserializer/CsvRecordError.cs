namespace CsvDeserializer;

public record CsvRecordError(string FilePath, int LineNumber, string Message);
