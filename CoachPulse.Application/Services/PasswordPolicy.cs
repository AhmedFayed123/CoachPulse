namespace CoachPulse.Application.Services;

public static class PasswordPolicy
{
    public static bool IsValid(string? password) =>
        !string.IsNullOrEmpty(password) && password.Length >= 8 &&
        password.Any(char.IsUpper) && password.Any(char.IsLower) &&
        password.Any(char.IsDigit) && password.Any(c => !char.IsLetterOrDigit(c));

    public const string Message = "Password must be at least 8 characters and contain upper-case, lower-case, numeric, and special characters.";
}
