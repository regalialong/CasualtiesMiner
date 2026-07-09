using AssetsTools.NET;

namespace CasualtiesMiner.Dumper.Game;

public readonly record struct PrefabBuildingSnapshot(string PrefabName, AssetTypeValueField Behaviour, string SpriteName);
