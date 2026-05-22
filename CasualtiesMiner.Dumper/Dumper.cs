using System.Reflection.Metadata.Ecma335;
using CasualtiesMiner.Shared.Models;
using ICSharpCode.Decompiler.CSharp;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;

namespace CasualtiesMiner.Dumper;

public class Dumper
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

    public ItemInfo[] DumpItems(CSharpDecompiler decompiler)
    {
        var itemList = new List<ItemInfo>();

        var itemType = _module.Types.FirstOrDefault(t => t.FullName == "Item");
        var itemInfoType = _module.Types.FirstOrDefault(t => t.FullName == "ItemInfo");
        if (itemType is null || itemInfoType is null) return [];

        var setupMethod = itemType.Methods.FirstOrDefault(m => m.Name == "SetupItems");
        var globalField = itemType.Fields.FirstOrDefault(m => m.Name == "GlobalItems");
        if (setupMethod is null || globalField is null) return [];

        var itemInfoCtor = itemInfoType.Methods.First(m => m.IsConstructor);
        var instructions = setupMethod.Body.Instructions;

        for (var i = 0; i < instructions.Count - 2; i++)
        {
            if (instructions[i].OpCode.Code != Code.Ldsfld || instructions[i].Operand != globalField) continue;
            if (instructions[i + 1].OpCode.Code != Code.Ldstr) continue;
            if (instructions[i + 2].OpCode.Code != Code.Newobj || instructions[i + 2].Operand != itemInfoCtor) continue;

            var itemName = (string)instructions[i + 1].Operand;
            var itemInfo = new Dictionary<string, object?>();

            i = ParseObjectFields(decompiler, instructions, i + 2, "ItemInfo", itemInfo, op =>
                op is "System.Void System.Collections.Generic.Dictionary`2<System.String,ItemInfo>::Add(!0,!1)"
                    or "System.Collections.Generic.Dictionary`2/Enumerator<!0,!1> System.Collections.Generic.Dictionary`2<System.String,ItemInfo>::GetEnumerator()");

            List<T> ConvertList<T>(List<object?>? objects)
            {
                return objects?.Cast<T>().ToList() ?? [];
            }

            itemList.Add(new ItemInfo
            {
                name = itemName,
                category = GetValue<string>(itemInfo, "category"),
                slotRotation = GetValue<float>(itemInfo, "slotRotation"),
                usable = GetValue<bool>(itemInfo, "usable"),
                usableOnLimb = GetValue<bool>(itemInfo, "usableOnLimb"),
                rotSpeed = itemInfo.TryGetValue("decayMinutes", out var value) ? 1.666f / (float)value! : 0,
                useAction = GetValue<string[]>(itemInfo, "useAction"),
                useLimbAction = GetValue<string[]>(itemInfo, "useLimbAction"),
                destroyAtZeroCondition = GetValue<bool>(itemInfo, "destroyAtZeroCondition"),
                weight = GetValue<float>(itemInfo, "weight"),
                scaleWeightWithCondition = GetValue<bool>(itemInfo, "scaleWeightWithCondition"),
                onlyHoldInHands = GetValue<bool>(itemInfo, "onlyHoldInHands"),
                autoAttack = GetValue<bool>(itemInfo, "autoAttack"),
                usableWithLMB = GetValue<bool>(itemInfo, "usableWithLMB"),
                wearable = GetValue<bool>(itemInfo, "wearable"),
                wearableCanBeHeld = GetValue<bool>(itemInfo, "wearableCanBeHeld"),
                desiredWearLimb = GetValue<string>(itemInfo, "desiredWearLimb"),
                wearSlotId = GetValue<string>(itemInfo, "wearSlotId"),
                wearableArmor = GetValue<float>(itemInfo, "wearableArmor"),
                wearableIsolation = GetValue<float>(itemInfo, "wearableIsolation"),
                wearableHitDurabilityLossMultiplier = GetValue<float>(itemInfo, "wearableHitDurabilityLossMultiplier"),
                jumpHeightMultChange = GetValue<float>(itemInfo, "jumpHeightMultChange"),
                combineable = GetValue<bool>(itemInfo, "combineable"),
                ignoreDepression = GetValue<bool>(itemInfo, "ignoreDepression"),
                value = GetValue<int>(itemInfo, "value"),
                wearableVisualOffset = GetValue(itemInfo, "wearableVisualOffset", 5),
                tags = GetValue(itemInfo, "tags", "").Split(","),
                decayInfo = GetValue<byte>(itemInfo, "decayInfo"),
                decayMinutes = GetValue<float>(itemInfo, "decayMinutes"),
                rec = GetValue(itemInfo, "rec", 2),
                qualities = ConvertList<CraftingQuality>(GetValue<List<object?>>(itemInfo, "qualities"))
            });
        }

        return [.. itemList];
    }

    public RecipeInfo[] DumpRecipes(CSharpDecompiler decompiler)
    {
        var recipeList = new List<RecipeInfo>();

        var recipesType = _module.Types.FirstOrDefault(t => t.FullName == "Recipes");
        var recipeType = _module.Types.FirstOrDefault(t => t.FullName == "Recipe");

        if (recipesType is null || recipeType is null) return [];

        var setupMethod = recipesType.Methods.FirstOrDefault(m => m.Name == "SetUpRecipes");
        var globalField = recipesType.Fields.FirstOrDefault(m => m.Name == "recipes");

        if (setupMethod is null || globalField is null) return [];

        var recipeCtor = recipeType.Methods.First(m => m.IsConstructor);
        var instructions = setupMethod.Body.Instructions;

        for (var i = 0; i < instructions.Count - 1; i++)
        {
            if (instructions[i].OpCode.Code != Code.Ldsfld || instructions[i].Operand != globalField) continue;
            if (instructions[i + 1].OpCode.Code != Code.Newobj || instructions[i + 1].Operand != recipeCtor) continue;

            var recipeDict = new Dictionary<string, object?>();

            i = ParseObjectFields(decompiler, instructions, i + 1, "Recipe", recipeDict,
                op => op == "System.Void System.Collections.Generic.List`1<Recipe>::Add(!0)");

            var recipe = new RecipeInfo
            {
                specialKnown = GetValue<bool>(recipeDict, "specialKnown"),
                INT = GetValue<int>(recipeDict, "INT"),
                items = recipeDict.TryGetValue("items", out var itemsObj) && itemsObj is List<object?> list
                    ? [.. list.Cast<RecipeItem>()]
                    : [],
                result = GetValue<RecipeResult>(recipeDict, "result"),
                hasMadeBefore = GetValue<bool>(recipeDict, "hasMadeBefore"),
                category = GetValue<int>(recipeDict, "category"),
                isRepair = GetValue<bool>(recipeDict, "isRepair"),
                index = GetValue<int>(recipeDict, "index")
            };

            recipeList.Add(recipe);
        }

        return [.. recipeList];
    }

    public LiquidInfo[] DumpLiquids(CSharpDecompiler decompiler)
    {
        var liquidList = new List<LiquidInfo>();

        var liquidsType = _module.Types.FirstOrDefault(t => t.FullName == "Liquids");
        var liquidType = _module.Types.FirstOrDefault(t => t.FullName == "LiquidType");
        if (liquidsType is null || liquidType is null) return [];

        var cctor = liquidsType.Methods.FirstOrDefault(m => m.IsConstructor && m.IsStatic);
        if (cctor is null) return [];

        var liquidCtor = liquidType.Methods.First(m => m.IsConstructor && !m.HasParameters);
        var instructions = cctor.Body.Instructions;

        for (var i = 0; i < instructions.Count - 1; i++)
        {
            if (instructions[i].OpCode.Code != Code.Ldstr) continue;
            if (instructions[i + 1].OpCode.Code != Code.Newobj || instructions[i + 1].Operand != liquidCtor) continue;

            var key = (string)instructions[i].Operand;
            var entry = new Dictionary<string, object?> { ["name"] = key };

            i = ParseObjectFields(decompiler, instructions, i + 1, "LiquidType", entry,
                op => op.Contains("::set_Item("));

            List<T> ConvertList<T>(List<object?>? objects)
            {
                return objects?.Cast<T>().ToList() ?? [];
            }

            var liquid = new LiquidInfo
            {
                name = key,
                color = GetValue<Color>(entry, "color"),
                valuePerLiter = GetValue<float>(entry, "valuePerLiter"),
                onDrink = GetValue<string[]>(entry, "onDrink"),
                onHealthUse = GetValue<string[]>(entry, "onHealthUse"),
                healthUsable = GetValue<bool>(entry, "healthUsable"),
                injectable = GetValue<bool>(entry, "injectable"),
                localeFromItem = GetValue<bool>(entry, "localeFromItem"),
                injectionSickness = GetValue(entry, "injectionSickness", 1f),
                qualities = ConvertList<CraftingQuality>(GetValue<List<object?>>(entry, "qualities"))
            };

            liquidList.Add(liquid);
        }

        return [.. liquidList];
    }

    public TileInfo[] DumpTiles(CSharpDecompiler decompiler)
    {
        var tileList = new List<TileInfo>();

        var worldGenType = _module.Types.FirstOrDefault(t => t.FullName == "WorldGeneration");
        var blockInfoType = _module.Types.FirstOrDefault(t => t.FullName == "BlockInfo");
        if (worldGenType is null || blockInfoType is null) return [];

        var setupMethod = worldGenType.Methods.FirstOrDefault(m => m.Name == "GetBlockInfo");
        if (setupMethod is null) return [];

        var instructions = setupMethod.Body.Instructions;

        var switchInst = instructions.FirstOrDefault(i => i.OpCode.Code == Code.Switch);
        if (switchInst == null) return [];

        var switchTargets = (Instruction[])switchInst.Operand;

        foreach (var targetInst in switchTargets)
        {
            if (targetInst.OpCode.Code != Code.Newobj) continue;

            var entry = new Dictionary<string, object?>();

            var index = instructions.IndexOf(targetInst);
            for (var j = index; j < instructions.Count; j++)
            {
                var instruction = instructions[j];
                if (instruction.OpCode.Code == Code.Ret) break;
                if (instruction.OpCode.Code != Code.Dup) continue;

                var valueOpcodes = new List<Instruction>();
                var k = j + 1;
                while (k < instructions.Count && instructions[k].OpCode.Code != Code.Stfld)
                {
                    valueOpcodes.Add(instructions[k]);
                    k++;
                }

                if (k >= instructions.Count || instructions[k].Operand is not FieldDefinition fd) continue;

                entry[fd.Name] = ParseFieldValue(decompiler, fd.FieldType, valueOpcodes, fd.Name);
                j = k;
            }

            tileList.Add(new TileInfo
            {
                health = GetValue<float>(entry, "health"),
                name = GetValue<string>(entry, "name"),
                hitsound = GetValue<string>(entry, "hitsound"),
                stepsound = GetValue<string>(entry, "stepsound"),
                noVariation = GetValue<bool>(entry, "noVariation"),
                metallic = GetValue<bool>(entry, "metallic"),
                toxicity = GetValue<float>(entry, "toxicity"),
                slippery = GetValue<bool>(entry, "slippery"),
                sleep = GetValue<int>(entry, "sleep")
            });
        }

        return [.. tileList];
    }

    private static int ParseObjectFields(
        CSharpDecompiler decompiler,
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

            if (inst.OpCode.Code == Code.Callvirt && isStopCall(inst.Operand?.ToString() ?? ""))
                return i;

            if (inst.OpCode.Code != Code.Dup) continue;

            var valueOpcodes = new List<Instruction>();

            while (++i < instructions.Count)
            {
                var next = instructions[i];
                if (next.OpCode.Code == Code.Stfld
                    && next.Operand is FieldDefinition fd
                    && fd.DeclaringType.Name == declaringTypeName)
                {
                    target[fd.Name] = ParseFieldValue(decompiler, fd.FieldType, valueOpcodes, fd.Name);
                    break;
                }

                valueOpcodes.Add(next);
            }
        }

        return i;
    }

    private static object? ParseFieldValue(
        CSharpDecompiler decompiler,
        TypeReference type,
        List<Instruction> instructions,
        string fieldName = "")
    {
        if (instructions.Count == 0) return null;

        if (type.IsPrimitive)
            return type.Name switch
            {
                "Boolean" => instructions[0].OpCode.Code == Code.Ldc_I4_1,
                "Single" => Convert.ToSingle(instructions[0].Operand),
                "Byte" => Convert.ToByte(ParseInt(instructions[0])),
                "Int32" => ParseInt(instructions[0]),
                _ => WarnUnhandled(decompiler, type, instructions[0], fieldName)
            };

        return type.Name switch
        {
            "String" => instructions[0].Operand ?? instructions[0].OpCode.Name,
            "Recognition" => ParseInt(instructions[0]),
            "SleepQuality" => ParseInt(instructions[0]),
            _ => ParseComplexValue(decompiler, type, instructions, fieldName)
        };
    }

    private static object? ParseComplexValue(
        CSharpDecompiler decompiler,
        TypeReference type,
        List<Instruction> instructions,
        string fieldName)
    {
        switch (type.FullName)
        {
            case "RecipeResult":
            {
                var dict = new Dictionary<string, object?>();
                foreach (var (f, vals) in ExtractFields(decompiler, instructions))
                    dict[f.Name] = ParseFieldValue(decompiler, f.FieldType, vals, f.Name);

                return new RecipeResult
                {
                    id = GetValue<string>(dict, "id"),
                    isLiquid = GetValue<bool>(dict, "isLiquid"),
                    amount = GetValue(dict, "amount", 1),
                    resultCondition = GetValue(dict, "resultCondition", 1f),
                    dontDrainResultLiquid = GetValue<bool>(dict, "dontDrainResultLiquid")
                };
            }

            case "RecipeItem":
            {
                var dict = new Dictionary<string, object?>();
                foreach (var (f, vals) in ExtractFields(decompiler, instructions))
                    dict[f.Name] = ParseFieldValue(decompiler, f.FieldType, vals, f.Name);

                return new RecipeItem
                {
                    specific = GetValue<bool>(dict, "specific"),
                    specificId = GetValue<string>(dict, "specificId"),
                    isLiquid = GetValue<bool>(dict, "isLiquid"),
                    quality = GetValue<CraftingQuality>(dict, "quality"),
                    minimumCondition = GetValue(dict, "minimumCondition", 0.9f),
                    destroyItem = GetValue(dict, "destroyItem", true),
                    ignoredId = GetValue<string>(dict, "ignoredId")
                };
            }

            case "Recipes/RecipeCategory":
                return ParseInt(instructions[0]);

            case "CraftingQuality":
            {
                var craftingQuality = new CraftingQuality
                {
                    id = "",
                    amount = 0
                };

                switch (instructions.Count)
                {
                    case 3:
                        craftingQuality.id = (string)instructions[0].Operand;
                        craftingQuality.amount = (float)instructions[1].Operand;
                        break;
                    case 2:
                        craftingQuality.id = (string)instructions[0].Operand;
                        craftingQuality.amount = 1f;
                        break;
                }

                return craftingQuality;
            }

            case "UnityEngine.Color":
            {
                var result = new Color();

                switch (instructions.Count)
                {
                    case 4:
                        result.r = (byte)(ParseInt(instructions[0]) * 255);
                        result.g = (byte)(ParseInt(instructions[1]) * 255);
                        result.b = (byte)(ParseInt(instructions[2]) * 255);
                        result.a = 255;
                        break;
                    case 6:
                        result.r = (byte)ParseInt(instructions[0]);
                        result.g = (byte)ParseInt(instructions[1]);
                        result.b = (byte)ParseInt(instructions[2]);
                        result.a = (byte)ParseInt(instructions[3]);
                        break;
                }

                return result;
            }

            case "ItemInfo/Use":
            case "ItemInfo/UseLimb":
            case "LiquidType/OnDrink":
            case "LiquidType/OnHealthUse":
            {
                var pointerToDelegate = instructions.First(p => p.OpCode.Code == Code.Ldftn);
                if (pointerToDelegate is null) return null;

                var methodRef = (MethodReference)pointerToDelegate.Operand;
                var methodDef = methodRef.Resolve();

                return decompiler.DecompileAsString(MetadataTokens.EntityHandle(methodDef.MetadataToken.ToInt32()))
                    .Replace("\r\n", "\n").Replace("\t", "    ").Split("\n");
            }
        }

        if (type.FullName.StartsWith("System.Collections.Generic.List`1"))
        {
            var elementType = type is GenericInstanceType git ? git.GenericArguments[0] : type;
            return ParseList(decompiler, elementType, instructions, fieldName);
        }

        Console.WriteLine($"[WARNING] No parser for '{fieldName}' ({type.FullName})");

        foreach (var inst in instructions)
            Console.WriteLine($"  {inst}");

        return null;
    }

    private static List<object?> ParseList(
        CSharpDecompiler decompiler,
        TypeReference elementType,
        List<Instruction> instructions,
        string fieldName)
    {
        var items = new List<object?>();
        var buffer = new List<Instruction>();
        List<Instruction>? current = null;

        foreach (var inst in instructions)
            switch (inst.OpCode.Code)
            {
                case Code.Newobj:
                case Code.Call:
                    if (inst.Operand is MethodReference ctor
                        && !ctor.DeclaringType.FullName.StartsWith("System.Collections.Generic.List`1"))
                    {
                        if (current is null)
                        {
                            current = [.. buffer, inst];
                            buffer.Clear();
                        }
                        else
                        {
                            current.Add(inst);
                        }
                    }

                    break;

                case Code.Callvirt:
                    if (current != null && inst.Operand?.ToString()?.Contains("::Add(") == true)
                    {
                        items.Add(ParseFieldValue(decompiler, elementType, current, fieldName));
                        current = null;
                    }

                    break;

                case Code.Dup:
                    buffer.Clear();
                    current?.Add(inst);
                    break;

                case Code.Stfld:
                    current?.Add(inst);
                    break;

                default:
                    (current ?? buffer).Add(inst);
                    break;
            }

        return items;
    }

    private static object WarnUnhandled(CSharpDecompiler _, TypeReference type, Instruction inst,
        string fieldName)
    {
        Console.WriteLine($"[WARNING] Unhandled primitive type: {type.Name} for field {fieldName}");
        return inst.Operand ?? inst.OpCode.Name;
    }

    public static Dictionary<FieldDefinition, List<Instruction>> ExtractFields(CSharpDecompiler _,
        List<Instruction> instructions)
    {
        var fields = new Dictionary<FieldDefinition, List<Instruction>>();

        for (var i = 0; i < instructions.Count; i++)
        {
            if (instructions[i].OpCode.Code != Code.Dup) continue;
            i++;

            var valueOpcodes = new List<Instruction>();
            while (i < instructions.Count && instructions[i].OpCode.Code != Code.Stfld)
                valueOpcodes.Add(instructions[i++]);

            if (i < instructions.Count && instructions[i].Operand is FieldDefinition field)
                fields[field] = valueOpcodes;
        }

        return fields;
    }

    private static int ParseInt(Instruction inst)
    {
        return inst.OpCode.Code switch
        {
            Code.Ldc_I4_0 => 0,
            Code.Ldc_I4_1 => 1,
            Code.Ldc_I4_2 => 2,
            Code.Ldc_I4_3 => 3,
            Code.Ldc_I4_4 => 4,
            Code.Ldc_I4_5 => 5,
            Code.Ldc_I4_6 => 6,
            Code.Ldc_I4_7 => 7,
            Code.Ldc_I4_8 => 8,
            Code.Ldc_I4_M1 => -1,
            _ => inst.Operand is int v ? v : Convert.ToInt32(inst.Operand)
        };
    }

    private static T GetValue<T>(Dictionary<string, object?> dictionary, string key, T defaultValue = default!)
    {
        if (dictionary.TryGetValue(key, out var value) && value is not null) return (T)value;

        return defaultValue;
    }
}