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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(
                typeof(CoachPulseDbContext).Assembly);

            modelBuilder.Entity<User>()
                .HasQueryFilter(x =>
                    x.TenantId == _currentTenantService.TenantId);

            modelBuilder.Entity<Client>()
                .HasQueryFilter(x =>
                    x.TenantId == _currentTenantService.TenantId);

            modelBuilder.Entity<Program>()
                .HasQueryFilter(x =>
                    x.TenantId == _currentTenantService.TenantId);

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
        }
    }
}
