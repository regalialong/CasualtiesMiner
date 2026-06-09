using Mono.Cecil.Cil;
using Mono.Collections.Generic;

namespace CasualtiesMiner.Dumper.Parsing;

internal static class ILInstructionFormat
{
    public static string FormatSlice(Collection<Instruction> instructions, int startInclusive, int endInclusive)
    {
        if (startInclusive > endInclusive)
        {
            return "";
        }

        var parts = new List<string>();
        for (var i = startInclusive; i <= endInclusive; i++)
        {
            parts.Add(instructions[i].ToString());
        }

        return string.Join("\n", parts);
    }

    public static string FormatBlock(IReadOnlyList<Instruction> instructions)
    {
        if (instructions.Count == 0)
        {
            return "";
        }

        return string.Join("\n", instructions.Select(i => i.ToString()));
    }
}
