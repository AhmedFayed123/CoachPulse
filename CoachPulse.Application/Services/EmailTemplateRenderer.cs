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
        var actionUrl = Value(map, "actionUrl");
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
        if (!string.IsNullOrWhiteSpace(actionUrl) && type == EmailTemplateType.PasswordReset)
            copy = ar ? "\u0627\u0636\u063A\u0637 \u0639\u0644\u0649 \u0627\u0644\u0631\u0627\u0628\u0637 \u0644\u0625\u0646\u0634\u0627\u0621 \u0643\u0644\u0645\u0629 \u0645\u0631\u0648\u0631 \u062C\u062F\u064A\u062F\u0629. \u0627\u0644\u0631\u0627\u0628\u0637 \u0635\u0627\u0644\u062D \u0644\u0645\u062F\u0629 \u0645\u062D\u062F\u0648\u062F\u0629." : "Use the secure link below to choose a new password. The link expires shortly.";
        else if (!string.IsNullOrWhiteSpace(actionUrl) && type == EmailTemplateType.EmailConfirmation)
            copy = ar ? "\u0627\u0636\u063A\u0637 \u0639\u0644\u0649 \u0627\u0644\u0631\u0627\u0628\u0637 \u0644\u062A\u0623\u0643\u064A\u062F \u0628\u0631\u064A\u062F\u0643 \u0627\u0644\u0625\u0644\u0643\u062A\u0631\u0648\u0646\u064A." : "Use the secure link below to confirm your email address.";
        else if (!string.IsNullOrWhiteSpace(actionUrl) && type == EmailTemplateType.ClientInvitation)
            copy = ar ? "\u0627\u0641\u062A\u062D \u0631\u0627\u0628\u0637 \u0627\u0644\u062F\u0639\u0648\u0629 \u0644\u0625\u0643\u0645\u0627\u0644 \u062A\u0633\u062C\u064A\u0644\u0643." : "Open the invitation link below to complete your registration.";
        if (!string.IsNullOrWhiteSpace(detail)) copy += " " + detail;
        var safeGreeting = WebUtility.HtmlEncode(greeting);
        var safeCopy = WebUtility.HtmlEncode(copy);
        var safeToken = WebUtility.HtmlEncode(token);
        var dir = ar ? "rtl" : "ltr";
        var safeActionUrl = WebUtility.HtmlEncode(actionUrl);
        var tokenHtml = string.IsNullOrEmpty(actionUrl)
            ? string.IsNullOrEmpty(safeToken) ? "" : $"<p><code>{safeToken}</code></p>"
            : $"<p><a href=\"{safeActionUrl}\" style=\"display:inline-block;padding:12px 20px;background:#155eef;color:#fff;text-decoration:none;border-radius:6px\">{WebUtility.HtmlEncode(ar ? "متابعة" : "Continue")}</a></p>";
        var html = $"<!doctype html><html lang=\"{(ar ? "ar" : "en")}\" dir=\"{dir}\"><body><main style=\"font-family:Arial,sans-serif;max-width:600px;margin:auto;padding:24px\"><h1>{WebUtility.HtmlEncode(title)}</h1><p>{safeGreeting}</p><p>{safeCopy}</p>{tokenHtml}</main></body></html>";
        var text = $"{title}\n\n{greeting}\n{copy}" + (string.IsNullOrEmpty(actionUrl) ? string.IsNullOrEmpty(token) ? "" : $"\n{token}" : $"\n{actionUrl}");
        return new EmailContent(title, html, text);
    }

    private static string Value(IReadOnlyDictionary<string, string> values, string key) => values.TryGetValue(key, out var value) ? value : string.Empty;
}
