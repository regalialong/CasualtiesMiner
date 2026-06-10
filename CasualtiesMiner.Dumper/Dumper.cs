using Mono.Cecil;

namespace CasualtiesMiner.Dumper;

public sealed partial class Dumper
{
    private readonly ModuleDefinition _module;

    public Dumper(ModuleDefinition module)
    {
        _module = module;
    }
}
