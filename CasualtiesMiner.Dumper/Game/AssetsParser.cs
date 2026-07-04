using AssetsTools.NET.Extra;

namespace CasualtiesMiner.Dumper.Game;

public sealed class AssetsParser : IDisposable
{
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

    public AssetsFileInstance LoadResources()
    {
        using var classPackage = OpenEmbeddedClassPackage();
        Manager.LoadClassPackage(classPackage);

        var assetsPath = Path.Combine(GamePath, "resources.assets");
        var instance = Manager.LoadAssetsFile(assetsPath, loadDeps: true);

        Manager.LoadClassDatabaseFromPackage(instance.file.Metadata.UnityVersion);

        return instance;
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

    public void Dispose()
    {
        Manager = default;
        GC.SuppressFinalize(this);
    }
}
