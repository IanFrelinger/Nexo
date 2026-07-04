using System.Reflection;
using Nexo.Core.Application.Certification.Models;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;

namespace Nexo.Infrastructure.Certification;

/// <summary>Compares expected and actual witness output values during certification.</summary>
internal static class WitnessValueComparer
{
    /// <summary>Determines whether two witness values are equal using certification comparison rules.</summary>
    public static bool AreEqual(object expected, object actual)
    {
        if (expected is int or long or short or byte)
        {
            try
            {
                return Convert.ToInt64(expected) == Convert.ToInt64(actual);
            }
            catch
            {
                return false;
            }
        }

        return string.Equals(
            Convert.ToString(expected, System.Globalization.CultureInfo.InvariantCulture),
            Convert.ToString(actual, System.Globalization.CultureInfo.InvariantCulture),
            StringComparison.Ordinal);
    }
}
