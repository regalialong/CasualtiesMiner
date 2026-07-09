using AssetsTools.NET;
using AssetsTools.NET.Extra;

namespace CasualtiesMiner.Dumper.Game;

public sealed class AssetsParser : IDisposable
{
    private List<(string PrefabName, AssetsFileInstance File, AssetTypeValueField GoBase)> prefabs;
    private List<KeyValuePair<string, (AssetsFileInstance File, AssetFileInfo Info)>> spriteIndex;

    public AssetsFileInstance? ResourcesAssets { get; private set; }
    public AssetsFileInstance? GlobalGameManagers { get; private set; }
    public AssetFileInfo? ResourcesManager { get; private set; }

    public string GamePath { get; private set; }

    public AssetsManager Manager { get; private set; }

    public AssetsParser(string gameDataPath)
    {
        GamePath = gameDataPath;

        Manager = new AssetsManager
        {
            MonoTempGenerator = new MonoCecilTempGenerator(Path.Combine(gameDataPath, "Managed"))
        };
    }

    private static Stream OpenEmbeddedClassPackage()
    {
        var assembly = typeof(AssetsParser).Assembly;
        var resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(static n => n.EndsWith("lz4.tpk", StringComparison.OrdinalIgnoreCase));

        return resourceName is null
            ? throw new InvalidOperationException(
                "Embedded class package not found. Add Assets/lz4.tpk as EmbeddedResource in CasualtiesMiner.Dumper.csproj.")
            : assembly.GetManifestResourceStream(resourceName)
                ?? throw new InvalidOperationException($"Failed to open embedded resource '{resourceName}'.");
    }

    public AssetsFileInstance LoadResources()
    {
        using var classPackage = OpenEmbeddedClassPackage();
        Manager.LoadClassPackage(classPackage);

        ResourcesAssets = Manager.LoadAssetsFile(Path.Combine(GamePath, "resources.assets"), loadDeps: true);
        Manager.LoadClassDatabaseFromPackage(ResourcesAssets.file.Metadata.UnityVersion);

        GlobalGameManagers = Manager.LoadAssetsFile(Path.Combine(GamePath, "globalgamemanagers"), loadDeps: true);
        ResourcesManager = GlobalGameManagers.file.GetAssetsOfType(AssetClassID.ResourceManager)[0];

        prefabs = [.. IterateResourcePrefabs()];
        spriteIndex = TakeAllSprites();//.ToDictionary(keySelector: e => e.Key, elementSelector: e => e.Value);

        return ResourcesAssets;
    }

    List<KeyValuePair<string, (AssetsFileInstance File, AssetFileInfo Info)>> TakeAllSprites()
    {
        List<KeyValuePair<string, (AssetsFileInstance File, AssetFileInfo Info)>> array = new();

        foreach (var info in ResourcesAssets!.file.GetAssetsOfType(AssetClassID.Sprite))
        {
            var spriteBase = Manager.GetBaseField(ResourcesAssets, info);

            array.Add(new KeyValuePair<string, (AssetsFileInstance File, AssetFileInfo Info)>(spriteBase["m_Name"].AsString, (ResourcesAssets, info)));
        }

        return array;
    }

    private IEnumerable<(string PrefabName, AssetsFileInstance File, AssetTypeValueField GoBase)> IterateResourcePrefabs()
    {
        var resourceManagerRoot = Manager.GetBaseField(GlobalGameManagers!, ResourcesManager!);
        var references = resourceManagerRoot["m_Container.Array"].ToList();

        foreach (var reference in references)
        {
            var assetExt = Manager.GetExtAsset(GlobalGameManagers!, reference[1]);

            if (assetExt.info == null || assetExt.info.TypeId != (int)AssetClassID.GameObject)
            {
                continue;
            }

            var goBase = Manager.GetBaseField(assetExt.file, assetExt.info);

            yield return (goBase["m_Name"].AsString, assetExt.file, goBase);
        }
    }

    public IEnumerable<PrefabBuildingSnapshot> ExtractPrefabFields(string behaviourName)
    {
        if (ResourcesAssets == null || GlobalGameManagers == null || ResourcesManager == null)
        {
            Console.WriteLine("Cannot extract prefab behaviours, call LoadResources() first.");

            return [];
        }

        var snapshots = new List<PrefabBuildingSnapshot>();

        foreach (var prefab in prefabs)
        {
            if (!TryFindBehaviour(prefab.File, prefab.GoBase, behaviourName, out var behaviour))
            {
                continue;
            }

            var spriteName = TryGetSpriteName(prefab.File, prefab.GoBase);
            snapshots.Add(new PrefabBuildingSnapshot(prefab.PrefabName, behaviour, spriteName));
        }

        return snapshots;
    }

    public string ExtractSprite(string objectName)
    {
        if (ResourcesAssets == null || GlobalGameManagers == null || ResourcesManager == null)
        {
            Console.WriteLine("Cannot extract monobehaviours, call LoadResources() first.");

            return string.Empty;
        }

        foreach (var prefab in prefabs)
        {
            if (prefab.PrefabName != objectName)
            {
                continue;
            }

            return TryGetSpriteName(prefab.File, prefab.GoBase);
        }

        return string.Empty;
    }

    private bool TryFindBehaviour(
        AssetsFileInstance file,
        AssetTypeValueField goBase,
        string behaviourName,
        out AssetTypeValueField behaviour)
    {
        foreach (var componentKeyPptr in goBase["m_Component.Array"])
        {
            var componentInstance = Manager.GetExtAsset(file, componentKeyPptr["component"]);

            if (componentInstance.info == null || componentInstance.info.TypeId != (int)AssetClassID.MonoBehaviour)
            {
                continue;
            }

            var script = Manager.GetExtAsset(ResourcesAssets!, componentInstance.baseField["m_Script"]);

            if (script.baseField == null || script.baseField["m_Name"].AsString != behaviourName)
            {
                continue;
            }

            behaviour = componentInstance.baseField;

            return true;
        }

        behaviour = default!;

        return false;
    }

    private string TryGetSpriteName(AssetsFileInstance file, AssetTypeValueField goBase)
    {
        foreach (var componentKeyPptr in goBase["m_Component.Array"])
        {
            var componentInstance = Manager.GetExtAsset(file, componentKeyPptr["component"]);

            if (componentInstance.info == null || componentInstance.info.TypeId != (int)AssetClassID.SpriteRenderer)
            {
                continue;
            }

            var spriteExtInfo = Manager.GetExtAsset(file, componentInstance.baseField["m_Sprite"]);

            if (spriteExtInfo.info == null)
            {
                continue;
            }

            return spriteExtInfo.baseField["m_Name"].AsString;
        }

        return string.Empty;
    }

    public void Dispose()
    {
        Manager?.UnloadAll(true);
        Manager = default;

        GC.SuppressFinalize(this);
    }

    ~AssetsParser()
    {
        Manager?.UnloadAll(true);
        Manager = default;
    }
}
