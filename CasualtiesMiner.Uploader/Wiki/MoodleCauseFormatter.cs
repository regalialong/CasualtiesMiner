using CasualtiesMiner.Uploader.Data;
using System.Text.RegularExpressions;

namespace CasualtiesMiner.Uploader.Wiki;

public static partial class MoodleCauseFormatter
{
    [GeneratedRegex(@"^(.+?)\s*(>=|<=|>|<)\s*(.+)$", RegexOptions.CultureInvariant)]
    private static partial Regex ComparisonExpr();

    public static string? FormatPrecondition(string? precondition)
    {
        if (string.IsNullOrWhiteSpace(precondition) || precondition.Equals("none", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var clauses = precondition.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        var formatted = clauses.Select(FormatComparisonClause).Where(clause => clause.Length > 0).ToArray();

        return formatted.Length == 0 ? null : string.Join(",", formatted);
    }

    public static string? FormatIntensity(
        string? causeKind,
        string? bodyFieldId,
        IReadOnlyDictionary<string, BodyFieldRow> bodyFields,
        IReadOnlyDictionary<string, double> gameFields)
    {
        if (causeKind != "timer" || string.IsNullOrWhiteSpace(bodyFieldId))
        {
            return null;
        }

        if (!bodyFields.TryGetValue(bodyFieldId, out var bodyField))
        {
            return null;
        }

        if (!TryResolveTimerConstants(bodyField, gameFields, out var healSpeed, out var intensityScale, out var splintMultiplier))
        {
            return null;
        }

        return FormatTimerIntensityBands(bodyField.Label, healSpeed, intensityScale, splintMultiplier);
    }

    private static string FormatComparisonClause(string clause)
    {
        var match = ComparisonExpr().Match(clause.Trim());
        if (!match.Success)
        {
            return clause;
        }

        var field = match.Groups[1].Value.Trim();
        var op = FormatComparisonOp(match.Groups[2].Value);
        var value = FormatThreshold(match.Groups[3].Value.Trim());
        var label = WikiUiLabels.BodyFields.TryGetValue(field, out var localized) ? localized : field;

        return $"{label} {op} {value}";
    }

    private static string FormatComparisonOp(string op) => op switch
    {
        ">=" => "≥",
        "<=" => "≤",
        _ => op,
    };

    private static string FormatThreshold(string value) =>
        value.EndsWith("f", StringComparison.OrdinalIgnoreCase) ? value[..^1] : value;

    private static bool TryResolveTimerConstants(
        BodyFieldRow bodyField,
        IReadOnlyDictionary<string, double> gameFields,
        out double healSpeed,
        out double intensityScale,
        out double splintMultiplier)
    {
        healSpeed = 0;
        intensityScale = 0;
        splintMultiplier = 0;

        return bodyField.HealSpeedFieldId is not null
               && bodyField.IntensityScaleFieldId is not null
               && bodyField.SplintMultiplierFieldId is not null
               && gameFields.TryGetValue(bodyField.HealSpeedFieldId, out healSpeed)
               && gameFields.TryGetValue(bodyField.IntensityScaleFieldId, out intensityScale)
               && gameFields.TryGetValue(bodyField.SplintMultiplierFieldId, out splintMultiplier);
    }

    private static string FormatTimerIntensityBands(
        string label,
        double healSpeed,
        double intensityScale,
        double splintMultiplier)
    {
        var thresholds = new double[4];
        for (var intensity = 3; intensity >= 1; intensity--)
        {
            thresholds[intensity] = (intensity - 0.5) / intensityScale;
        }

        var lines = new List<string>();
        for (var intensity = 3; intensity >= 0; intensity--)
        {
            string text;
            if (intensity == 3)
            {
                var seconds = thresholds[3] / healSpeed;
                var splintSeconds = thresholds[3] / (healSpeed * splintMultiplier);
                text = $"{label} > {FormatDuration(seconds)} (~{FormatDuration(splintSeconds)} with splint)";
            }
            else if (intensity == 0)
            {
                text = $"{label} ≤ {FormatDuration(thresholds[1] / healSpeed)}";
            }
            else
            {
                text = $"{label} {FormatDuration(thresholds[intensity] / healSpeed)}"
                       + $" – {FormatDuration(thresholds[intensity + 1] / healSpeed)}";
            }

            lines.Add($"Intensity {intensity}: {text}");
        }

        return string.Join("<br />", lines);
    }

    private static string FormatDuration(double seconds)
    {
        seconds = Math.Max(0, Math.Round(seconds));
        var mins = (int)(seconds / 60);
        var secs = (int)(seconds % 60);

        return mins > 0 && secs > 0
            ? $"{mins} min {secs} s"
            : mins > 0
                ? $"{mins} min"
                : $"{secs} s";
    }
}
