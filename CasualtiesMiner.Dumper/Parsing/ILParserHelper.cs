using Mono.Cecil;
using Mono.Cecil.Cil;

namespace CasualtiesMiner.Dumper.Parsing;

internal static class ILParserHelper
{
    public static int IndexOf(IList<Instruction> instructions, Instruction instruction)
    {
        for (var i = 0; i < instructions.Count; i++)
        {
            if (ReferenceEquals(instructions[i], instruction))
            {
                return i;
            }
        }

        return -1;
    }

    public static bool IsLocalLoad(Instruction instruction) =>
        instruction.OpCode.Code switch
        {
            Code.Ldloc_0 or Code.Ldloc_1 or Code.Ldloc_2 or Code.Ldloc_3 or Code.Ldloc or Code.Ldloc_S => true,
            _ => false
        };

    public static bool TryGetLocalLoadIndex(Instruction instruction, out int localIndex)
    {
        localIndex = instruction.OpCode.Code switch
        {
            Code.Ldloc_0 => 0,
            Code.Ldloc_1 => 1,
            Code.Ldloc_2 => 2,
            Code.Ldloc_3 => 3,
            Code.Ldloc or Code.Ldloc_S => ((VariableDefinition)instruction.Operand!).Index,
            _ => -1
        };

        return localIndex >= 0;
    }

    public static bool TryGetLocalStoreIndex(Instruction instruction, out int localIndex)
    {
        localIndex = -1;

        switch (instruction.OpCode.Code)
        {
            case Code.Stloc_0:
                localIndex = 0;
                return true;
            case Code.Stloc_1:
                localIndex = 1;
                return true;
            case Code.Stloc_2:
                localIndex = 2;
                return true;
            case Code.Stloc_3:
                localIndex = 3;
                return true;
            case Code.Stloc or Code.Stloc_S:
                localIndex = ((VariableDefinition)instruction.Operand!).Index;
                return true;
            default:
                return false;
        }
    }

    public static string FormatLocalName(
        Instruction instruction,
        IReadOnlyDictionary<int, string> localPaths)
    {
        if (!TryGetLocalLoadIndex(instruction, out var index))
        {
            return "?";
        }

        if (localPaths.TryGetValue(index, out var path))
        {
            return path;
        }

        return $"var{index}";
    }

    public static bool TryReadFieldChain(
        Instruction start,
        out string path,
        out FieldReference? leaf,
        out Instruction? afterChain)
    {
        path = "";
        leaf = null;
        afterChain = null;

        var ins = start;
        if (ins.OpCode.Code == Code.Ldarg_0)
        {
            ins = ins.Next;
        }

        if (ins is null)
        {
            return false;
        }

        var parts = new List<string>();

        while (ins is { OpCode.Code: Code.Ldfld, Operand: FieldReference field })
        {
            parts.Add(field.Name);
            leaf = field;
            ins = ins.Next;
        }

        if (parts.Count == 0)
        {
            return false;
        }

        path = string.Join(".", parts);
        afterChain = ins;

        return true;
    }

    public static bool IsStackCompare(Code code) =>
        code is Code.Ceq or Code.Cgt or Code.Clt or Code.Cgt_Un or Code.Clt_Un;

    public static string StackCompareOperator(Code code) =>
        code switch
        {
            Code.Clt or Code.Clt_Un => "<",
            Code.Cgt or Code.Cgt_Un => ">",
            Code.Ceq => "==",
            _ => ""
        };

    public static bool IsFloatCompare(Code code) =>
        code is Code.Cgt or Code.Cgt_Un or Code.Clt or Code.Clt_Un or Code.Ceq;

    public static string FloatCompareOperator(Code code) =>
        code switch
        {
            Code.Cgt or Code.Cgt_Un => ">",
            Code.Clt or Code.Clt_Un => "<",
            Code.Ceq => "==",
            _ => ""
        };

    public static bool IsConditionalBranch(Instruction instruction) =>
        IsConditionalBranch(instruction.OpCode.Code);

    public static bool IsConditionalBranch(Code code) =>
        code switch
        {
            Code.Bgt or Code.Bgt_S or Code.Bgt_Un or Code.Bgt_Un_S => true,
            Code.Blt or Code.Blt_S or Code.Blt_Un or Code.Blt_Un_S => true,
            Code.Bge or Code.Bge_S or Code.Bge_Un or Code.Bge_Un_S => true,
            Code.Ble or Code.Ble_S or Code.Ble_Un or Code.Ble_Un_S => true,
            Code.Beq or Code.Beq_S => true,
            Code.Bne_Un or Code.Bne_Un_S => true,
            _ => false
        };

    public static bool IsUnconditionalBranch(Instruction instruction) =>
        instruction.OpCode.Code is Code.Br or Code.Br_S;

    public static string BranchOperatorWhenTaken(Code code) =>
        code switch
        {
            Code.Bgt or Code.Bgt_S or Code.Bgt_Un or Code.Bgt_Un_S => ">",
            Code.Blt or Code.Blt_S or Code.Blt_Un or Code.Blt_Un_S => "<",
            Code.Bge or Code.Bge_S or Code.Bge_Un or Code.Bge_Un_S => ">=",
            Code.Ble or Code.Ble_S or Code.Ble_Un or Code.Ble_Un_S => "<=",
            Code.Beq or Code.Beq_S => "==",
            Code.Bne_Un or Code.Bne_Un_S => "!=",
            _ => ""
        };

    public static string BranchOperatorOnFallThrough(Code code) =>
        code switch
        {
            Code.Bgt or Code.Bgt_S or Code.Bgt_Un or Code.Bgt_Un_S => "<=",
            Code.Blt or Code.Blt_S or Code.Blt_Un or Code.Blt_Un_S => ">=",
            Code.Bge or Code.Bge_S or Code.Bge_Un or Code.Bge_Un_S => "<",
            Code.Ble or Code.Ble_S or Code.Ble_Un or Code.Ble_Un_S => ">",
            Code.Beq or Code.Beq_S => "!=",
            Code.Bne_Un or Code.Bne_Un_S => "==",
            _ => ""
        };

    public static string InvertBranchOperatorWhenTaken(Code code) =>
        code switch
        {
            Code.Bgt or Code.Bgt_S or Code.Bgt_Un or Code.Bgt_Un_S => "<=",
            Code.Blt or Code.Blt_S or Code.Blt_Un or Code.Blt_Un_S => ">=",
            Code.Bge or Code.Bge_S or Code.Bge_Un or Code.Bge_Un_S => "<",
            Code.Ble or Code.Ble_S or Code.Ble_Un or Code.Ble_Un_S => ">",
            Code.Beq or Code.Beq_S => "!=",
            Code.Bne_Un or Code.Bne_Un_S => "==",
            _ => ""
        };

    public static string BranchOperatorForGuard(
        IList<Instruction> instructions,
        Instruction branch,
        int callIndex)
    {
        if (branch.Operand is Instruction target)
        {
            var targetIndex = IndexOf(instructions, target);
            if (targetIndex >= 0 && targetIndex <= callIndex)
            {
                return BranchOperatorWhenTaken(branch.OpCode.Code);
            }
        }

        return BranchOperatorOnFallThrough(branch.OpCode.Code);
    }

    public static bool TryFormatFieldCompare(
        IList<Instruction> instructions,
        int callIndex,
        Instruction start,
        out string expression)
    {
        expression = "";

        if (!TryReadFieldChain(start, out var path, out var leaf, out var afterChain))
        {
            return false;
        }

        if (!TryReadFieldCompare(instructions, callIndex, afterChain, leaf, out var op, out var literal))
        {
            return false;
        }

        expression = $"{path} {op} {literal}";

        return true;
    }

    public static bool TryFormatLocalCompare(
        IList<Instruction> instructions,
        int callIndex,
        Instruction start,
        IReadOnlyDictionary<int, string> localPaths,
        out string expression)
    {
        expression = "";

        var literalInsn = start.Next;
        if (literalInsn is null || !TryFormatLiteral(literalInsn, null, out var literal))
        {
            return false;
        }

        var branch = literalInsn.Next;
        if (branch is null)
        {
            return false;
        }

        var op = BranchOperatorForGuard(instructions, branch, callIndex);
        if (op.Length == 0)
        {
            return false;
        }

        expression = $"{FormatLocalName(start, localPaths)} {op} {literal}";

        return true;
    }

    private static bool TryReadFieldCompare(
        IList<Instruction> instructions,
        int callIndex,
        Instruction? afterChain,
        FieldReference? leaf,
        out string op,
        out string literal)
    {
        op = "";
        literal = "";

        if (afterChain is null || !TryFormatLiteral(afterChain, leaf, out literal))
        {
            return false;
        }

        var branch = afterChain.Next;
        if (branch is null)
        {
            return false;
        }

        op = BranchOperatorForGuard(instructions, branch, callIndex);

        return op.Length > 0;
    }

    public static bool TryFormatLiteral(Instruction instruction, FieldReference? leaf, out string literal)
    {
        literal = "";

        switch (instruction.OpCode.Code)
        {
            case Code.Ldc_R4:
                literal = FormatFloatLiteral((float)instruction.Operand!);
                return true;
            case Code.Ldc_R8:
                literal = FormatFloatLiteral((float)(double)instruction.Operand!);
                return true;
        }

        if (!ILInstructionParser.IsLdcI4(instruction))
        {
            return false;
        }

        var value = ILInstructionParser.ParseInt(instruction);
        literal = leaf?.FieldType.FullName == "System.Single"
            ? FormatFloatLiteral(value)
            : value.ToString();

        return true;
    }

    public static bool TryParseFieldTruthyGuard(
        IList<Instruction> instructions,
        int index,
        out string path,
        out int endExclusive)
    {
        path = "";
        endExclusive = index;

        if (index >= instructions.Count)
        {
            return false;
        }

        var i = index;
        if (instructions[i].OpCode.Code == Code.Ldarg_0)
        {
            i++;
        }

        var parts = new List<string>();
        while (i < instructions.Count && instructions[i].OpCode.Code == Code.Ldfld)
        {
            parts.Add(((FieldReference)instructions[i].Operand!).Name);
            i++;
        }

        if (parts.Count == 0)
        {
            return false;
        }

        i = SkipTruthyConversions(instructions, i);
        if (i >= instructions.Count)
        {
            return false;
        }

        if (instructions[i].OpCode.Code is not (Code.Brfalse or Code.Brfalse_S))
        {
            return false;
        }

        path = string.Join(".", parts);
        endExclusive = i + 1;

        return true;
    }

    private static int SkipTruthyConversions(IList<Instruction> instructions, int index)
    {
        while (index < instructions.Count)
        {
            switch (instructions[index].OpCode.Code)
            {
                case Code.Box:
                case Code.Unbox_Any:
                case Code.Unbox:
                case Code.Castclass:
                case Code.Isinst:
                case Code.Conv_I1:
                case Code.Conv_U1:
                case Code.Conv_I4:
                case Code.Conv_I8:
                    index++;
                    continue;
                case Code.Call:
                case Code.Callvirt:
                    if (instructions[index].Operand is MethodReference method
                        && method.ReturnType.FullName == "System.Boolean"
                        && method.Parameters.Count == 1)
                    {
                        index++;
                        continue;
                    }

                    return index;
                default:
                    return index;
            }
        }

        return index;
    }

    public static string FormatFloatLiteral(float value) => $"{value:G}f";
}
