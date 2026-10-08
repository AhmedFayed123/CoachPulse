using CoachPulse.Application.Interfaces;
using CoachPulse.Domain.Entities;
using CoachPulse.Domain.Enums;
using CoachPulse.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoachPulse.Infrastructure.Data.Seed;

public static class DbSeeder
{
    public static async Task SeedAsync(
        CoachPulseDbContext context,
        IPasswordHasher passwordHasher)
    {
        var existingSuperAdmin = await context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x =>
                x.Email == "superadmin@coachpulse.local" &&
                x.Role == UserRole.SuperAdmin &&
                x.TenantId == null);

        if (existingSuperAdmin != null)
            return;

        var superAdmin = new User
        {
            Id = Guid.NewGuid(),
            TenantId = null,
            Email = "superadmin@coachpulse.local",
            PasswordHash = passwordHasher.Hash("SuperAdmin@12345"),
            Role = UserRole.SuperAdmin
        };

        context.Users.Add(superAdmin);

        await context.SaveChangesAsync();
    }
}