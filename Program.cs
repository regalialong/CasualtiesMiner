using Mono.Collections.Generic;
using System.Text.Json;
using Mono.Cecil.Cil;
using Mono.Cecil;

public class Program
{
    public static async Task Main(string[] args)
    {
        var fileName = args.Length > 0 ? args[0] : "Assembly-CSharp.dll";

        if (!File.Exists(fileName))
        {
            Console.WriteLine($"Can't find {fileName}");
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

        await Task.WhenAll(
            AnalyzeItems(module),
            AnalyzeRecipes(module),
            AnalyzeLiquids(module)
        );
    }

    private static int ParseInt(Instruction inst)
    {
        return inst.OpCode.Name switch
        {
            "ldc.i4.0" => 0,
            "ldc.i4.1" => 1,
            "ldc.i4.2" => 2,
            "ldc.i4.3" => 3,
            "ldc.i4.4" => 4,
            "ldc.i4.5" => 5,
            "ldc.i4.6" => 6,
            "ldc.i4.7" => 7,
            "ldc.i4.8" => 8,
            "ldc.i4.m1" => -1,
            _ => inst.Operand is int v ? v : Convert.ToInt32(inst.Operand)
        };
    }

    public static Dictionary<FieldDefinition, List<Instruction>> ExtractFields(
        List<Instruction> instructions)
    {
        var fields = new Dictionary<FieldDefinition, List<Instruction>>();

        for (var i = 0; i < instructions.Count; i++)
        {
            if (instructions[i].OpCode.Name != "dup") continue;
            i++;

            var valueOpcodes = new List<Instruction>();
            while (i < instructions.Count && instructions[i].OpCode.Name != "stfld")
                valueOpcodes.Add(instructions[i++]);

            if (i < instructions.Count && instructions[i].Operand is FieldDefinition field)
                fields[field] = valueOpcodes;
        }

        return fields;
    }

    private static int ParseObjectFields(
        Collection<Instruction> instructions,
        int startIndex,
        string declaringTypeName,
        Dictionary<string, object?> target,
        Func<string, bool> isStopCall)
    {
        var i = startIndex;

        while (++i < instructions.Count)
        {
            var inst = instructions[i];

            if (inst.OpCode.Name == "callvirt" && isStopCall(inst.Operand?.ToString() ?? ""))
                return i;

            if (inst.OpCode.Name != "dup") continue;

            var valueOpcodes = new List<Instruction>();

            while (++i < instructions.Count)
            {
                var next = instructions[i];
                if (next.OpCode.Name == "stfld"
                    && next.Operand is FieldDefinition fd
                    && fd.DeclaringType.Name == declaringTypeName)
                {
                    target[fd.Name] = ParseFieldValue(fd, valueOpcodes);
                    break;
                }

                valueOpcodes.Add(next);
            }
        }

        return i;
    }

    private static object? ParseFieldValue(FieldDefinition field, List<Instruction> instructions)
    {
        if (instructions.Count == 0) return null;

        if (field.FieldType.IsPrimitive)
            return field.FieldType.Name switch
            {
                "Boolean" => instructions[0].OpCode.Name == "ldc.i4.1",
                "Single" => instructions[0].Operand,
                "Byte" => Convert.ToByte(ParseInt(instructions[0])),
                "Int32" => ParseInt(instructions[0]),
                _ => WarnUnhandled(field, instructions[0])
            };

        return field.FieldType.Name switch
        {
            "String" => instructions[0].Operand ?? instructions[0].OpCode.Name,
            "Recognition" => ParseInt(instructions[0]),
            _ => ParseComplexValue(field, instructions)
        };
    }

    private static object? ParseComplexValue(FieldDefinition field, List<Instruction> instructions)
    {
        switch (field.FieldType.FullName)
        {
            case "RecipeResult":
                {
                    var result = new Dictionary<string, object?>();
                    foreach (var (f, vals) in ExtractFields(instructions))
                        result[f.Name] = ParseFieldValue(f, vals);
                    return result;
                }

            case "Recipes/RecipeCategory":
                return ParseInt(instructions[0]);

            case "CraftingQuality":
                {
                    var result = new Dictionary<string, object?>();

                    switch (instructions.Count)
                    {
                        case 3:
                            result["id"] = instructions[0].Operand;
                            result["amount"] = instructions[1].Operand;
                            break;
                        case 2:
                            result["id"] = instructions[0].Operand;
                            result["amount"] = 1f;
                            break;
                    }

                    return result;
                }

            case "UnityEngine.Color":
                {
                    var result = new Dictionary<string, object?>();

                    // TODO: If the last call is `call valuetype [UnityEngine.CoreModule]UnityEngine.Color [UnityEngine.CoreModule]UnityEngine.Color32::op_Implicit(valuetype [UnityEngine.CoreModule]UnityEngine.Color32)`
                    if (instructions.Count == 4)
                    {
                        result["r"] = ParseInt(instructions[0]) * 255;
                        result["g"] = ParseInt(instructions[1]) * 255;
                        result["b"] = ParseInt(instructions[2]) * 255;
                        result["a"] = 255;
                    }
                    else if (instructions.Count == 6)
                    {
                        result["r"] = ParseInt(instructions[0]);
                        result["g"] = ParseInt(instructions[1]);
                        result["b"] = ParseInt(instructions[2]);
                        result["a"] = ParseInt(instructions[3]);
                    }

                    return result;
                }
        }

        if (field.FieldType.FullName.StartsWith("System.Collections.Generic.List`1"))
            return ParseList(instructions);

        Console.WriteLine($"[WARNING] No parser for '{field.Name}' ({field.FieldType.FullName})");
        foreach (var inst in instructions) Console.WriteLine($"  {inst}");
        return null;
    }

    private static List<Dictionary<string, object?>> ParseList(List<Instruction> instructions)
    {
        var items = new List<Dictionary<string, object?>>();
        Dictionary<string, object?>? current = null;
        var buffer = new List<Instruction>();

        foreach (var inst in instructions)
            switch (inst.OpCode.Name)
            {
                case "newobj":
                case "call":
                    if (inst.Operand is MethodReference ctor
                        && !ctor.DeclaringType.FullName.StartsWith("System.Collections.Generic.List`1"))
                    {
                        if (current == null)
                        {
                            current = new Dictionary<string, object?>();
                            for (var p = 0; p < ctor.Parameters.Count && p < buffer.Count; p++)
                                current[ctor.Parameters[p].Name] = buffer[p].Operand ?? buffer[p].OpCode.Name;
                            buffer.Clear();
                        }
                        else
                        {
                            buffer.Add(inst);
                        }
                    }
                    break;

                case "dup":
                    buffer.Clear();
                    break;

                case "stfld":
                    if (current != null && inst.Operand is FieldDefinition field)
                    {
                        current[field.Name] = ParseFieldValue(field, buffer);
                        buffer = new List<Instruction>();
                    }

                    break;

                case "callvirt":
                    if (current != null && inst.Operand?.ToString()?.Contains("::Add(") == true)
                    {
                        items.Add(current);
                        current = null;
                    }

                    break;

                default:
                    buffer.Add(inst);
                    break;
            }

        return items;
    }

    private static object WarnUnhandled(FieldDefinition field, Instruction inst)
    {
        Console.WriteLine($"[WARNING] Unhandled primitive type: {field.FieldType.Name}");
        return inst.Operand ?? inst.OpCode.Name;
    }


    public static Task AnalyzeItems(ModuleDefinition module)
    {
        Console.WriteLine("Analyzing Items...");
        var itemList = new List<Dictionary<string, object?>>();

        var itemType = module.Types.FirstOrDefault(t => t.FullName == "Item");
        var itemInfoType = module.Types.FirstOrDefault(t => t.FullName == "ItemInfo");
        if (itemType is null || itemInfoType is null) return Task.CompletedTask;

        var setupMethod = itemType.Methods.FirstOrDefault(m => m.Name == "SetupItems");
        var globalField = itemType.Fields.FirstOrDefault(m => m.Name == "GlobalItems");
        if (setupMethod is null || globalField is null) return Task.CompletedTask;

        var itemInfoCtor = itemInfoType.Methods.First(m => m.IsConstructor);
        var instructions = setupMethod.Body.Instructions;

        for (var i = 0; i < instructions.Count - 2; i++)
        {
            if (instructions[i].OpCode.Name != "ldsfld" || instructions[i].Operand != globalField) continue;
            if (instructions[i + 1].OpCode.Name != "ldstr") continue;
            if (instructions[i + 2].OpCode.Name != "newobj" || instructions[i + 2].Operand != itemInfoCtor) continue;

            var itemName = (string)instructions[i + 1].Operand;
            var itemInfo = new Dictionary<string, object?> { ["name"] = itemName };

            i = ParseObjectFields(instructions, i + 2, "ItemInfo", itemInfo, op =>
                op is "System.Void System.Collections.Generic.Dictionary`2<System.String,ItemInfo>::Add(!0,!1)"
                    or "System.Collections.Generic.Dictionary`2/Enumerator<!0,!1> System.Collections.Generic.Dictionary`2<System.String,ItemInfo>::GetEnumerator()");

            itemList.Add(itemInfo);
        }

        File.WriteAllText("items.json", JsonSerializer.Serialize(itemList));
        return Task.CompletedTask;
    }

    public static Task AnalyzeRecipes(ModuleDefinition module)
    {
        Console.WriteLine("Analyzing Recipes...");
        var recipeList = new List<Dictionary<string, object?>>();

        var recipesType = module.Types.FirstOrDefault(t => t.FullName == "Recipes");
        var recipeType = module.Types.FirstOrDefault(t => t.FullName == "Recipe");
        if (recipesType is null || recipeType is null) return Task.CompletedTask;

        var setupMethod = recipesType.Methods.FirstOrDefault(m => m.Name == "SetUpRecipes");
        var globalField = recipesType.Fields.FirstOrDefault(m => m.Name == "recipes");
        if (setupMethod is null || globalField is null) return Task.CompletedTask;

        var recipeCtor = recipeType.Methods.First(m => m.IsConstructor);
        var instructions = setupMethod.Body.Instructions;

        for (var i = 0; i < instructions.Count - 1; i++)
        {
            if (instructions[i].OpCode.Name != "ldsfld" || instructions[i].Operand != globalField) continue;
            if (instructions[i + 1].OpCode.Name != "newobj" || instructions[i + 1].Operand != recipeCtor) continue;

            var recipeInfo = new Dictionary<string, object?>();

            i = ParseObjectFields(instructions, i + 1, "Recipe", recipeInfo,
                op => op == "System.Void System.Collections.Generic.List`1<Recipe>::Add(!0)");

            recipeList.Add(recipeInfo);
        }

        File.WriteAllText("recipes.json", JsonSerializer.Serialize(recipeList));
        return Task.CompletedTask;
    }

    public static Task AnalyzeLiquids(ModuleDefinition module)
    {
        Console.WriteLine("Analyzing Liquids...");
        var liquidList = new List<Dictionary<string, object?>>();

        var liquidsType = module.Types.FirstOrDefault(t => t.FullName == "Liquids");
        var liquidType = module.Types.FirstOrDefault(t => t.FullName == "LiquidType");
        if (liquidsType is null || liquidType is null) return Task.CompletedTask;

        var cctor = liquidsType.Methods.FirstOrDefault(m => m.IsConstructor && m.IsStatic);
        if (cctor is null) return Task.CompletedTask;

        var liquidCtor = liquidType.Methods.First(m => m.IsConstructor && !m.HasParameters);
        var instructions = cctor.Body.Instructions;

        for (var i = 0; i < instructions.Count - 1; i++)
        {
            if (instructions[i].OpCode.Name != "ldstr") continue;
            if (instructions[i + 1].OpCode.Name != "newobj" || instructions[i + 1].Operand != liquidCtor) continue;

            var key = (string)instructions[i].Operand;
            var entry = new Dictionary<string, object?> { ["id"] = key };

            i = ParseObjectFields(instructions, i + 1, "LiquidType", entry,
                op => op.Contains("::set_Item("));

            liquidList.Add(entry);
        }

        File.WriteAllText("liquids.json", JsonSerializer.Serialize(liquidList));
        return Task.CompletedTask;
    }

    // TODO: Add layer info
    // Okay so from what I know, All layer extends from `LayerModifier`?? and there's absolutely no fucking way we can parse it without inspecting
    // Their method body (Initialize, Disable)

    // TODO: Add block info
    // Take a look at the `WorldGeneration.GetBlockInfo`, it's a big Switch case, so should be easy? idrk
}