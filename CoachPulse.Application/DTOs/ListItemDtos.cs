using CoachPulse.Domain.Enums;

namespace CoachPulse.Application.DTOs;

public sealed record ClientListItem(Guid Id, Guid UserId, string? Name, string Email, Guid? CoachId, string? Goals, ClientStatus Status, string? RiskLevel);

public sealed record PaymentListItem(Guid Id, Guid SubscriptionId, decimal Amount, string Currency, PaymentMethod Method,
    PaymentStatus Status, string? ReferenceNumber, DateTime CreatedAt, string? RejectionReason, Guid? ReceiptFileId,
    string? ClientName = null, string? PlanName = null, string? ClientEmail = null);

public sealed record ConversationListItem(Guid ClientId, string Name, int UnreadCount, ConversationLastMessage? LastMessage);

public sealed record ConversationLastMessage(Guid Id, string Content, Guid SenderId, DateTime Timestamp);
