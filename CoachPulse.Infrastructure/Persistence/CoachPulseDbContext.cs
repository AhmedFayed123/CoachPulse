using CoachPulse.Application.Interfaces;
using CoachPulse.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoachPulse.Infrastructure.Persistence
{
    public class CoachPulseDbContext : DbContext
    {
        private readonly ICurrentTenantService _currentTenantService;

        public CoachPulseDbContext(
            DbContextOptions<CoachPulseDbContext> options,
            ICurrentTenantService currentTenantService)
            : base(options)
        {
            _currentTenantService = currentTenantService;
        }

        public DbSet<Tenant> Tenants => Set<Tenant>();
        public DbSet<User> Users => Set<User>();
        public DbSet<Client> Clients => Set<Client>();
        public DbSet<Exercise> Exercises => Set<Exercise>();
        public DbSet<Program> Programs => Set<Program>();
        public DbSet<ProgramExercise> ProgramExercises => Set<ProgramExercise>();
        public DbSet<MealPlan> MealPlans => Set<MealPlan>();
        public DbSet<CheckIn> CheckIns => Set<CheckIn>();
        public DbSet<Message> Messages => Set<Message>();
        public DbSet<Plan> Plans => Set<Plan>();
        public DbSet<Subscription> Subscriptions => Set<Subscription>();
        public DbSet<Payment> Payments => Set<Payment>();
        public DbSet<ChurnRiskLog> ChurnRiskLogs => Set<ChurnRiskLog>();
        public DbSet<AIGeneratedProgram> AIGeneratedPrograms => Set<AIGeneratedProgram>();
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
        public DbSet<ClientInvitation> ClientInvitations
    => Set<ClientInvitation>();
        public DbSet<PaymobWebhookEvent> WebhookEvents => Set<PaymobWebhookEvent>();
        public DbSet<StoredFile> StoredFiles => Set<StoredFile>();
        public DbSet<StaffInvitation> StaffInvitations => Set<StaffInvitation>();
        public DbSet<UserSecurityToken> UserSecurityTokens => Set<UserSecurityToken>();
        public DbSet<SecurityAuditLog> SecurityAuditLogs => Set<SecurityAuditLog>();
        public DbSet<ClientNote> ClientNotes => Set<ClientNote>();
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(
                typeof(CoachPulseDbContext).Assembly);

            modelBuilder.Entity<User>()
                .HasQueryFilter(x =>
                    x.TenantId == _currentTenantService.TenantId && !x.IsDeleted);

            modelBuilder.Entity<Client>()
                .HasQueryFilter(x =>
                    x.TenantId == _currentTenantService.TenantId);

            modelBuilder.Entity<Program>()
                .HasQueryFilter(x =>
                    x.TenantId == _currentTenantService.TenantId && !x.IsDeleted);

            modelBuilder.Entity<Exercise>()
                .HasQueryFilter(x => !x.IsDeleted &&
                    (x.TenantId == null || x.TenantId == _currentTenantService.TenantId));

            modelBuilder.Entity<MealPlan>()
                .HasQueryFilter(x =>
                    x.TenantId == _currentTenantService.TenantId);

            modelBuilder.Entity<CheckIn>()
                .HasQueryFilter(x =>
                    x.TenantId == _currentTenantService.TenantId);

            modelBuilder.Entity<Message>()
                .HasQueryFilter(x =>
                    x.TenantId == _currentTenantService.TenantId);

            modelBuilder.Entity<Plan>()
                .HasQueryFilter(x =>
                    x.TenantId == _currentTenantService.TenantId);

            modelBuilder.Entity<Subscription>()
                .HasQueryFilter(x =>
                    x.TenantId == _currentTenantService.TenantId);

            modelBuilder.Entity<Payment>()
                .HasQueryFilter(x =>
                    x.TenantId == _currentTenantService.TenantId);

            modelBuilder.Entity<ChurnRiskLog>()
                .HasQueryFilter(x =>
                    x.TenantId == _currentTenantService.TenantId);

            modelBuilder.Entity<AIGeneratedProgram>()
                .HasQueryFilter(x =>
                    x.TenantId == _currentTenantService.TenantId);

            modelBuilder.Entity<StoredFile>()
                .HasQueryFilter(x =>
                    x.TenantId == _currentTenantService.TenantId);

            modelBuilder.Entity<StaffInvitation>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Email).HasMaxLength(255).IsRequired();
                b.Property(x => x.TokenHash).HasMaxLength(64).IsRequired();
                b.HasIndex(x => x.TokenHash).IsUnique();
                b.HasQueryFilter(x => x.TenantId == _currentTenantService.TenantId);
            });
            modelBuilder.Entity<UserSecurityToken>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Purpose).HasMaxLength(40).IsRequired();
                b.Property(x => x.TokenHash).HasMaxLength(64).IsRequired();
                b.HasIndex(x => x.TokenHash).IsUnique();
            });
            modelBuilder.Entity<SecurityAuditLog>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.EventType).HasMaxLength(100).IsRequired();
                b.Property(x => x.Identifier).HasMaxLength(255);
                b.HasIndex(x => new { x.UserId, x.CreatedAt });
            });
            modelBuilder.Entity<ClientNote>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Content).HasMaxLength(4000).IsRequired();
                b.HasIndex(x => new { x.TenantId, x.ClientId, x.CreatedAt });
            });
        }
    }
}
