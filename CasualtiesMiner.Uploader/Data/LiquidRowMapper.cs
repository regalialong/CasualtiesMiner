using CasualtiesMiner.Shared.Models;
using CasualtiesMiner.Uploader.Wiki;

namespace CasualtiesMiner.Uploader.Data;

/// <summary>
/// Converts dumped <see cref="LiquidRow"/> instances into wiki-ready <see cref="LiquidRow"/>s.
/// </summary>
public sealed class LiquidRowMapper
{
    public static LiquidRow Map(LiquidType item)
    {
        var liquidId = string.IsNullOrWhiteSpace(item.registryId) ? item.localeName : item.registryId;
        var localeName = string.IsNullOrWhiteSpace(item.localeName) ? liquidId : item.localeName;

        return new LiquidRow
        {
            LiquidId = liquidId,
            LocaleName = localeName,
            Color = item.color.ToHex(),
            ValuePerLiter = (double)(decimal)item.valuePerLiter,
            InjectionSickness = (double)(decimal)item.injectionSickness,
            HealthUsable = item.healthUsable,
            Injectable = item.injectable,
            LocaleFromItem = item.localeFromItem,
            Qualities = MapQualities(item.qualities)
        };
    }

    private static string[] MapQualities(List<CraftingQuality>? qualities)
    {
        if (qualities is null || qualities.Count == 0)
        {
            return [];
        }

        return qualities
            .Where(q => !string.IsNullOrWhiteSpace(q.id))
            .Select(q => Math.Abs(q.amount - 1f) < 0.00001f ? q.id : $"{q.id}:{Format(q.amount)}")
            .ToArray();
    }

    private static string Format(float value)
    {
        return value.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }
}
