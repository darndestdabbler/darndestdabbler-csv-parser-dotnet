using PersonModel;

namespace CsvDeserializer;

public record CsvReadResult(IReadOnlyList<Person> People, IReadOnlyList<CsvRecordError> Errors);
