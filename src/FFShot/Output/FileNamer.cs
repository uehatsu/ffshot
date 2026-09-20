using System.Text;
using System.Text.RegularExpressions;

namespace FFShot.Output;

/// <summary>
/// ファイル名パターンを展開する。
/// <c>{yyyyMMdd_HHmmss}</c> のような { } 内は DateTime 書式、<c>{target}</c> は "full" / "window"。
/// </summary>
public static partial class FileNamer
{
    [GeneratedRegex(@"\{([^{}]+)\}")]
    private static partial Regex TokenRegex();

    public static string Expand(string pattern, DateTime now, string target)
    {
        var expanded = TokenRegex().Replace(pattern, m =>
        {
            var token = m.Groups[1].Value;
            if (token.Equals("target", StringComparison.OrdinalIgnoreCase))
            {
                return target;
            }
            try
            {
                return now.ToString(token, System.Globalization.CultureInfo.InvariantCulture);
            }
            catch (FormatException)
            {
                return token;
            }
        });

        var name = Sanitize(expanded);
        if (string.IsNullOrWhiteSpace(name))
        {
            name = "ffshot_" + now.ToString("yyyyMMdd_HHmmss", System.Globalization.CultureInfo.InvariantCulture);
        }
        return name;
    }

    public static string Sanitize(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sb = new StringBuilder(name.Length);
        foreach (var c in name)
        {
            sb.Append(Array.IndexOf(invalid, c) >= 0 ? '_' : c);
        }
        return sb.ToString().Trim().TrimEnd('.');
    }

    /// <summary>既存ファイルと衝突しないパスを返す（_1, _2 ... を付与）。</summary>
    public static string UniquePath(string folder, string baseName, string extension, Func<string, bool>? exists = null)
    {
        exists ??= File.Exists;
        var candidate = Path.Combine(folder, baseName + extension);
        for (var i = 1; exists(candidate); i++)
        {
            candidate = Path.Combine(folder, $"{baseName}_{i}{extension}");
        }
        return candidate;
    }
}
