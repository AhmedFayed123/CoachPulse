using CoachPulse.Domain.Entities;
using CoachPulse.Domain.Enums;

namespace CoachPulse.Application.Interfaces;

public interface IClientAccessService
{
    IQueryable<Client> VisibleClients(Guid tenantId, Guid userId, UserRole role);
}
