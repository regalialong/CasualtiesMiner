using CasualtiesMiner.Uploader.Data;
using System.Text;

namespace CasualtiesMiner.Uploader.Wiki;

/// <summary>
/// Generates <c>Module:Moodle/data</c> keyed by locale id.
/// </summary>
public static partial class WikiGenerator
{
    public static string BuildMoodleDataModule(IReadOnlyList<MoodleRow> rows)
    {
        var sb = new StringBuilder();
        sb.AppendLine(GeneratedHeader);
        sb.AppendLine("return {");

        foreach (var row in rows)
        {
            sb.Append("  ").Append(LuaFormat.TableKey(row.LocaleId)).AppendLine(" = {");

            foreach (var (key, value) in EnumerateMoodleFields(row))
            {
                sb.Append("    ").Append(key).Append(" = ").Append(value).AppendLine(",");
            }

            sb.AppendLine("  },");
        }

        sb.AppendLine("}");
        return sb.ToString();
    }

    private static IEnumerable<(string Key, string Value)> EnumerateMoodleFields(MoodleRow row)
    {
        yield return ("icon", LuaFormat.String(row.Icon));

        if (row.Intensity is int intensity)
        {
            yield return ("intensity", LuaFormat.Int(intensity));
        }
        else if (!string.IsNullOrWhiteSpace(row.IntensityExpr))
        {
            yield return ("intensity_expr", LuaFormat.String(row.IntensityExpr));
        }

        yield return ("critical", LuaFormat.Bool(row.Critical));

        if (!string.IsNullOrWhiteSpace(row.CriticalExpr))
        {
            yield return ("critical_expr", LuaFormat.String(row.CriticalExpr));
        }

        yield return ("chipped_only", LuaFormat.Bool(row.ChippedOnly));

        if (!string.IsNullOrWhiteSpace(row.DescLocaleKey))
        {
            yield return ("desc_locale_key", LuaFormat.String(row.DescLocaleKey));
        }
    }
}
