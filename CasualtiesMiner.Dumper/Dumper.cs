using Mono.Cecil;

namespace CasualtiesMiner.Dumper;

public sealed partial class Dumper
{
    private readonly ModuleDefinition _module;

    public Dumper(string filePath)
    {
        _module = ModuleDefinition.ReadModule(filePath);
    }

    public Dumper(ModuleDefinition module)
    {
        _module = module;
    }
}
