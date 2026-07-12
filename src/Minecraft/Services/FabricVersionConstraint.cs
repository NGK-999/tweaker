using System.Globalization;
using System.Text.RegularExpressions;

namespace ApexTweaker.Minecraft.Services;

internal static partial class FabricVersionConstraint
{
    public static bool Matches(string version, string constraint)
    {
        if (string.IsNullOrWhiteSpace(constraint) || constraint.Trim() == "*")
        {
            return true;
        }

        if (!VersionNumber.TryParse(version, out var candidate))
        {
            return true;
        }

        var alternatives = constraint.Split("||", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        return alternatives.Any(alternative => MatchesAlternative(candidate, alternative));
    }

    private static bool MatchesAlternative(VersionNumber candidate, string alternative)
    {
        var normalized = alternative.Trim().Trim('[', ']', '(', ')');
        if (normalized == "*")
        {
            return true;
        }

        if (normalized.Contains(',') && !normalized.Contains(' '))
        {
            return normalized
                .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .Any(part => MatchesAlternative(candidate, part));
        }

        var comparators = ComparatorRegex().Matches(normalized);
        if (comparators.Count > 0)
        {
            return comparators.All(match => MatchesComparator(candidate, match.Groups[1].Value, match.Groups[2].Value));
        }

        if (normalized.StartsWith('~'))
        {
            return MatchesTilde(candidate, normalized[1..]);
        }

        if (normalized.Contains('x') || normalized.Contains('X') || normalized.Contains('*'))
        {
            return MatchesWildcard(candidate, normalized);
        }

        return VersionNumber.TryParse(normalized, out var exact) && candidate.Equals(exact);
    }

    private static bool MatchesComparator(VersionNumber candidate, string operation, string expectedText)
    {
        if (!VersionNumber.TryParse(expectedText, out var expected))
        {
            return true;
        }

        var comparison = candidate.CompareTo(expected);
        return operation switch
        {
            ">" => comparison > 0,
            ">=" => comparison >= 0,
            "<" => comparison < 0,
            "<=" => comparison <= 0,
            "=" or "==" => comparison == 0,
            "~" => MatchesTilde(candidate, expectedText),
            _ => true
        };
    }

    private static bool MatchesTilde(VersionNumber candidate, string expectedText)
    {
        if (!VersionNumber.TryParse(expectedText, out var expected))
        {
            return true;
        }

        return candidate.Major == expected.Major &&
               candidate.Minor == expected.Minor &&
               candidate.CompareTo(expected) >= 0;
    }

    private static bool MatchesWildcard(VersionNumber candidate, string expectedText)
    {
        var parts = expectedText
            .Trim()
            .TrimStart('~', '=', 'v', 'V')
            .Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var values = new[] { candidate.Major, candidate.Minor, candidate.Patch };
        for (var index = 0; index < parts.Length && index < values.Length; index++)
        {
            if (parts[index].Equals("x", StringComparison.OrdinalIgnoreCase) || parts[index] == "*")
            {
                return true;
            }

            var numeric = LeadingNumberRegex().Match(parts[index]);
            if (!numeric.Success || !int.TryParse(numeric.Value, CultureInfo.InvariantCulture, out var expected))
            {
                return true;
            }

            if (values[index] != expected)
            {
                return false;
            }
        }

        return true;
    }

    [GeneratedRegex(@"(>=|<=|==|=|>|<|~)\s*([0-9]+(?:\.[0-9xX*]+){0,2}(?:[-+][^\s]+)?)")]
    private static partial Regex ComparatorRegex();

    [GeneratedRegex(@"^\d+")]
    private static partial Regex LeadingNumberRegex();

    private readonly partial record struct VersionNumber(int Major, int Minor, int Patch) : IComparable<VersionNumber>
    {
        public int CompareTo(VersionNumber other)
        {
            var major = Major.CompareTo(other.Major);
            if (major != 0)
            {
                return major;
            }

            var minor = Minor.CompareTo(other.Minor);
            return minor != 0 ? minor : Patch.CompareTo(other.Patch);
        }

        public static bool TryParse(string value, out VersionNumber version)
        {
            var matches = NumberRegex().Matches(value ?? string.Empty);
            if (matches.Count == 0)
            {
                version = default;
                return false;
            }

            var values = new int[3];
            for (var index = 0; index < Math.Min(3, matches.Count); index++)
            {
                _ = int.TryParse(matches[index].Value, CultureInfo.InvariantCulture, out values[index]);
            }

            version = new VersionNumber(values[0], values[1], values[2]);
            return true;
        }

        [GeneratedRegex(@"\d+")]
        private static partial Regex NumberRegex();
    }
}
