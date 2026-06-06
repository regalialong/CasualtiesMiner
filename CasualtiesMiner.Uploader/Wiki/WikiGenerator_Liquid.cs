using CasualtiesMiner.Uploader.Data;
using System.Text;

namespace CasualtiesMiner.Uploader.Wiki;

/// <summary>
/// Generates <c>Module:Liquid/data</c> for bulk Bucket upload.
/// </summary>
public static partial class WikiGenerator
{
    public static string BuildLiquidDataModule(IReadOnlyList<LiquidRow> rows)
    {
        var sb = new StringBuilder();
        sb.AppendLine(GeneratedHeader);
        sb.AppendLine("return {");

        foreach (var row in rows)
        {
            sb.AppendLine("  {");

            foreach (var (key, value) in EnumerateLiquidFields(row))
            {
                sb.Append("    ").Append(key).Append(" = ").Append(value).AppendLine(",");
            }

            sb.AppendLine("  },");
        }

        sb.AppendLine("}");
        return sb.ToString();
    }

    private static IEnumerable<(string Key, string Value)> EnumerateLiquidFields(LiquidRow row)
    {
        yield return ("item_id", LuaFormat.String(row.ItemId));
        yield return ("page", LuaFormat.String(row.PageTitle));
    }
}
