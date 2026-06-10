using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;

namespace CasualtiesMiner.Dumper.Parsing;

internal static class ILInstructionFormat
{
    public static void WriteMethodIl(TextWriter writer, MethodDefinition method, bool markAddMoodleCalls = false)
    {
        if (method.Body is null)
        {
            writer.WriteLine($"// {method.FullName} — no method body");
            return;
        }

        var instructions = method.Body.Instructions;
        writer.WriteLine($"// {method.FullName}");
        writer.WriteLine($"// {instructions.Count} instructions");
        writer.WriteLine();

        for (var i = 0; i < instructions.Count; i++)
        {
            var instruction = instructions[i];
            var marker = markAddMoodleCalls && MoodleStackWalker.IsInstructionMoodleCall(instruction, out _)
                ? " >>> AddMoodle"
                : "";
            writer.WriteLine($"IL_{i:X4} @{instruction.Offset:X4}: {instruction}{marker}");
        }
    }

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
