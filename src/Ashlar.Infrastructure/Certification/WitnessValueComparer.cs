using System.Reflection;
using Ashlar.Core.Application.Certification.Models;
using Ashlar.Core.Domain.Bricks;
using Ashlar.Core.Domain.Execution;

namespace Ashlar.Infrastructure.Certification;

/// <summary>Compares expected and actual witness output values during certification.</summary>
internal static class WitnessValueComparer
{
    /// <summary>Determines whether two witness values are equal using certification comparison rules.</summary>
    public static bool AreEqual(object expected, object actual)
    {
        // A witness pins EXACT output, so equality must not coerce across kinds. Integer and
        // boolean are matched type-first: if either side is integral (resp. boolean), BOTH must
        // be, otherwise the values are unequal. Without this, `Convert.ToInt64` silently rounds a
        // double into an int (2.4 == 2, 1.5 == 2 via banker's rounding) and the string fallback
        // equates an int with its decimal string or a bool — a wrong-by-<0.5 or wrong-typed brick
        // output would pass the correctness proof.
        var expectedIsInt = expected is int or long or short or byte;
        var actualIsInt = actual is int or long or short or byte;
        if (expectedIsInt || actualIsInt)
        {
            if (!(expectedIsInt && actualIsInt))
            {
                return false;
            }
            return Convert.ToInt64(expected) == Convert.ToInt64(actual);
        }

        if (expected is bool || actual is bool)
        {
            return expected is bool eb && actual is bool ab && eb == ab;
        }

        // Strings, floating-point, and other types keep the invariant-culture string comparison —
        // both operands are now the same broad kind (neither is integral or boolean).
        return string.Equals(
            Convert.ToString(expected, System.Globalization.CultureInfo.InvariantCulture),
            Convert.ToString(actual, System.Globalization.CultureInfo.InvariantCulture),
            StringComparison.Ordinal);
    }
}
