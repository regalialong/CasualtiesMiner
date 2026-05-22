namespace CasualitiesMiner.Shared;

public class RecipeItem
{
    public bool destroyItem = true;

    public required string ignoredId;

    public bool isLiquid;

    public float minimumCondition = 0.9f;

    public required CraftingQuality quality;
    public bool specific;

    public required string specificId;
}

public class RecipeResult
{
    public int amount = 1;
    public bool dontDrainResultLiquid;
    public required string id;
    public bool isLiquid;
    public float resultCondition = 1f;
}

public class RecipeInfo
{
    public int category;
    public bool hasMadeBefore;
    public int index;
    public int INT;
    public bool isRepair;
    public required List<RecipeItem> items;
    public required RecipeResult result;

    public bool specialKnown;
}