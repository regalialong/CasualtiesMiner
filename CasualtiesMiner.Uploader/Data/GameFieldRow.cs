namespace CasualtiesMiner.Uploader.Data;


/// <summary>
/// Wiki-ready game field row for <c>Module:GameFields/data</c>.
/// </summary>
public class GameFieldRow
{
    public required string GameFieldId { get; init; }
    public required string Value { get; init; }
}
