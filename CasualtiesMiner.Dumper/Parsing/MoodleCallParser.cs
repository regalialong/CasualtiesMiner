using CasualtiesMiner.Shared.Models;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;

namespace CasualtiesMiner.Dumper.Parsing;

internal static class MoodleCallParser
{
    public static bool IsAddMoodleCall(Instruction instruction, out MethodReference? method)
    {
        method = null;

        if (instruction.OpCode.Code is not (Code.Call or Code.Callvirt))
        {
            return false;
        }

        if (instruction.Operand is not MethodReference candidate)
        {
            return false;
        }

        if (candidate.Name != "AddMoodle" || candidate.DeclaringType.Name != "MoodleManager")
        {
            return false;
        }

        method = candidate;

        return true;
    }

    public static MoodleInfo? Parse(
        Collection<Instruction> instructions,
        int callIndex,
        MethodReference getMoodleMethod)
    {
        var idx = callIndex - 1;

        if (idx < 0 || !ILBackwardParser.IsBoolLiteral(instructions[idx], out var chippedOnly))
        {
            Console.WriteLine($"[WARNING] Could not parse chippedOnly for AddMoodle at IL_{callIndex:X4}.");
            return null;
        }

        idx--;

        string? criticalExpr;
        if (ILBackwardParser.IsBoolLiteral(instructions[idx], out bool critical))
        {
            idx--;
            criticalExpr = null;
        }
        else
        {
            var criticalEnd = idx;

            ILBackwardParser.ConsumeOne(instructions, ref idx);

            critical = false;
            criticalExpr = ILInstructionFormat.FormatSlice(instructions, idx + 1, criticalEnd);
        }

        var descEnd = idx;
        ILBackwardParser.ConsumeOne(instructions, ref idx);
        var descLocaleKey = ExtractLocaleKey(instructions, idx + 1, descEnd, getMoodleMethod);

        var nameEnd = idx;
        ILBackwardParser.ConsumeOne(instructions, ref idx);
        var localeId = ExtractLocaleKey(instructions, idx + 1, nameEnd, getMoodleMethod);
        if (localeId is null)
        {
            Console.WriteLine($"[WARNING] Could not parse locale id for AddMoodle at IL_{callIndex:X4}.");
            return null;
        }

        if (idx < 0 || instructions[idx].OpCode.Code != Code.Ldstr)
        {
            Console.WriteLine($"[WARNING] Could not parse icon for AddMoodle '{localeId}' at IL_{callIndex:X4}.");
            return null;
        }

        var icon = (string)instructions[idx].Operand!;
        var intensityEnd = idx - 1;
        idx--;

        if (intensityEnd < 0)
        {
            Console.WriteLine($"[WARNING] Missing intensity for AddMoodle '{localeId}' at IL_{callIndex:X4}.");
            return null;
        }

        ILBackwardParser.ConsumeOne(instructions, ref idx);
        var intensityStart = idx + 1;
        var intensityBlock = new List<Instruction>();

        for (var i = intensityStart; i <= intensityEnd; i++)
        {
            intensityBlock.Add(instructions[i]);
        }

        var intensity = ILBackwardParser.TryParseSingleLiteralInt(intensityBlock);
        var intensityExpr = intensity is null && intensityBlock.Count > 0
            ? ILInstructionFormat.FormatBlock(intensityBlock)
            : null;

        return new MoodleInfo
        {
            localeId = localeId,
            descLocaleKey = descLocaleKey,
            icon = icon,
            intensity = intensity,
            intensityExpr = intensityExpr,
            critical = critical,
            criticalExpr = criticalExpr,
            chippedOnly = chippedOnly
        };
    }

    private static string? ExtractLocaleKey(
        Collection<Instruction> instructions,
        int startInclusive,
        int endInclusive,
        MethodReference getMoodleMethod)
    {
        for (var i = endInclusive; i > startInclusive; i--)
        {
            if (!IsGetMoodleCall(instructions[i], getMoodleMethod))
            {
                continue;
            }

            if (instructions[i - 1].OpCode.Code == Code.Ldstr)
            {
                return (string)instructions[i - 1].Operand!;
            }
        }

        for (var i = endInclusive; i >= startInclusive; i--)
        {
            if (instructions[i].OpCode.Code == Code.Ldstr)
            {
                return (string)instructions[i].Operand!;
            }
        }

        return null;
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
}
