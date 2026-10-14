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
        public DbSet<Meal> Meals => Set<Meal>();
        public DbSet<WorkoutLog> WorkoutLogs => Set<WorkoutLog>();
        public DbSet<CheckIn> CheckIns => Set<CheckIn>();
        public DbSet<Message> Messages => Set<Message>();
        public DbSet<Plan> Plans => Set<Plan>();
        public DbSet<Subscription> Subscriptions => Set<Subscription>();
        public DbSet<Payment> Payments => Set<Payment>();
        public DbSet<PaymentRefund> PaymentRefunds => Set<PaymentRefund>();
        public DbSet<IdempotencyKey> IdempotencyKeys => Set<IdempotencyKey>();
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
        public DbSet<Notification> Notifications => Set<Notification>();
        public DbSet<NotificationPreference> NotificationPreferences => Set<NotificationPreference>();
        public DbSet<ClientNote> ClientNotes => Set<ClientNote>();
        public DbSet<AiUsage> AiUsages => Set<AiUsage>();
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(
                typeof(CoachPulseDbContext).Assembly);

            modelBuilder.Entity<User>()
                .HasQueryFilter(x =>
                    x.TenantId == _currentTenantService.TenantId && !x.IsDeleted && x.IsActive);

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
                    x.TenantId == _currentTenantService.TenantId && !x.IsDeleted);
            modelBuilder.Entity<WorkoutLog>().HasQueryFilter(x => x.TenantId == _currentTenantService.TenantId && !x.IsDeleted);
            modelBuilder.Entity<WorkoutLog>(b =>
            {
                b.HasKey(x => x.Id);
                b.HasIndex(x => new { x.TenantId, x.ClientId, x.ProgramExerciseId, x.PerformedOn, x.SetNumber }).IsUnique();
                b.HasIndex(x => new { x.TenantId, x.ClientId, x.PerformedAt });
                b.Property(x => x.WeightKg).HasPrecision(7, 2);
            });
            modelBuilder.Entity<Meal>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Name).HasMaxLength(200).IsRequired();
                b.Property(x => x.Calories).HasPrecision(8, 2);
                b.Property(x => x.Protein).HasPrecision(8, 2);
                b.Property(x => x.Carbs).HasPrecision(8, 2);
                b.Property(x => x.Fat).HasPrecision(8, 2);
            });

            modelBuilder.Entity<CheckIn>()
                .HasQueryFilter(x => x.TenantId == _currentTenantService.TenantId && !x.IsDeleted);

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

            modelBuilder.Entity<PaymentRefund>().HasQueryFilter(x => x.TenantId == _currentTenantService.TenantId);
            modelBuilder.Entity<IdempotencyKey>().HasQueryFilter(x => x.TenantId == _currentTenantService.TenantId);
            modelBuilder.Entity<AiUsage>().HasQueryFilter(x => x.TenantId == _currentTenantService.TenantId);
            modelBuilder.Entity<AiUsage>(b => { b.HasKey(x => x.Id); b.HasIndex(x => new { x.TenantId, x.CreatedAt }); b.HasIndex(x => new { x.TenantId, x.GeneratedProgramId }).IsUnique(); });

            modelBuilder.Entity<PaymentRefund>(b =>
            {
                b.HasKey(x => x.Id); b.Property(x => x.Amount).HasPrecision(18, 2); b.Property(x => x.Currency).HasMaxLength(3).IsRequired();
                b.HasOne<Payment>().WithMany().HasForeignKey(x => x.PaymentId).OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<IdempotencyKey>(b =>
            {
                b.HasKey(x => x.Id); b.Property(x => x.Key).HasMaxLength(128).IsRequired(); b.Property(x => x.Endpoint).HasMaxLength(200).IsRequired();
                b.Property(x => x.RequestHash).HasMaxLength(64).IsRequired(); b.Property(x => x.ResponseSnapshot).IsRequired();
                b.HasIndex(x => new { x.TenantId, x.UserId, x.Endpoint, x.Key }).IsUnique();
            });

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
                b.HasIndex(x => new { x.TenantId, x.CreatedAt });
            });
            modelBuilder.Entity<Notification>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Type).HasMaxLength(80).IsRequired();
                b.Property(x => x.Title).HasMaxLength(200).IsRequired();
                b.Property(x => x.Body).HasMaxLength(2000).IsRequired();
                b.HasIndex(x => new { x.UserId, x.IsRead, x.CreatedAt });
                b.HasQueryFilter(x => x.TenantId == _currentTenantService.TenantId && !x.IsDeleted);
            });
            modelBuilder.Entity<NotificationPreference>(b =>
            {
                b.HasKey(x => new { x.UserId, x.TenantId, x.Type });
                b.Property(x => x.Type).HasMaxLength(80);
                b.HasQueryFilter(x => x.TenantId == _currentTenantService.TenantId);
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
