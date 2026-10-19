namespace CoachPulse.Application.DTOs;

public enum EmailTemplateType
{
    EmailConfirmation,
    PasswordReset,
    ClientInvitation,
    StaffInvitation,
    SubscriptionExpiryReminder,
    CheckInReminder
}

public sealed record EmailContent(string Subject, string HtmlBody, string TextBody);
