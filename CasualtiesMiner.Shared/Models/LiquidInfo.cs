namespace CasualtiesMiner.Shared.Models;

public class Color : IEquatable<Color>
{
    public byte a;
    public byte b;
    public byte g;
    public byte r;

    public bool Equals(Color? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        return a == other.a &&
               b == other.b &&
               g == other.g &&
               r == other.r;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as Color);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(r, g, b, a);
    }

    public static bool operator ==(Color? left, Color? right)
    {
        if (left is null) return right is null;
        return left.Equals(right);
    }

    public static bool operator !=(Color? left, Color? right)
    {
        return !(left == right);
    }
}

public class LiquidInfo : IEquatable<LiquidInfo>
{
    public required Color color;
    public bool healthUsable;
    public bool injectable;
    public float injectionSickness = 1f;
    public bool localeFromItem;
    public required string name;
    public required string[] onDrink;
    public required string[] onHealthUse;
    public List<CraftingQuality> qualities = [];
    public float valuePerLiter;

    public bool Equals(LiquidInfo? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        if (color != other.color ||
            healthUsable != other.healthUsable ||
            injectable != other.injectable ||
            Math.Abs(injectionSickness - other.injectionSickness) > 0.00001 ||
            localeFromItem != other.localeFromItem ||
            name != other.name ||
            Math.Abs(valuePerLiter - other.valuePerLiter) > 0.00001)
            return false;

        var onDrinkEqual = (onDrink == null && other.onDrink == null) ||
                           (onDrink != null && other.onDrink != null && onDrink.SequenceEqual(other.onDrink));

        var onHealthUseEqual = (onHealthUse == null && other.onHealthUse == null) ||
                               (onHealthUse != null && other.onHealthUse != null &&
                                onHealthUse.SequenceEqual(other.onHealthUse));

        var qualitiesEqual = (qualities == null && other.qualities == null) ||
                             (qualities != null && other.qualities != null && qualities.SequenceEqual(other.qualities));

        return onDrinkEqual && onHealthUseEqual && qualitiesEqual;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as LiquidInfo);
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();

        hash.Add(color);
        hash.Add(healthUsable);
        hash.Add(injectable);
        hash.Add(injectionSickness);
        hash.Add(localeFromItem);
        hash.Add(name);
        hash.Add(valuePerLiter);
        hash.Add(GetSequenceHashCode(onDrink));
        hash.Add(GetSequenceHashCode(onHealthUse));
        hash.Add(GetSequenceHashCode(qualities));

        return hash.ToHashCode();
    }

    private static int GetSequenceHashCode<T>(IEnumerable<T>? sequence)
    {
        if (sequence is null) return 0;

        var hash = new HashCode();
        foreach (var item in sequence) hash.Add(item);
        return hash.ToHashCode();
    }

    public static bool operator ==(LiquidInfo? left, LiquidInfo? right)
    {
        if (left is null) return right is null;
        return left.Equals(right);
    }

    public static bool operator !=(LiquidInfo? left, LiquidInfo? right)
    {
        return !(left == right);
    }
}