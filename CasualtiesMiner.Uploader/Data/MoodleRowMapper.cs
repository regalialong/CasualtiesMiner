using CasualtiesMiner.Shared.Models;

namespace CasualtiesMiner.Uploader.Data;

public static class MoodleRowMapper
{
    public static MoodleRow Map(MoodleInfo moodle)
    {
        return new MoodleRow
        {
            Icon = moodle.icon,
            LocaleId = moodle.localeId,
            DescLocaleKey = moodle.descLocaleKey,
            PreconditionForMoodle = moodle.preconditionForMoodle,
            Intensity = moodle.intensity,
            IntensityExpr = moodle.intensityExpr,
            Critical = moodle.critical,
            CriticalExpr = moodle.criticalExpr,
            ChippedOnly = moodle.chippedOnly,
        };
    }
}
