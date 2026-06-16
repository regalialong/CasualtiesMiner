using CasualtiesMiner.Shared.Models;

namespace CasualtiesMiner.Uploader.Data;

/// <summary>
/// A flattened, wiki-ready representation of a single recipe entity. All values are already converted to the
/// shape expected by the Bucket schema; the Lua/wikitext generators only serialize this object.
/// </summary>
public sealed record RecipeRow
{
    public required string RecipeItemId { get; init; }

    public required List<RecipeItem> Items { get; init; }
    public required RecipeResult Result { get; init; }
    public required RecipeCategory Category { get; init; }

    public required int Intelligence { get; init; }
    public required int Index { get; init; }

    public required bool HasMadeBefore { get; init; }
    public required bool IsRepair { get; init; }
}
