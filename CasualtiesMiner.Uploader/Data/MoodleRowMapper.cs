using CasualtiesMiner.Shared.Models;
using CasualtiesMiner.Uploader.Wiki;

namespace CasualtiesMiner.Uploader.Data;

public static class MoodleRowMapper
{
    public static MoodleRow Map(MoodleInfo moodle)
    {
        var (causeKind, causeField) = MoodleCauseClassifier.ClassifyIntensityExpr(moodle.intensityExpr);
        var preconditionDisplay = MoodleCauseFormatter.FormatPrecondition(moodle.preconditionForMoodle);

        return new MoodleRow
        {
            Icon = moodle.icon,
            LocaleId = moodle.localeId,
            DescLocaleKey = moodle.descLocaleKey,
            PreconditionForMoodle = moodle.preconditionForMoodle,
            PreconditionDisplay = preconditionDisplay,
            Intensity = moodle.intensity,
            IntensityBodyFieldId = causeKind == "timer" ? causeField : null,
            Critical = moodle.critical,
            CriticalExpr = moodle.criticalExpr,
            ChippedOnly = moodle.chippedOnly,
        };
    }
}
