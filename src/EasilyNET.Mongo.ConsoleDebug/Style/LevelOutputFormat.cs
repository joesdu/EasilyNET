using Serilog.Events;

// ReSharper disable StringLiteralTypo

namespace EasilyNET.Mongo.ConsoleDebug.Style;

/// <summary>
/// Implements the {Level} element.
/// can now have a fixed width applied to it, as well as casing rules.
/// Width is set through formats like "u3" (uppercase three chars),
/// "w1" (one lowercase char), or "t4" (title case four chars).
/// </summary>
internal static class LevelOutputFormat
{
    private static readonly string[][] _titleCaseLevelMap =
    [
        ["V", "Vb", "Vrb", "Verb", "Verbo", "Verbos", "Verbose"],
        ["D", "De", "Dbg", "Dbug", "Debug"],
        ["I", "In", "Inf", "Info", "Infor", "Inform", "Informa", "Informat", "Informati", "Informatio", "Information"],
        ["W", "Wn", "Wrn", "Warn", "Warni", "Warnin", "Warning"],
        ["E", "Er", "Err", "Eror", "Error"],
        ["F", "Fa", "Ftl", "Fatl", "Fatal"]
    ];

    private static readonly string[][] _lowerCaseLevelMap =
    [
        ["v", "vb", "vrb", "verb", "verbo", "verbos", "verbose"],
        ["d", "de", "dbg", "dbug", "debug"],
        ["i", "in", "inf", "info", "infor", "inform", "informa", "informat", "informati", "informatio", "information"],
        ["w", "wn", "wrn", "warn", "warni", "warnin", "warning"],
        ["e", "er", "err", "eror", "error"],
        ["f", "fa", "ftl", "fatl", "fatal"]
    ];

    private static readonly string[][] _upperCaseLevelMap =
    [
        ["V", "VB", "VRB", "VERB", "VERBO", "VERBOS", "VERBOSE"],
        ["D", "DE", "DBG", "DBUG", "DEBUG"],
        ["I", "IN", "INF", "INFO", "INFOR", "INFORM", "INFORMA", "INFORMAT", "INFORMATI", "INFORMATIO", "INFORMATION"],
        ["W", "WN", "WRN", "WARN", "WARNI", "WARNIN", "WARNING"],
        ["E", "ER", "ERR", "EROR", "ERROR"],
        ["F", "FA", "FTL", "FATL", "FATAL"]
    ];

    internal static string GetLevelMoniker(LogEventLevel value, string? format = null)
    {
        var index = (int)value;
        if (index is < 0 or > (int)LogEventLevel.Fatal)
            return Casing.Format(value.ToString(), format);
        if (format is null || (format.Length is not 2 && format.Length is not 3))
            return Casing.Format(GetLevelMoniker(_titleCaseLevelMap, index), format);

        // Using int.Parse() here requires allocating a string to exclude the first character prefix.
        // Junk like "wxy" will be accepted but produce benign results.
        var width = format[1] - '0';
        switch (format.Length)
        {
            case 3:
                width *= 10;
                width += format[2] - '0';
                break;
        }
        return width < 1
                   ? string.Empty
                   : format[0] switch
                   {
                       'w' => GetLevelMoniker(_lowerCaseLevelMap, index, width),
                       'u' => GetLevelMoniker(_upperCaseLevelMap, index, width),
                       't' => GetLevelMoniker(_titleCaseLevelMap, index, width),
                       _   => Casing.Format(GetLevelMoniker(_titleCaseLevelMap, index), format)
                   };
    }

    // ReSharper disable once SuggestBaseTypeForParameter
    private static string GetLevelMoniker(string[][] caseLevelMap, int index, int width)
    {
        var caseLevel = caseLevelMap[index];
        return caseLevel[Math.Min(width, caseLevel.Length) - 1];
    }

    // ReSharper disable once SuggestBaseTypeForParameter
    private static string GetLevelMoniker(string[][] caseLevelMap, int index) => caseLevelMap[index][^1];
}