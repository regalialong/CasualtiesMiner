using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using Mono.Cecil;
using System.Collections.Concurrent;
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

        var dumpedData = new ConcurrentDictionary<string, object>();

        await Task.WhenAll(
            Task.Run(() =>
            {
                dumpedData.TryAdd("items", dumper.DumpItems(new CSharpDecompiler(fileName, decompilerSettings)));
            }),
            Task.Run(() =>
            {
                dumpedData.TryAdd("recipes",
                    dumper.DumpRecipes(new CSharpDecompiler(fileName, decompilerSettings)));
            }),
            Task.Run(() =>
            {
                dumpedData.TryAdd("liquids",
                    dumper.DumpLiquids(new CSharpDecompiler(fileName, decompilerSettings)));
            }),
            Task.Run(() =>
            {
                dumpedData.TryAdd("tiles", dumper.DumpTiles(new CSharpDecompiler(fileName, decompilerSettings)));
            })
        );

        // TODO: Fix AOT here plz
        await File.WriteAllTextAsync("data.json",
            JsonSerializer.Serialize(
                dumpedData, DumperJsonOptions.CamelCaseOptions));
    }
}