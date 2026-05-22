namespace CasualtiesMiner.Shared.Models;

public class CraftingQuality
{
    public required float amount;
    public required string id;
}

public class ItemInfo
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
}