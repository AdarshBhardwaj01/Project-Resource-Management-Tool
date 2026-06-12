using System.Text.RegularExpressions;
using PRM.Common.Constants;

namespace PRM.Common.Helpers;

public static class PasswordValidator
{
    public static void Validate(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        if (password.Length < PasswordRules.MinimumLength)
        {
            throw new Exceptions.BusinessValidationException(
                $"Password must be at least {PasswordRules.MinimumLength} characters.");
        }
        if (!password.Any(char.IsUpper))
        {
            throw new Exceptions.BusinessValidationException(
                "Password must contain at least one uppercase letter.");
        }
        if (!password.Any(char.IsDigit))
        {
            throw new Exceptions.BusinessValidationException(
                "Password must contain at least one number.");
        }
    }
}
