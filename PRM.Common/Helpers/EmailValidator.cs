using PRM.Common.Exceptions;

namespace PRM.Common.Helpers;

public static class EmailValidator
{
    public static void Validate(string email)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        if (!email.Contains('@') || !email.Split('@')[1].Contains('.'))
        {
            throw new BusinessValidationException("Email must be a valid format.");
        }
    }
}
