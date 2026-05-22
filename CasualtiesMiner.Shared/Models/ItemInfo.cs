namespace CasualtiesMiner.Shared.Models;

public class CraftingQuality : IEquatable<CraftingQuality>
{
    public required float amount;
    public required string id;

    public bool Equals(CraftingQuality? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        return Math.Abs(amount - other.amount) < 0.00001 && id == other.id;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as CraftingQuality);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(amount, id);
    }

    public static bool operator ==(CraftingQuality? left, CraftingQuality? right)
    {
        if (left is null) return right is null;
        return left.Equals(right);
    }

    public static bool operator !=(CraftingQuality? left, CraftingQuality? right)
    {
        return !(left == right);
    }
}

public class ItemInfo : IEquatable<ItemInfo>
{
    public required bool autoAttack;
    public required string category;
    public required bool combineable;
    public required byte decayInfo;
    public required float decayMinutes;
    public required string desiredWearLimb;
    public required bool destroyAtZeroCondition;
    public required bool ignoreDepression;
    public required float jumpHeightMultChange;
    public required string name;
    public required bool onlyHoldInHands;
    public required List<CraftingQuality> qualities;
    public required int rec;
    public required float rotSpeed;
    public required bool scaleWeightWithCondition;
    public required float slotRotation;
    public required string[] tags = [];
    public required bool usable;
    public required bool usableOnLimb;
    public required bool usableWithLMB;
    public required string[] useAction;
    public required string[] useLimbAction;
    public required int value;
    public required bool wearable;
    public required float wearableArmor;
    public required bool wearableCanBeHeld;
    public required float wearableHitDurabilityLossMultiplier;
    public required float wearableIsolation;
    public required int wearableVisualOffset = 5;
    public required string wearSlotId;
    public required float weight;

    public bool Equals(ItemInfo? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        if (autoAttack != other.autoAttack ||
            category != other.category ||
            combineable != other.combineable ||
            decayInfo != other.decayInfo ||
            Math.Abs(decayMinutes - other.decayMinutes) > 0.00001 ||
            desiredWearLimb != other.desiredWearLimb ||
            destroyAtZeroCondition != other.destroyAtZeroCondition ||
            ignoreDepression != other.ignoreDepression ||
            Math.Abs(jumpHeightMultChange - other.jumpHeightMultChange) > 0.00001 ||
            name != other.name ||
            onlyHoldInHands != other.onlyHoldInHands ||
            rec != other.rec ||
            Math.Abs(rotSpeed - other.rotSpeed) > 0.00001 ||
            scaleWeightWithCondition != other.scaleWeightWithCondition ||
            Math.Abs(slotRotation - other.slotRotation) > 0.00001 ||
            usable != other.usable ||
            usableOnLimb != other.usableOnLimb ||
            usableWithLMB != other.usableWithLMB ||
            value != other.value ||
            wearable != other.wearable ||
            Math.Abs(wearableArmor - other.wearableArmor) > 0.00001 ||
            wearableCanBeHeld != other.wearableCanBeHeld ||
            Math.Abs(wearableHitDurabilityLossMultiplier - other.wearableHitDurabilityLossMultiplier) > 0.00001 ||
            Math.Abs(wearableIsolation - other.wearableIsolation) > 0.00001 ||
            wearableVisualOffset != other.wearableVisualOffset ||
            wearSlotId != other.wearSlotId ||
            Math.Abs(weight - other.weight) > 0.00001)
            return false;

        var qualitiesEqual = (qualities == null && other.qualities == null) ||
                             (qualities != null && other.qualities != null && qualities.SequenceEqual(other.qualities));

        var tagsEqual = (tags == null && other.tags == null) ||
                        (tags != null && other.tags != null && tags.SequenceEqual(other.tags));

        var useActionEqual = (useAction == null && other.useAction == null) ||
                             (useAction != null && other.useAction != null && useAction.SequenceEqual(other.useAction));

        var useLimbActionEqual = (useLimbAction == null && other.useLimbAction == null) ||
                                 (useLimbAction != null && other.useLimbAction != null &&
                                  useLimbAction.SequenceEqual(other.useLimbAction));

        return qualitiesEqual && tagsEqual && useActionEqual && useLimbActionEqual;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as ItemInfo);
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();

        hash.Add(autoAttack);
        hash.Add(category);
        hash.Add(combineable);
        hash.Add(decayInfo);
        hash.Add(decayMinutes);
        hash.Add(desiredWearLimb);
        hash.Add(destroyAtZeroCondition);
        hash.Add(ignoreDepression);
        hash.Add(jumpHeightMultChange);
        hash.Add(name);
        hash.Add(onlyHoldInHands);
        hash.Add(rec);
        hash.Add(rotSpeed);
        hash.Add(scaleWeightWithCondition);
        hash.Add(slotRotation);
        hash.Add(usable);
        hash.Add(usableOnLimb);
        hash.Add(usableWithLMB);
        hash.Add(value);
        hash.Add(wearable);
        hash.Add(wearableArmor);
        hash.Add(wearableCanBeHeld);
        hash.Add(wearableHitDurabilityLossMultiplier);
        hash.Add(wearableIsolation);
        hash.Add(wearableVisualOffset);
        hash.Add(wearSlotId);
        hash.Add(weight);
        hash.Add(GetSequenceHashCode(qualities));
        hash.Add(GetSequenceHashCode(tags));
        hash.Add(GetSequenceHashCode(useAction));
        hash.Add(GetSequenceHashCode(useLimbAction));

        return hash.ToHashCode();
    }

    private static int GetSequenceHashCode<T>(IEnumerable<T>? sequence)
    {
        if (sequence is null) return 0;

        var hash = new HashCode();
        foreach (var item in sequence) hash.Add(item);
        return hash.ToHashCode();
    }

    public static bool operator ==(ItemInfo? left, ItemInfo? right)
    {
        if (left is null) return right is null;
        return left.Equals(right);
    }

    public static bool operator !=(ItemInfo? left, ItemInfo? right)
    {
        return !(left == right);
    }
}