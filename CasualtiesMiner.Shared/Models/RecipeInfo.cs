namespace CasualtiesMiner.Shared.Models;

public sealed class RecipeItem : IEquatable<RecipeItem>
{
    public bool destroyItem = true;
    public required string ignoredId;
    public bool isLiquid;
    public float minimumCondition = 0.9f;
    public required CraftingQuality quality;
    public bool specific;
    public required string specificId;

    public bool Equals(RecipeItem? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        return destroyItem == other.destroyItem &&
               ignoredId == other.ignoredId &&
               isLiquid == other.isLiquid &&
               Math.Abs(minimumCondition - other.minimumCondition) < 0.00001 &&
               quality == other.quality &&
               specific == other.specific &&
               specificId == other.specificId;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as RecipeItem);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(destroyItem, ignoredId, isLiquid, minimumCondition, quality, specific, specificId);
    }

    public static bool operator ==(RecipeItem? left, RecipeItem? right)
    {
        if (left is null) return right is null;
        return left.Equals(right);
    }

    public static bool operator !=(RecipeItem? left, RecipeItem? right)
    {
        return !(left == right);
    }
}

public sealed class RecipeResult : IEquatable<RecipeResult>
{
    public int amount = 1;
    public bool dontDrainResultLiquid;
    public required string id;
    public bool isLiquid;
    public float resultCondition = 1f;

    public bool Equals(RecipeResult? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        return amount == other.amount &&
               dontDrainResultLiquid == other.dontDrainResultLiquid &&
               id == other.id &&
               isLiquid == other.isLiquid &&
               Math.Abs(resultCondition - other.resultCondition) < 0.00001;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as RecipeResult);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(amount, dontDrainResultLiquid, id, isLiquid, resultCondition);
    }

    public static bool operator ==(RecipeResult? left, RecipeResult? right)
    {
        if (left is null) return right is null;
        return left.Equals(right);
    }

    public static bool operator !=(RecipeResult? left, RecipeResult? right)
    {
        return !(left == right);
    }
}

public sealed class RecipeInfo : IEquatable<RecipeInfo>
{
    public int category;
    public bool hasMadeBefore;
    public int index;
    public int INT;
    public bool isRepair;
    public required List<RecipeItem> items;
    public required RecipeResult result;
    public bool specialKnown;

    public bool Equals(RecipeInfo? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        if (category != other.category ||
            hasMadeBefore != other.hasMadeBefore ||
            index != other.index ||
            INT != other.INT ||
            isRepair != other.isRepair ||
            result != other.result ||
            specialKnown != other.specialKnown)
            return false;

        var itemsEqual = (items == null && other.items == null) ||
                         (items != null && other.items != null && items.SequenceEqual(other.items));

        return itemsEqual;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as RecipeInfo);
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();

        hash.Add(category);
        hash.Add(hasMadeBefore);
        hash.Add(index);
        hash.Add(INT);
        hash.Add(isRepair);
        hash.Add(result);
        hash.Add(specialKnown);
        hash.Add(GetSequenceHashCode(items));

        return hash.ToHashCode();
    }

    private static int GetSequenceHashCode<T>(IEnumerable<T>? sequence)
    {
        if (sequence is null) return 0;

        var hash = new HashCode();
        foreach (var item in sequence) hash.Add(item);
        return hash.ToHashCode();
    }

    public static bool operator ==(RecipeInfo? left, RecipeInfo? right)
    {
        if (left is null) return right is null;
        return left.Equals(right);
    }

    public static bool operator !=(RecipeInfo? left, RecipeInfo? right)
    {
        return !(left == right);
    }
}