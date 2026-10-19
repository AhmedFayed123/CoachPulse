using CoachPulse.Application.Interfaces;
using CoachPulse.Domain.Entities;
using CoachPulse.Domain.Enums;
using CoachPulse.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Net.Mail;

namespace CoachPulse.Infrastructure.Data.Seed;

public static class DbSeeder
{
    public static async Task SeedAsync(
        CoachPulseDbContext context,
        IPasswordHasher passwordHasher,
        IConfiguration configuration,
        bool isProduction = false)
    {
        var existingSuperAdmin = await context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x =>
                x.Role == UserRole.SuperAdmin &&
                x.TenantId == null);

        if (existingSuperAdmin != null)
            return;

        var email = configuration["SuperAdmin:Email"] ?? configuration["SUPERADMIN_EMAIL"] ?? configuration["Bootstrap:SuperAdminEmail"];
        var password = configuration["SuperAdmin:Password"] ?? configuration["SUPERADMIN_PASSWORD"] ?? configuration["Bootstrap:SuperAdminPassword"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            if (!isProduction) return;
            throw new InvalidOperationException("Production bootstrap requires SuperAdmin__Email and SuperAdmin__Password environment variables before first startup.");
        }
        try { _ = new MailAddress(email); } catch (FormatException) { throw new InvalidOperationException("SUPERADMIN_EMAIL must be a valid email address."); }
        if (password.Length < 16 || !password.Any(char.IsUpper) || !password.Any(char.IsLower) || !password.Any(char.IsDigit) || !password.Any(c => !char.IsLetterOrDigit(c)))
            throw new InvalidOperationException("SUPERADMIN_PASSWORD must be at least 16 characters and include upper, lower, numeric, and symbol characters.");

        var superAdmin = new User
        {
            Id = Guid.NewGuid(),
            TenantId = null,
            Email = email.Trim().ToLowerInvariant(),
            PasswordHash = passwordHasher.Hash(password),
            Role = UserRole.SuperAdmin,
            MustChangePassword = true
        };

        context.Users.Add(superAdmin);

        await context.SaveChangesAsync();
    }
}
