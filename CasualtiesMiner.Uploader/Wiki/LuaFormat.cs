using System.Globalization;

namespace CasualtiesMiner.Uploader.Wiki;

internal static class LuaFormat
{
    public static string String(string value)
    {
        return "\"" + value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\r", "")
            .Replace("\n", "\\n") + "\"";
    }

    public static string Bool(bool value) => value ? "true" : "false";

    public static string Int(int value) => value.ToString(CultureInfo.InvariantCulture);

    public static string Num(double value) => value.ToString("R", CultureInfo.InvariantCulture);
}
