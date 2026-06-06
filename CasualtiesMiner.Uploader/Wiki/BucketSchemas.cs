using CasualtiesMiner.Uploader.Data;

namespace CasualtiesMiner.Uploader.Wiki;

/// <summary>
/// Bucket table definitions. Each entry is a page in the <c>Bucket:</c> namespace whose content is the
/// JSON schema described at https://meta.weirdgloop.org/w/Extension:Bucket.
/// </summary>
public static class BucketSchemas
{
    /// <summary>
    /// The lightweight reference table that lists every item and its category.
    /// </summary>
    public const string IndexItemBucket = "Item";
    public const string LiquidContainerBucket = "Item_liquid";
    public const string BatteryBucket = "Item_battery";

    public const string LiquidBucket = "Liquid";

    private const string IndexSchema =
        """
        {
            "item_id":  { "type": "TEXT" },
            "page":     { "type": "PAGE" },
            "category": { "type": "TEXT" },
            "subtype":  { "type": "TEXT" },
            "weight":   { "type": "DOUBLE" },
            "value":    { "type": "INTEGER" },
            "tags":     { "type": "TEXT", "repeated": true },
            "usable":   { "type": "BOOLEAN" },
            "wearable": { "type": "BOOLEAN" },
            "combineable": { "type": "BOOLEAN" },
            "obtainable":  { "type": "BOOLEAN" }
        }
        """;

    /// <summary>
    /// Full item per-category schema. The same definition is used for every category bucket.
    /// </summary>
    private const string CategorySchema =
        """
        {
            "item_id":                     { "type": "TEXT" },
            "page":                        { "type": "PAGE" },
            "weight":                      { "type": "DOUBLE" },
            "value":                       { "type": "INTEGER" },
            "tags":                        { "type": "TEXT", "repeated": true },
            "qualities":                   { "type": "TEXT", "repeated": true },
            "slot_rotation":               { "type": "DOUBLE", "index": false },
            "usable":                      { "type": "BOOLEAN" },
            "usable_on_limb":              { "type": "BOOLEAN" },
            "usable_with_lmb":             { "type": "BOOLEAN" },
            "auto_attack":                 { "type": "BOOLEAN" },
            "only_hold_in_hands":          { "type": "BOOLEAN" },
            "combineable":                 { "type": "BOOLEAN" },
            "destroy_at_zero_condition":   { "type": "BOOLEAN" },
            "scale_weight_with_condition": { "type": "BOOLEAN" },
            "ignore_depression":           { "type": "BOOLEAN" },
            "decay_minutes":               { "type": "DOUBLE" },
            "decay_info":                  { "type": "INTEGER" },
            "rec":                         { "type": "INTEGER" },
            "wearable":                    { "type": "BOOLEAN" },
            "wearable_can_be_held":        { "type": "BOOLEAN" },
            "wear_slot_id":                { "type": "TEXT" },
            "desired_wear_limb":           { "type": "TEXT" },
            "wearable_armor":              { "type": "DOUBLE" },
            "wearable_isolation":          { "type": "DOUBLE" },
            "wear_hit_dur_loss_mult":      { "type": "DOUBLE", "index": false },
            "jump_height_mult_change":     { "type": "DOUBLE", "index": false },
            "wearable_visual_offset":      { "type": "INTEGER", "index": false }
        }
        """;

    private const string LiquidItemSchema =
        """
        {
            "item_id":          { "type": "TEXT" },
            "page":             { "type": "PAGE" },
            "capacity":         { "type": "DOUBLE" },
            "auto_fill":        { "type": "BOOLEAN" },
            "default_contents": { "type": "TEXT", "repeated": true }
        }
        """;

    private const string BatteryItemSchema =
        """
        {
            "item_id":    { "type": "TEXT" },
            "page":       { "type": "PAGE" },
            "max_charge": { "type": "DOUBLE" }
        }
        """;

    /// <summary>
    /// Full liquid schema.
    /// </summary>
    private const string LiquidSchema =
        """
        {
            "liquid_id":          { "type": "TEXT" },
            "color":              { "type": "TEXT", "index": false },
            "value_per_liter":    { "type": "DOUBLE" },
            "health_usable":      { "type": "BOOLEAN" },
            "injectable":         { "type": "BOOLEAN" },
            "locale_from_item":   { "type": "BOOLEAN" },
            "injection_sickness": { "type": "DOUBLE" },
            "qualities":          { "type": "TEXT", "repeated": true },
        }
        """;

    /// <summary>
    /// Bucket name for a given game category (e.g. <c>medical</c> -&gt; <c>Item_medical</c>).
    /// </summary>
    public static string CategoryBucket(string category)
    {
        return "Item_" + category;
    }

    /// <summary>
    /// All schema pages keyed by their bucket name (page title is <c>Bucket:{name}</c>).
    /// </summary>
    public static IReadOnlyList<(string Bucket, string Schema)> All()
    {
        var pages = new List<(string, string)>
        {
            (IndexItemBucket, IndexSchema),
            (LiquidContainerBucket, LiquidItemSchema),
            (BatteryBucket, BatteryItemSchema),
            (LiquidBucket, LiquidSchema),
        };

        foreach (var category in ItemRowMapper.Categories)
        {
            pages.Add((CategoryBucket(category), CategorySchema));
        }

        return pages;
    }
}
