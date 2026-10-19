using System.Net;
using CoachPulse.Application.DTOs;

namespace CoachPulse.Application.Services;

public static class EmailTemplateRenderer
{
    public static EmailContent Render(EmailTemplateType type, string? language, IReadOnlyDictionary<string, string>? values = null)
    {
        var ar = string.Equals(language, "ar", StringComparison.OrdinalIgnoreCase);
        var map = values ?? new Dictionary<string, string>();
        var name = Value(map, "name");
        var token = Value(map, "token");
        var detail = Value(map, "detail");
        var title = type switch
        {
            EmailTemplateType.EmailConfirmation => ar ? "تأكيد البريد الإلكتروني" : "Confirm your email",
            EmailTemplateType.PasswordReset => ar ? "إعادة تعيين كلمة المرور" : "Reset your password",
            EmailTemplateType.ClientInvitation => ar ? "دعوة للانضمام كعميل" : "Your client invitation",
            EmailTemplateType.StaffInvitation => ar ? "دعوة للانضمام إلى فريق التدريب" : "Your coach invitation",
            EmailTemplateType.SubscriptionExpiryReminder => ar ? "تذكير بانتهاء الاشتراك" : "Subscription expiry reminder",
            EmailTemplateType.CheckInReminder => ar ? "موعد تسجيل المتابعة" : "Check-in reminder",
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };
        var greeting = ar ? $"مرحباً {name}" : $"Hello {name}";
        var copy = type switch
        {
            EmailTemplateType.EmailConfirmation => ar ? "استخدم رمز التأكيد التالي:" : "Use this confirmation code:",
            EmailTemplateType.PasswordReset => ar ? "استخدم الرمز التالي لإعادة تعيين كلمة المرور:" : "Use this code to reset your password:",
            EmailTemplateType.ClientInvitation => ar ? "استخدم رمز الدعوة التالي لإكمال التسجيل:" : "Use this invitation code to finish registration:",
            EmailTemplateType.StaffInvitation => ar ? "استخدم رمز الدعوة التالي للانضمام إلى فريق التدريب:" : "Use this invitation code to join the coaching team:",
            EmailTemplateType.SubscriptionExpiryReminder => ar ? "ينتهي اشتراكك قريباً." : "Your subscription will expire soon.",
            _ => ar ? "حان موعد تسجيل متابعة تقدمك." : "It is time to submit your progress check-in."
        };
        if (!string.IsNullOrWhiteSpace(detail)) copy += " " + detail;
        var safeGreeting = WebUtility.HtmlEncode(greeting);
        var safeCopy = WebUtility.HtmlEncode(copy);
        var safeToken = WebUtility.HtmlEncode(token);
        var dir = ar ? "rtl" : "ltr";
        var tokenHtml = string.IsNullOrEmpty(safeToken) ? "" : $"<p><code>{safeToken}</code></p>";
        var html = $"<!doctype html><html lang=\"{(ar ? "ar" : "en")}\" dir=\"{dir}\"><body><main style=\"font-family:Arial,sans-serif;max-width:600px;margin:auto;padding:24px\"><h1>{WebUtility.HtmlEncode(title)}</h1><p>{safeGreeting}</p><p>{safeCopy}</p>{tokenHtml}</main></body></html>";
        var text = $"{title}\n\n{greeting}\n{copy}" + (string.IsNullOrEmpty(token) ? "" : $"\n{token}");
        return new EmailContent(title, html, text);
    }

    private static string Value(IReadOnlyDictionary<string, string> values, string key) => values.TryGetValue(key, out var value) ? value : string.Empty;
}
