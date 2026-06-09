namespace CasualtiesMiner.Uploader.Data;

/// <summary>
/// Wiki-ready moodle row for <c>Module:Moodle/data</c>.
/// </summary>
public sealed record MoodleRow
{
    public required string LocaleId { get; init; }
    public required string Icon { get; init; }
    public int? Intensity { get; init; }
    public string? IntensityExpr { get; init; }
    public required bool Critical { get; init; }
    public string? CriticalExpr { get; init; }
    public required bool ChippedOnly { get; init; }
    public string? DescLocaleKey { get; init; }
}
