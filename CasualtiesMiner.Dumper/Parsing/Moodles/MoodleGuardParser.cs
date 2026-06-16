using Mono.Cecil.Cil;
using Mono.Collections.Generic;

namespace CasualtiesMiner.Dumper.Parsing.Moodles;

internal static class MoodleGuardParser
{
    public static IReadOnlyList<string> ParseGuards(
        Collection<Instruction> instructions,
        int callIndex,
        int argsStartIndex,
        IReadOnlyDictionary<int, string> localPaths)
    {
        var guards = new List<string>();

        for (var i = 0; i < argsStartIndex;)
        {
            if (instructions[i].OpCode.Code == Code.Ldarg_0
                && TryParseShortCircuitAndGuard(instructions, i, out var andGuard, out var andNext))
            {
                guards.Add(andGuard);
                i = andNext;
                continue;
            }

            if (ILParserHelper.IsLocalLoad(instructions[i])
                && TryParseStackOrGuard(instructions, i, localPaths, out var stackOrGuard, out var stackOrNext))
            {
                guards.Add(stackOrGuard);
                i = stackOrNext;
                continue;
            }

            var instruction = instructions[i];

            if (instruction.OpCode.Code == Code.Ldarg_0
                && ILParserHelper.TryParseFieldTruthyGuard(instructions, i, out var truthyGuard, out var truthyNext))
            {
                guards.Add(truthyGuard);
                i = truthyNext;
                continue;
            }

            if (instruction.OpCode.Code == Code.Ldfld
                && instruction.Operand is Mono.Cecil.FieldReference field
                && field.Name == "body"
                && instruction.Previous?.OpCode.Code == Code.Ldarg_0
                && ILParserHelper.TryFormatFieldCompare(instructions, callIndex, instruction.Previous, out var guardExpr))
            {
                guards.Add(guardExpr);
            }
            else if (ILParserHelper.IsLocalLoad(instruction)
                     && ILParserHelper.TryFormatLocalCompare(instructions, callIndex, instruction, localPaths, out var localGuard))
            {
                guards.Add(localGuard);
            }

            i++;
        }

        return guards;
    }

    private static bool TryParseShortCircuitAndGuard(
        Collection<Instruction> instructions,
        int index,
        out string expression,
        out int endExclusive)
    {
        expression = "";
        endExclusive = index + 1;

        if (!ILParserHelper.TryParseFieldTruthyGuard(instructions, index, out var left, out var secondStart))
        {
            return false;
        }

        if (!ILParserHelper.TryParseFieldTruthyGuard(instructions, secondStart, out var right, out endExclusive))
        {
            return false;
        }

        expression = $"{left} && {right}";
        return true;
    }

    // num > 25f || flag  →  ldloc; ldc; cgt; ldloc; or; brfalse
    private static bool TryParseStackOrGuard(
        Collection<Instruction> instructions,
        int index,
        IReadOnlyDictionary<int, string> localPaths,
        out string expression,
        out int endExclusive)
    {
        expression = "";
        endExclusive = index + 1;

        if (!ILParserHelper.IsLocalLoad(instructions[index])
            || !ILParserHelper.TryFormatLiteral(instructions[index + 1], null, out var literal)
            || !ILParserHelper.IsFloatCompare(instructions[index + 2].OpCode.Code)
            || !ILParserHelper.IsLocalLoad(instructions[index + 3])
            || instructions[index + 4].OpCode.Code != Code.Or
            || instructions[index + 5].OpCode.Code is not (Code.Brfalse or Code.Brfalse_S)
            || index + 5 >= instructions.Count)
        {
            return false;
        }

        var leftOp = ILParserHelper.FloatCompareOperator(instructions[index + 2].OpCode.Code);
        if (leftOp.Length == 0)
        {
            return false;
        }

        var left = $"{ILParserHelper.FormatLocalName(instructions[index], localPaths)} {leftOp} {literal}";
        var right = ILParserHelper.FormatLocalName(instructions[index + 3], localPaths);
        expression = $"{left} || {right}";
        endExclusive = index + 6;

        return true;
    }
}
