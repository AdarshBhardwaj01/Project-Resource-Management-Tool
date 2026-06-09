using System.Globalization;
using PRM.Common.Exceptions;

namespace PRM.Common.Helpers;

public static class DateValidator
{
    private static readonly string[] SupportedFormats =
    [
        "dd-MMM-yy",
        "dd-MMM-yyyy",
        "dd-MM-yyyy",
        "dd/MM/yyyy"
    ];

    public static DateTime ParseRequired(string input, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            throw new BusinessValidationException($"{fieldName} is required.");
        }

        if (DateTime.TryParseExact(
                input.Trim(),
                SupportedFormats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var parsedDate))
        {
            return parsedDate.Date;
        }

        throw new BusinessValidationException($"{fieldName} must be a valid date (e.g. 01-01-2026).");
    }
}
