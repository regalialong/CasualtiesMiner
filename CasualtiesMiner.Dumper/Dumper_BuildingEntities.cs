using CasualtiesMiner.Dumper.Game;
using CasualtiesMiner.Dumper.Mappers;
using CasualtiesMiner.Shared.Models;

namespace CasualtiesMiner.Dumper;

public sealed partial class Dumper
{
    public static BuildingEntity[] DumpBuildingEntities(AssetsParser assetsParser)
    {
        var byId = new Dictionary<string, BuildingEntity>(StringComparer.Ordinal);
        var spriteById = new Dictionary<string, string>(StringComparer.Ordinal);
        var prefabsById = new Dictionary<string, List<string>>(StringComparer.Ordinal);

        foreach (var snapshot in assetsParser.ExtractPrefabFields("BuildingEntity"))
        {
            var entity = BehaviourMapper.MapBuildingEntity(snapshot.Behaviour);

            if (string.IsNullOrWhiteSpace(entity.id))
                continue;

            prefabsById.TryAdd(entity.id, []);
            prefabsById[entity.id].Add(snapshot.PrefabName);

            if (!byId.TryGetValue(entity.id, out var existing))
            {
                entity.spriteName = snapshot.SpriteName;
                byId[entity.id] = entity;

                if (!string.IsNullOrWhiteSpace(snapshot.SpriteName))
                {
                    spriteById[entity.id] = snapshot.SpriteName;
                }

                continue;
            }

            if (!existing.Equals(entity))
            {
                Console.WriteLine(
                    $"Warning: BuildingEntity '{entity.id}' differs between prefabs " +
                    $"'{prefabsById[entity.id][0]}' and '{snapshot.PrefabName}'. Keeping the first instance.");
            }

            if (string.IsNullOrWhiteSpace(existing.spriteName) && !string.IsNullOrWhiteSpace(snapshot.SpriteName))
            {
                existing.spriteName = snapshot.SpriteName;
                spriteById[entity.id] = snapshot.SpriteName;
            }
            else if (!string.IsNullOrWhiteSpace(snapshot.SpriteName)
                     && !string.IsNullOrWhiteSpace(existing.spriteName)
                     && !string.Equals(existing.spriteName, snapshot.SpriteName, StringComparison.Ordinal))
            {
                Console.WriteLine(
                    $"Warning: BuildingEntity '{entity.id}' sprite mismatch: prefab '{snapshot.PrefabName}' " +
                    $"has '{snapshot.SpriteName}', already have '{existing.spriteName}'.");
            }
        }

        return [.. byId.Values.OrderBy(x => x.id, StringComparer.Ordinal)];
    }
}
