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


        ModuleDefinition? module = null;

        try
        {
            module = ModuleDefinition.ReadModule(fileName);
        }
        catch (Exception)
        {
        }

        if (module?.Name != "Assembly-CSharp.dll")
        {
            Console.WriteLine("Invalid file! Expecting Assembly-CSharp!");
            return;
        }

        await Task.WhenAll(
            AnalyzeItems(module)
        );
    }

    public static object? ParseOperand(FieldDefinition field, List<Instruction> instructions)
    {
        if (field.FieldType.IsPrimitive)
            switch (field.FieldType.Name)
            {
                case "Boolean":
                    return instructions[0].OpCode.Name == "ldc.i4.1";
                case "Byte":
                case "Int32":
                    var value = instructions[0].OpCode.Name switch
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
                        _ => instructions[0].Operand ?? instructions[0].OpCode.Name
                    };

                    return field.FieldType.Name == "Byte" ? Convert.ToByte(value) : value;
                case "Single":
                    return instructions[0].Operand;
                default:
                    Console.WriteLine($"Unhandled FieldType: {field.FieldType.Name}");
                    return instructions[0].Operand ?? instructions[0].OpCode.Name;
            }

        // Console.WriteLine(field.FieldType.Name);

        switch (field.FieldType.Name)
        {
            case "String":
                return instructions[0].Operand ?? instructions[0].OpCode.Name;
            case "Recognition":
                return instructions[0].OpCode.Name switch
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
                    _ => instructions[0].Operand ?? instructions[0].OpCode.Name
                };
        }

        switch (field.FieldType.FullName)
        {
            case "System.Collections.Generic.List`1<CraftingQuality>":
                var qualities = new List<Dictionary<string, object>>();

                var name = "";
                var amount = 1f;

                foreach (var instruction in instructions.Where(instruction => instruction.OpCode.Name != "dup"))
                {
                    switch (instruction.OpCode.Name)
                    {
                        case "ldstr":
                            name = (string)instruction.Operand;
                            break;
                        case "ldc.r4":
                            amount = (float)instruction.Operand;
                            break;
                    }
                }

                qualities.Add(new Dictionary<string, object>
                {
                    ["name"] = name,
                    ["amount"] = amount
                });

                return qualities;
        }

        Console.WriteLine($"[WARNING] Missing parser for {field.Name} of type {field.FieldType.FullName}");
        foreach (var inst in instructions) Console.WriteLine(inst);

        return null;
    }


    public static Task AnalyzeItems(ModuleDefinition module)
    {
        Console.WriteLine("Analyzing Items...");

        var itemList = new List<Dictionary<string, object?>>();

        var itemType = module.Types.First(t => t.FullName == "Item");
        var itemInfoType = module.Types.First(t => t.FullName == "ItemInfo");

        if (itemType is null || itemInfoType is null)
            return Task.CompletedTask;

        var setupItemsMethod = itemType.Methods.First(m => m.Name == "SetupItems");
        var globalItemsField = itemType.Fields.First(m => m.Name == "GlobalItems");

        if (setupItemsMethod is null || globalItemsField is null)
            return Task.CompletedTask;


        var methodBody = setupItemsMethod.Body;
        var instructions = methodBody.Instructions;

        var haveItem = false;
        for (var i = 0; i < instructions.Count; i++)
        {
            if (haveItem)
            {
                haveItem = false;
                Console.WriteLine();
            }

            var instruction = instructions[i];

            if (instruction.OpCode.Name != "ldsfld") continue;
            if (instruction.Operand != globalItemsField)
                continue;

            var itemNameInstruction = instructions[++i];

            if (itemNameInstruction.OpCode.Name != "ldstr")
                continue;

            var itemName = (string)itemNameInstruction.Operand;

            var createItemInfoObjInstruction = instructions[++i];
            if (createItemInfoObjInstruction.OpCode.Name != "newobj")
                continue;

            if (createItemInfoObjInstruction.Operand != itemInfoType.Methods.First(p => p.IsConstructor))
                continue;

            var itemInfo = new Dictionary<string, object?>
            {
                ["name"] = itemName
            };

            while (true)
            {
                var innerInstruction = instructions[++i];

                if (innerInstruction.OpCode.Name == "callvirt")
                {
                    if (innerInstruction.Operand.ToString() ==
                        "System.Void System.Collections.Generic.Dictionary`2<System.String,ItemInfo>::Add(!0,!1)" ||
                        innerInstruction.Operand.ToString() ==
                        "System.Collections.Generic.Dictionary`2/Enumerator<!0,!1> System.Collections.Generic.Dictionary`2<System.String,ItemInfo>::GetEnumerator()")
                        break;
                }
                else if (innerInstruction.OpCode.Name != "dup")
                {
                    continue;
                }

                var valueOpcodes = new List<Instruction>();

                while (true)
                {
                    var suspectValueType = instructions[++i];

                    if (suspectValueType.Operand is FieldDefinition fieldDefinition)
                    {
                        if (fieldDefinition.DeclaringType.Name == "ItemInfo")
                        {
                            i -= 1;
                            break;
                        }
                    }

                    valueOpcodes.Add(suspectValueType);
                }

                var suspectField = instructions[++i];
                if (suspectField.Operand is FieldDefinition field)
                {
                    itemInfo[field.Name] = ParseOperand(field, valueOpcodes);
                }
            }

            itemList.Add(itemInfo);
            haveItem = true;
        }

        // Console.WriteLine($"Amount of item {itemList.Count}");
        // using (var file = File.OpenText("items.json"))
        // {
        //     file.
        // }

        File.WriteAllText("items.json", JsonSerializer.Serialize(itemList));
        return Task.CompletedTask;
    }
}