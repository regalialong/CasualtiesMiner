namespace CasualtiesMiner.Uploader.Data;

/// <summary>
/// Wiki-ready body field metadata for <c>Module:BodyField/data</c> / Bucket <c>bodyfield</c>.
/// Timer rows reference scalar constants via <see cref="GameFieldIds"/>.
/// </summary>
public sealed record BodyFieldRow
{
    public required string BodyFieldId { get; init; }
    public required string Label { get; init; }
    public required string Kind { get; init; }
    public string? HealSpeedFieldId { get; init; }
    public string? MaxTimerFieldId { get; init; }
    public string? IntensityScaleFieldId { get; init; }
    public string? SplintMultiplierFieldId { get; init; }
}
