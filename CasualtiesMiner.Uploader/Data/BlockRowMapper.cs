using CasualtiesMiner.Shared.Models;

namespace CasualtiesMiner.Uploader.Data;

internal sealed class BlockRowMapper
{
    internal static BlockRow Map(BlockInfo info)
    {
        return new BlockRow
        {
            Name = info.name,
            Hitsound = info.hitsound,
            Stepsound = info.stepsound,
            Health = (double)(decimal)info.health,
            Toxicity = (double)(decimal)info.toxicity,
            NoVariation = info.noVariation,
            Metallic = info.metallic,
            Slippery = info.slippery,
            SleepQuality = info.sleep
        };
    }
}
