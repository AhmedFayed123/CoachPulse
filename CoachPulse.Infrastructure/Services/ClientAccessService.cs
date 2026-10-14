using CoachPulse.Application.Interfaces;
using CoachPulse.Domain.Entities;
using CoachPulse.Domain.Enums;
using CoachPulse.Infrastructure.Persistence;

namespace CoachPulse.Infrastructure.Services;

public sealed class ClientAccessService(CoachPulseDbContext db) : IClientAccessService
{
    public IQueryable<Client> VisibleClients(Guid tenantId, Guid userId, UserRole role)
    {
        var query = db.Clients.Where(x => x.TenantId == tenantId);
        return role switch
        {
            UserRole.Owner => query,
            UserRole.Staff => query.Where(x => x.CoachId == userId && db.Users.Any(u => u.Id == userId && u.TenantId == tenantId && u.IsActive && !u.IsDeleted)),
            _ => query.Where(_ => false)
        };
    }
}
