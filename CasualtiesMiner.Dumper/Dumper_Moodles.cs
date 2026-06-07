using CasualtiesMiner.Dumper.Parsing;
using CasualtiesMiner.Shared.Models;

namespace CasualtiesMiner.Dumper;

public sealed partial class Dumper
{
    public MoodleInfo[] DumpMoodles()
    {
        var moodleManager = _module.Types.FirstOrDefault(t => t.FullName == "MoodleManager");
        if (moodleManager is null)
            return [];

        var addAllMoodles = moodleManager.Methods.FirstOrDefault(m => m.Name == "AddAllMoodles");
        if (addAllMoodles?.Body is null)
            return [];

        var localeType = _module.Types.FirstOrDefault(t => t.FullName == "Locale");
        var getMoodle = localeType?.Methods.FirstOrDefault(m => m is { Name: "GetMoodle", Parameters.Count: 1 });
        if (getMoodle is null)
            return [];

        var moodles = new List<MoodleInfo>();
        var instructions = addAllMoodles.Body.Instructions;

        for (var i = 0; i < instructions.Count; i++)
        {
            if (!MoodleCallParser.IsAddMoodleCall(instructions[i], out var method))
            {
                continue;
            }

            if (method!.Parameters.Count != 6)
            {
                Console.WriteLine($"[WARNING] Parameters for AddModdle was bigger than 6, skipping...");

                continue;
            }

            var moodle = MoodleCallParser.Parse(instructions, i, getMoodle);

            if (moodle is not null)
            {
                moodles.Add(moodle);
            }
        }

        return [.. moodles.OrderBy(m => m.localeId, StringComparer.Ordinal)];
    }
}
