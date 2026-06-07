using CasualtiesMiner.Shared.Models;
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

        ItemInfo[] items = [];
        Recipe[] recipes = [];
        LiquidType[] liquids = [];
        BlockInfo[] tiles = [];
        MoodleInfo[] moodles = [];

        await Task.WhenAll(
            Task.Run(() => items = dumper.DumpItems(new CSharpDecompiler(fileName, decompilerSettings))),
            Task.Run(() => recipes = dumper.DumpRecipes(new CSharpDecompiler(fileName, decompilerSettings))),
            Task.Run(() => liquids = dumper.DumpLiquids(new CSharpDecompiler(fileName, decompilerSettings))),
            Task.Run(() => tiles = dumper.DumpTiles(new CSharpDecompiler(fileName, decompilerSettings))),
            Task.Run(() => moodles = dumper.DumpMoodles())
        );

        Console.WriteLine($"Dumped {items.Length} items.");
        Console.WriteLine($"Dumped {recipes.Length} recipes.");
        Console.WriteLine($"Dumped {liquids.Length} liquids.");
        Console.WriteLine($"Dumped {tiles.Length} tiles.");
        Console.WriteLine($"Dumped {moodles.Length} moodles.");

        var dumpedData = new DumpedData
        {
            Items = items,
            Recipes = recipes,
            Liquids = liquids,
            Tiles = tiles,
            Moodles = moodles
        };

        await File.WriteAllTextAsync("data.json",
            JsonSerializer.Serialize(dumpedData, DumperJsonOptions.CamelCaseOptions));
    }
}