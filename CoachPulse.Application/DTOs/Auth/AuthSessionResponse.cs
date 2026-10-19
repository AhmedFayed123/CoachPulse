using CoachPulse.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace CoachPulse.Application.DTOs.Auth;

public sealed record AuthSessionResponse([property: Required] string AccessToken, [property: Required] string RefreshToken,
    [property: Required] int ExpiresIn, [property: Required] AuthSessionUser User, [property: Required] AuthSessionTenant? Tenant);
public sealed record AuthSessionUser([property: Required] Guid Id, string? Name, [property: Required] UserRole Role);
public sealed record AuthSessionTenant([property: Required] Guid Id, [property: Required] string Slug, [property: Required] string Name);
