using CasualtiesMiner.Shared.Models;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;

namespace CasualtiesMiner.Dumper.Parsing;

internal static class MoodleCallParser
{
    // code looks so sophisticated beacuse moodles can't be parsed like
    // fields in big lists where you have every field stacked on top of eachother.
    // Moreover, some fields being calculated as arguments in function
    // so it just adds more complexity to the whole thing
    public static MoodleInfo? Parse(
        Collection<Instruction> instructions,
        MethodReference getMoodleMethod)
    {
        var callIndex = instructions.Count - 1;
        var argsStartIndex = FindAddMoodleArgsStartIndex(instructions, callIndex);
        if (argsStartIndex < 0)
        {
            return null;
        }

        var guards = new List<string>();

        for (var i = 0; i < argsStartIndex; i++)
        {
            var instruction = instructions[i];

            switch (instruction.OpCode.Code)
            {
                case Code.Ldfld:
                    if (instruction.Operand is not FieldReference field)
                    {
                        continue;
                    }

                    if (field.Name != "body"
                        || instruction.Previous?.OpCode.Code != Code.Ldarg_0)
                    {
                        continue;
                    }

                    if (TryFormatFieldCompare(instructions, callIndex, instruction.Previous, out var guardExpr))
                    {
                        guards.Add(guardExpr);
                    }

                    continue;

                default:
                    if (IsLocalLoad(instruction) && TryFormatLocalCompare(instructions, callIndex, instruction, out var localGuard))
                    {
                        guards.Add(localGuard);
                    }

                    continue;
            }
        }

        var cursor = argsStartIndex + 1;
        if (cursor >= callIndex)
        {
            return null;
        }

        // get ternary `intensity`, literal, or computed expression (RoundToInt, Clamp)
        if (!TryParseIntensity(instructions, cursor, callIndex, out var intensityText, out cursor))
        {
            return null;
        }

        int? intensity = null;
        string? intensityExpr = null;
        if (int.TryParse(intensityText, out var intensityValue))
        {
            intensity = intensityValue;
        }
        else
        {
            intensityExpr = intensityText;
        }

        if (cursor >= callIndex || instructions[cursor].OpCode.Code != Code.Ldstr)
        {
            return null;
        }

        // get `icon`
        var icon = (string)instructions[cursor].Operand!;
        cursor++;

        // get `locale`
        if (!TryParseLocaleKey(instructions, ref cursor, getMoodleMethod, callIndex, skipSuffix: false, out var localeId))
        {
            return null;
        }

        // get `locale desc` (may continue with .Replace(...))
        if (!TryParseLocaleKey(instructions, ref cursor, getMoodleMethod, callIndex, skipSuffix: true, out var descLocaleKey))
        {
            return null;
        }

        // get `critical` argument/expression
        var critical = false;
        string? criticalExpr = null;
        if (cursor < callIndex)
        {
            if (!TryParseBoolArg(instructions, cursor, out critical, out criticalExpr, out cursor))
            {
                return null;
            }
        }

        var chippedOnly = false;
        if (cursor < callIndex && ILInstructionParser.IsLdcI4(instructions[cursor]))
        {
            chippedOnly = ILInstructionParser.ParseInt(instructions[cursor]) != 0;
        }

        Console.WriteLine($"icon: {icon}");
        Console.WriteLine($"localeId: {localeId}");
        Console.WriteLine($"descLocaleKey: {descLocaleKey}");
        Console.WriteLine($"preconditionForMoodle: {(string.IsNullOrEmpty(string.Join(", ", guards)) ? "none" : string.Join(", ", guards))}");
        Console.WriteLine($"intensity: {intensityExpr ?? intensity.ToString()}");
        Console.WriteLine($"critical: {criticalExpr ?? critical.ToString()}");
        Console.WriteLine($"chippedOnly: {chippedOnly}\n");

        return new MoodleInfo
        {
            icon = icon,
            localeId = localeId,
            descLocaleKey = descLocaleKey,
            preconditionForMoodle = string.IsNullOrEmpty(string.Join(", ", guards)) ? "none" : string.Join(", ", guards),
            intensity = intensity,
            intensityExpr = intensityExpr,
            critical = critical,
            criticalExpr = criticalExpr,
            chippedOnly = chippedOnly
        };
    }

    private static bool TryParseLocaleKey(
        Collection<Instruction> instructions,
        ref int cursor,
        MethodReference getMoodleMethod,
        int callIndex,
        bool skipSuffix,
        out string localeKey)
    {
        localeKey = "";

        if (cursor >= instructions.Count || instructions[cursor].OpCode.Code != Code.Ldstr)
        {
            return false;
        }

        localeKey = (string)instructions[cursor].Operand!;
        cursor++;

        if (cursor < instructions.Count
            && IsGetMoodleCall(instructions[cursor], getMoodleMethod))
        {
            cursor++;

            if (skipSuffix)
            {
                SkipDescExpressionRemainder(instructions, ref cursor, callIndex);
            }
        }

        return true;
    }

    // skips IL after GetMoodle that builds the final desc string (like .Replace(...)).
    private static void SkipDescExpressionRemainder(
        Collection<Instruction> instructions,
        ref int cursor,
        int callIndex)
    {
        var end = FindIndexBeforeTrailingBoolArgs(instructions, callIndex);
        if (end > cursor)
        {
            cursor = end;
        }
    }

    private static int FindIndexBeforeTrailingBoolArgs(Collection<Instruction> instructions, int callIndex)
    {
        var idx = callIndex - 1;
        if (idx < 0)
        {
            return 0;
        }

        if (ILInstructionParser.IsLdcI4(instructions[idx]))
        {
            idx--;
        }

        if (idx >= 0)
        {
            if (ILInstructionParser.IsLdcI4(instructions[idx]))
            {
                idx--;
            }
            else if (IsCriticalExprStart(instructions, idx))
            {
                ILInstructionParser.ConsumeOne(instructions, ref idx);
            }
        }

        return idx + 1;
    }

    private static bool TryParseBoolArg(
        Collection<Instruction> instructions,
        int startIndex,
        out bool value,
        out string? expression,
        out int endExclusive)
    {
        value = false;
        expression = null;

        if (TryParseFieldCompareBool(instructions, startIndex, out var compareExpr, out endExclusive))
        {
            expression = compareExpr;

            return true;
        }

        if (startIndex < instructions.Count && ILInstructionParser.IsLdcI4(instructions[startIndex]))
        {
            value = ILInstructionParser.ParseInt(instructions[startIndex]) != 0;
            endExclusive = startIndex + 1;

            return true;
        }

        return false;
    }

    private static bool TryParseFieldCompareBool(
        Collection<Instruction> instructions,
        int startIndex,
        out string expression,
        out int endExclusive)
    {
        expression = "";
        endExclusive = startIndex;

        if (startIndex >= instructions.Count)
        {
            return false;
        }

        if (!TryReadFieldChain(instructions[startIndex], out var path, out var leaf, out var afterChain))
        {
            return false;
        }

        if (afterChain is null || !TryFormatLiteral(afterChain, leaf, out var literal))
        {
            return false;
        }

        var compare = afterChain.Next;
        if (compare is null || !IsStackCompareInsn(compare.OpCode.Code))
        {
            return false;
        }

        var op = StackCompareOperator(compare.OpCode.Code);
        if (op.Length == 0)
        {
            return false;
        }

        var compareIndex = IndexOf(instructions, compare);
        if (compareIndex < 0)
        {
            return false;
        }

        expression = $"{path} {op} {literal}";
        endExclusive = compareIndex + 1;

        return true;
    }

    private static bool IsGetMoodleCall(Instruction instruction, MethodReference getMoodleMethod)
    {
        if (instruction.OpCode.Code is not (Code.Call or Code.Callvirt))
        {
            return false;
        }

        if (instruction.Operand is not MethodReference called)
        {
            return false;
        }

        return called.Name == getMoodleMethod.Name
               && called.DeclaringType.FullName == getMoodleMethod.DeclaringType.FullName;
    }

    private static bool TryParseIntensity(
        Collection<Instruction> instructions,
        int startIndex,
        int callIndex,
        out string expression,
        out int endExclusive)
    {
        if (TryParseTernary(instructions, startIndex, out expression, out endExclusive))
        {
            return true;
        }

        if (startIndex < instructions.Count && ILInstructionParser.IsLdcI4(instructions[startIndex]))
        {
            expression = ILInstructionParser.ParseInt(instructions[startIndex]).ToString();
            endExclusive = startIndex + 1;
            return true;
        }

        return TryParseComputedIntensity(instructions, startIndex, callIndex, out expression, out endExclusive);
    }

    private static bool TryParseComputedIntensity(
        Collection<Instruction> instructions,
        int startIndex,
        int callIndex,
        out string expression,
        out int endExclusive)
    {
        expression = "";
        endExclusive = startIndex;

        var iconIndex = FindIconIndex(instructions, startIndex, callIndex);
        if (iconIndex <= startIndex)
        {
            return false;
        }

        var slice = new Instruction[iconIndex - startIndex];
        for (var i = 0; i < slice.Length; i++)
        {
            slice[i] = instructions[startIndex + i];
        }

        if (!ILComplexExpressionParser.TryFormat(slice, out expression))
        {
            return false;
        }

        endExclusive = iconIndex;
        return true;
    }

    private static int FindIconIndex(Collection<Instruction> instructions, int startIndex, int callIndex)
    {
        for (var i = startIndex; i < callIndex; i++)
        {
            if (instructions[i].OpCode.Code == Code.Ldstr)
            {
                return i;
            }
        }

        return -1;
    }

    private static bool TryParseTernaryOrLiteral(
        Collection<Instruction> instructions,
        int startIndex,
        out string expression,
        out int endExclusive)
    {
        if (TryParseTernary(instructions, startIndex, out expression, out endExclusive))
        {
            return true;
        }

        if (startIndex < instructions.Count && ILInstructionParser.IsLdcI4(instructions[startIndex]))
        {
            expression = ILInstructionParser.ParseInt(instructions[startIndex]).ToString();
            endExclusive = startIndex + 1;
            return true;
        }

        expression = "";
        endExclusive = startIndex;

        return false;
    }

    private static bool TryParseTernary(
        Collection<Instruction> instructions,
        int startIndex,
        out string expression,
        out int endExclusive)
    {
        expression = "";
        endExclusive = startIndex;

        if (startIndex >= instructions.Count)
        {
            return false;
        }

        if (!TryReadTernaryCondition(instructions[startIndex], out var condition, out var branchInsn))
        {
            return false;
        }

        var branchIndex = IndexOf(instructions, branchInsn);
        if (branchIndex < 0 || branchIndex + 1 >= instructions.Count)
        {
            return false;
        }

        var falseStart = branchIndex + 1;
        if (!TryParseTernaryOrLiteral(instructions, falseStart, out var whenFalse, out var afterFalse))
        {
            return false;
        }

        if (afterFalse >= instructions.Count)
        {
            return false;
        }

        var skipInsn = instructions[afterFalse];
        if (skipInsn.OpCode.Code is not (Code.Br or Code.Br_S)
            || skipInsn.Operand is not Instruction mergeLabel)
        {
            return false;
        }

        if (branchInsn.Operand is not Instruction trueLabel)
        {
            return false;
        }

        var trueStart = IndexOf(instructions, trueLabel);
        if (trueStart < 0)
        {
            return false;
        }

        if (!TryParseTernaryOrLiteral(instructions, trueStart, out var whenTrue, out var afterTrue))
        {
            return false;
        }

        var mergeIndex = IndexOf(instructions, mergeLabel);
        if (mergeIndex < 0 || afterTrue != mergeIndex)
        {
            return false;
        }

        expression = $"({condition}) ? {WrapNested(whenTrue)} : {WrapNested(whenFalse)}";
        endExclusive = mergeIndex;

        return true;
    }

    private static bool TryReadTernaryCondition(
        Instruction start,
        out string condition,
        out Instruction branchInsn)
    {
        condition = "";
        branchInsn = null!;

        if (!TryReadFieldChain(start, out var path, out var leaf, out var afterChain))
        {
            return false;
        }

        if (afterChain is null || !TryFormatLiteral(afterChain, leaf, out var literal))
        {
            return false;
        }

        var branch = afterChain.Next;
        if (branch is null || branch.Operand is not Instruction)
        {
            return false;
        }

        var op = BranchOperatorWhenTaken(branch.OpCode.Code);
        if (op.Length == 0)
        {
            return false;
        }

        branchInsn = branch;
        condition = $"{path} {op} {literal}";
        return true;
    }

    private static bool TryFormatFieldCompare(
        Collection<Instruction> instructions,
        int callIndex,
        Instruction start,
        out string expression)
    {
        expression = "";

        if (!TryReadFieldChain(start, out var path, out var leaf, out var afterChain))
        {
            return false;
        }

        if (!TryReadCompare(instructions, callIndex, afterChain, leaf, out var op, out var literal))
        {
            return false;
        }

        expression = $"{path} {op} {literal}";

        return true;
    }

    private static bool TryReadFieldChain(
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

    private static bool TryReadCompare(
        Collection<Instruction> instructions,
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

    private static bool TryFormatLiteral(Instruction instruction, FieldReference? leaf, out string literal)
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

    private static bool TryFormatLocalCompare(
        Collection<Instruction> instructions,
        int callIndex,
        Instruction start,
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

        expression = $"{FormatLocalName(start)} {op} {literal}";

        return true;
    }

    private static string FormatLocalName(Instruction instruction) =>
        instruction.OpCode.Code switch
        {
            Code.Ldloc_0 => "var0",
            Code.Ldloc_1 => "var1",
            Code.Ldloc_2 => "var2",
            Code.Ldloc_3 => "var3",
            Code.Ldloc or Code.Ldloc_S => $"var{((VariableDefinition)instruction.Operand!).Index}",
            _ => "?"
        };

    private static int FindAddMoodleArgsStartIndex(Collection<Instruction> instructions, int callIndex)
    {
        // receiver is the last ldarg.0 before call whose next when starts loading args
        // (not ldarg.0 → ldfld, which is a guard or nested field read).
        for (var i = callIndex - 1; i >= 0; i--)
        {
            if (instructions[i].OpCode.Code != Code.Ldarg_0)
            {
                continue;
            }

            if (i + 1 < callIndex && instructions[i + 1].OpCode.Code == Code.Ldfld)
            {
                continue;
            }

            if (i > 0 && instructions[i - 1].OpCode.Code == Code.Ldarg_0)
            {
                return i - 1;
            }

            return i;
        }

        return -1;
    }

    private static int IndexOf(Collection<Instruction> instructions, Instruction instruction)
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

    private static bool IsCriticalExprStart(Collection<Instruction> instructions, int cursor) =>
        cursor + 1 < instructions.Count
            && instructions[cursor].OpCode.Code == Code.Ldarg_0
            && instructions[cursor + 1].OpCode.Code == Code.Ldfld;

    private static bool IsLocalLoad(Instruction instruction) =>
        instruction.OpCode.Code switch
        {
            Code.Ldloc_0 or Code.Ldloc_1 or Code.Ldloc_2 or Code.Ldloc_3 or Code.Ldloc or Code.Ldloc_S => true,
            _ => false
        };

    private static bool IsStackCompareInsn(Code code) =>
        code is Code.Ceq or Code.Cgt or Code.Clt or Code.Cgt_Un or Code.Clt_Un;

    private static string StackCompareOperator(Code code) =>
        code switch
        {
            Code.Clt or Code.Clt_Un => "<",
            Code.Cgt or Code.Cgt_Un => ">",
            Code.Ceq => "==",
            _ => ""
        };

    // jump target at or before AddMoodle → moodle is added when the branch is taken.
    // jump target after AddMoodle (or outside the frame) -> moodle is added on fall-through.
    private static string BranchOperatorForGuard(
        Collection<Instruction> instructions,
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

    // operator for code that runs when the branch is not taken (fall-through into AddMoodle).
    private static string BranchOperatorOnFallThrough(Code code) =>
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

    private static string BranchOperatorWhenTaken(Code code) =>
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

    private static string WrapNested(string value) => value.Contains('?') ? $"({value})" : value;

    private static string FormatFloatLiteral(float value) => $"{value:G}f";
}
