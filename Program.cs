using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

public class Program
{
    public async static Task Main(string[] args)
    {
        var fileName = args.Length > 0 ? args[0] : "Assembly-CSharp.dll";


        if (!File.Exists(fileName))
        {
            Console.WriteLine($"Can't find {fileName}");
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

    // Increase and return new value
    // Why do I need this???
    public static int IR(ref int input)
    {
        input += 1;
        return input;
    }

    public static object quickAndDirtyOperandParser(Instruction instruction)
    {
        switch (instruction.OpCode.Name)
        {
            case "ldc.i4.0":
                return 0;
            case "ldc.i4.1":
                return 1;
            case "ldc.i4.2":
                return 2;
            case "ldc.i4.3":
                return 3;
            case "ldc.i4.4":
                return 4;
            case "ldc.i4.5":
                return 5;
            case "ldc.i4.6":
                return 6;
            case "ldc.i4.7":
                return 7;
            case "ldc.i4.8":
                return 8;
            default:
                return instruction.Operand;
        }
    }


    public async static Task AnalyzeItems(ModuleDefinition module)
    {
        Console.WriteLine("Analyzing Items...");

        var itemList = new List<string>();

        var itemType = module.Types.First(t => t.FullName == "Item");
        var itemInfoType = module.Types.First(t => t.FullName == "ItemInfo");

        if (itemType is null || itemInfoType is null)
            return;

        var setupItemsMethod = itemType.Methods.First(m => m.Name == "SetupItems");
        var globalItemsField = itemType.Fields.First(m => m.Name == "GlobalItems");

        if (setupItemsMethod is null || globalItemsField is null)
            return;


        var methodBody = setupItemsMethod.Body;
        var instructions = methodBody.Instructions;

        var haveItem = false;
        for (int i = 0; i < instructions.Count; i++)
        {
            if (haveItem)
            {
                haveItem = false;
                Console.WriteLine();
            }

            var instruction = instructions[i];

            if (instruction.OpCode.Name == "ldsfld")
            {
                if (instruction.Operand != globalItemsField)
                    continue;

                var itemNameInstruction = instructions[IR(ref i)];

                if (itemNameInstruction.OpCode.Name != "ldstr")
                    continue;

                var itemName = (string)itemNameInstruction.Operand;
                itemList.Add(itemName);

                var createItemInfoObjInstruction = instructions[IR(ref i)];
                if (createItemInfoObjInstruction.OpCode.Name != "newobj")
                    continue;

                if (createItemInfoObjInstruction.Operand != itemInfoType.Methods.First((p) => p.IsConstructor))
                    continue;

                Console.WriteLine($"Item: {itemName}");

                while (true)
                {
                    var innerInstruction = instructions[IR(ref i)];

                    if (innerInstruction.OpCode.Name == "callvirt")
                    {
                        // Console.WriteLine("AAA");
                        // Console.WriteLine(innerInstruction.Operand.ToString());
                        //                         Console.WriteLine("BBB");
                        if (innerInstruction.Operand.ToString() == "System.Void System.Collections.Generic.Dictionary`2<System.String,ItemInfo>::Add(!0,!1)" || innerInstruction.Operand.ToString() == "System.Collections.Generic.Dictionary`2/Enumerator<!0,!1> System.Collections.Generic.Dictionary`2<System.String,ItemInfo>::GetEnumerator()")
                            break;
                    }
                    else if (innerInstruction.OpCode.Name != "dup")
                    {
                        continue;
                    }

                    var valueOpcodes = new List<Instruction>();

                    while (true)
                    {
                        var suspectValueType = instructions[IR(ref i)];

                        if (suspectValueType.Operand is not null && suspectValueType.Operand is FieldDefinition)
                        {
                            var x = (FieldDefinition)suspectValueType.Operand;

                            if (x.DeclaringType.Name == "ItemInfo")
                            {
                                i -= 1;
                                break;
                            }
                        }

                        valueOpcodes.Add(suspectValueType);
                    }

                    // Do fuckeries like check if it is ctor or stuff like that

                    var suspectField = instructions[IR(ref i)];
                    if (suspectField.Operand is not null && suspectField.Operand is FieldDefinition)
                    {
                        var x = (FieldDefinition)suspectField.Operand;
                        Console.Write($"{x.DeclaringType.Name}.{x.Name}: ");
                        if (valueOpcodes.Count == 1)
                        {
                            Console.WriteLine(quickAndDirtyOperandParser(valueOpcodes[0]));
                        }
                        else
                        {
                            Console.WriteLine("Basically a list of Opcodes, so we need to write a parser for it :sob:");
                            // Console.WriteLine();
                            // foreach (var inst in valueOpcodes)
                            // {
                            //     Console.WriteLine(inst);
                            // }
                        }
                    }
                }

                haveItem = true;
            }
        }

        Console.WriteLine($"Amount of item {itemList.Count}");
        foreach(var item in itemList)
            Console.WriteLine(item);
    }
}