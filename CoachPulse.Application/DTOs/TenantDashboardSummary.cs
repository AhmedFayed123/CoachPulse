namespace CoachPulse.Application.DTOs;

public sealed record TenantDashboardSummary([property: System.ComponentModel.DataAnnotations.Required] int ActiveClients,
    decimal? MonthlyRevenue,
    [property: System.ComponentModel.DataAnnotations.Required] int ExpiringSubscriptionsCount,
    [property: System.ComponentModel.DataAnnotations.Required] TenantDashboardAlerts Alerts);
public sealed record TenantDashboardAlerts(int PendingCheckIns, int? PendingPayments, int UnreadMessages);
