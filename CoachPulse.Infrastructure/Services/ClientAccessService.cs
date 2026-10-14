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
            UserRole.Staff => query.Where(x => x.CoachId == userId),
            _ => query.Where(_ => false)
        };
    }
}
