using CasualtiesMiner.Shared.Models;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;

namespace CasualtiesMiner.Dumper.Parsing;

internal static class MoodleStackWalker
{
    public static bool IsInstructionMoodleCall(Instruction instruction, out MethodReference? method)
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

    private static Collection<Instruction> Slice(Collection<Instruction> instructions, int start, int count)
    {
        var slice = new Collection<Instruction>(count);

        for (var i = start; i < start + count && i < instructions.Count; i++)
        {
            slice.Add(instructions[i]);
        }

        return slice;
    }

    public static IReadOnlyList<MoodleInfo> Walk(
        Collection<Instruction> instructions,
        MethodReference getMoodleMethod)
    {
        var moodles = new List<MoodleInfo>();
        var frameStartIndex = 0;

        for (var i = 0; i < instructions.Count; i++)
        {
            if (!IsInstructionMoodleCall(instructions[i], out _))
            {
                continue;
            }

            var frameLength = i - frameStartIndex + 1;
            var frame = Slice(instructions, frameStartIndex, frameLength);
            var callIndexInFrame = frame.Count - 1;

            if (TryParseCallFrame(frame, callIndexInFrame, getMoodleMethod, out var moodle))
            {
                moodles.Add(moodle);
            }
            else
            {
                Console.WriteLine(
                    $"[WARNING] Could not resolve AddMoodle at IL_{i:X4} (@{instructions[i].Offset:X4}).");
            }

            frameStartIndex = i + 1;
        }

        return moodles;
    }

    private static bool TryParseCallFrame(
        Collection<Instruction> frame,
        int callIndexInFrame,
        MethodReference getMoodleMethod,
        out MoodleInfo moodle)
    {
        moodle = null!;

        var parsed = MoodleCallParser.Parse(frame, getMoodleMethod);
        if (parsed is null)
        {
            return false;
        }

        moodle = parsed;
        return true;
    }
}
