using CasualtiesMiner.Shared.Models;

namespace CasualtiesMiner.Uploader.Data;

public static class MoodleRowMapper
{
    public static MoodleRow Map(MoodleInfo moodle)
    {
        return new MoodleRow
        {
            LocaleId = moodle.localeId,
            Icon = moodle.icon,
            Intensity = moodle.intensity,
            IntensityExpr = moodle.intensityExpr,
            Critical = moodle.critical,
            CriticalExpr = moodle.criticalExpr,
            ChippedOnly = moodle.chippedOnly,
            DescLocaleKey = moodle.descLocaleKey
        };
    }
}
