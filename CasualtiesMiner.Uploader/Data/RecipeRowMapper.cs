using CasualtiesMiner.Shared.Models;

namespace CasualtiesMiner.Uploader.Data;

public static class RecipeRowMapper
{
    public static RecipeRow Map(Recipe recipe)
    {
        return new RecipeRow
        {
            RecipeItemId = recipe.isRepair ? string.Concat("repaired", recipe.result.id) : recipe.result.id,
            Items = recipe.items,
            Result = recipe.result,
            Category = recipe.category,
            Intelligence = recipe.INT,
            Index = recipe.index,
            HasMadeBefore = recipe.hasMadeBefore,
            IsRepair = recipe.isRepair
        };
    }
}
