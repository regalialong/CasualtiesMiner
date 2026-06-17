namespace CasualtiesMiner.Uploader.Data;

public sealed record RecipeItemRow
{
    public required string RecipeId { get; init; }
    public required string SpecificId { get; init; }
    public required string IgnoredId { get; init; }

    public required IReadOnlyList<string> Quality { get; init; } = [];

    public required double MinimumCondition { get; init; }

    public required bool Specific { get; init; }
    public required bool DestroyItem { get; init; }
    public required bool IsLiquid { get; init; }
}
