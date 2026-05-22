using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using Mono.Cecil;
using System.Text.Json;

namespace CasualtiesMiner.Dumper.Cli;

public class Program
{
    public static async Task Main(string[] args)
    {
        var fileName = args.Length > 0 ? args[0] : "Assembly-CSharp.dll";
        if (!File.Exists(fileName))
        {
            Console.WriteLine($"Can't find {fileName}.");

            return;
        }

        ModuleDefinition? module;
        try
        {
            module = ModuleDefinition.ReadModule(fileName);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to read module: {ex.Message}");
            return;
        }

        if (module.Name != "Assembly-CSharp.dll")
        {
            Console.WriteLine("Invalid file! Expecting Assembly-CSharp!");
            return;
        }

        var dumper = new Dumper(module);
        var decompilerSettings = new DecompilerSettings
        {
            ThrowOnAssemblyResolveErrors = false,
            UsingDeclarations = false
        };
        var cSharpDecompiler = new CSharpDecompiler(fileName, decompilerSettings);

        await File.WriteAllTextAsync("items.json",
            JsonSerializer.Serialize(dumper.DumpItems(cSharpDecompiler), JsonOptions.CamelCaseOptions));
        await File.WriteAllTextAsync("recipes.json",
            JsonSerializer.Serialize(dumper.DumpRecipes(cSharpDecompiler), JsonOptions.CamelCaseOptions));
        await File.WriteAllTextAsync("liquids.json",
            JsonSerializer.Serialize(dumper.DumpLiquids(cSharpDecompiler), JsonOptions.CamelCaseOptions));
        await File.WriteAllTextAsync("tiles.json",
            JsonSerializer.Serialize(dumper.DumpTiles(cSharpDecompiler), JsonOptions.CamelCaseOptions));
    }
}