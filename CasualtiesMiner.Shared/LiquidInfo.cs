namespace CasualitiesMiner.Shared;

public class Color
{
    public byte a;
    public byte b;
    public byte g;
    public byte r;
}

public class LiquidInfo
{
    public required Color color;

    public bool healthUsable;

    public bool injectable;

    public float injectionSickness = 1f;

    public bool localeFromItem;
    public required string name;

    public required string[] onDrink;

    public required string[] onHealthUse;

    public List<CraftingQuality> qualities = new();

    public float valuePerLiter;
}