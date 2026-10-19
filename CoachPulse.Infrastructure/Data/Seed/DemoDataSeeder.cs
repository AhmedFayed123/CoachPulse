using CoachPulse.Application.Interfaces;
using CoachPulse.Domain.Entities;
using CoachPulse.Domain.Enums;
using CoachPulse.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace CoachPulse.Infrastructure.Data.Seed;

public static class DemoDataSeeder
{
    public static async Task SeedAsync(CoachPulseDbContext db, IPasswordHasher hasher, IConfiguration config, bool isDevelopment, bool isProduction, CancellationToken ct = default)
    {
        var enabled = config.GetValue<bool>("DemoSeed:Enabled");
        if (isProduction && enabled) throw new InvalidOperationException("DemoSeed:Enabled cannot be true in Production.");
        if (!isDevelopment || !enabled) return;

        const string slug = "coachpulse-demo";
        var password = config["DemoSeed:Password"];
        if (string.IsNullOrWhiteSpace(password) || password.Length < 16)
            throw new InvalidOperationException("Development demo seeding requires DemoSeed__Password with at least 16 characters.");

        var existingDemoTenant = await db.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Slug == slug, ct);
        if (existingDemoTenant is not null)
        {
            await SeedMissingDemoRecordsAsync(db, existingDemoTenant, hasher, password, ct);
            return;
        }

        var tenant = new Tenant { Id = Guid.NewGuid(), Name = "CoachPulse Demo Studio", Slug = slug, Status = "Active", Currency = "EGP", TimeZone = "Africa/Cairo", DefaultLanguage = "en" };
        var coach = new User { Id = Guid.NewGuid(), TenantId = tenant.Id, Email = "coach@coachpulse-demo.test", Name = "Demo Coach", PasswordHash = hasher.Hash(password), Role = UserRole.TenantOwner };
        var staff = new User { Id = Guid.NewGuid(), TenantId = tenant.Id, Email = "staff@coachpulse-demo.test", Name = "Demo Coach Staff", PasswordHash = hasher.Hash(password), Role = UserRole.CoachStaff };
        var users = Enumerable.Range(1, 3).Select(i => new User
        {
            Id = Guid.NewGuid(), TenantId = tenant.Id, Email = $"client{i}@coachpulse-demo.test", Name = $"Demo Client {i}",
            PasswordHash = hasher.Hash(password), Role = UserRole.Client
        }).ToArray();
        var clients = new[]
        {
            new Client { Id = Guid.NewGuid(), TenantId = tenant.Id, UserId = users[0].Id, CoachId = coach.Id, Status = ClientStatus.Active, Goals = "Build strength" },
            new Client { Id = Guid.NewGuid(), TenantId = tenant.Id, UserId = users[1].Id, CoachId = coach.Id, Status = ClientStatus.Paused, Goals = "Improve mobility" },
            new Client { Id = Guid.NewGuid(), TenantId = tenant.Id, UserId = users[2].Id, CoachId = coach.Id, Status = ClientStatus.Archived, Goals = "Improve conditioning" }
        };
        var plans = new[]
        {
            new Plan { Id = Guid.NewGuid(), TenantId = tenant.Id, Name = "Demo Monthly", Price = 1200m, DurationDays = 30 },
            new Plan { Id = Guid.NewGuid(), TenantId = tenant.Id, Name = "Demo Quarterly", Price = 3200m, DurationDays = 90 }
        };
        var exercises = new[]
        {
            new Exercise { Id = Guid.NewGuid(), TenantId = tenant.Id, Name = "Goblet Squat", MuscleGroup = "Legs", Equipment = "Dumbbell" },
            new Exercise { Id = Guid.NewGuid(), TenantId = tenant.Id, Name = "Dumbbell Bench Press", MuscleGroup = "Chest", Equipment = "Dumbbell" },
            new Exercise { Id = Guid.NewGuid(), TenantId = tenant.Id, Name = "Seated Cable Row", MuscleGroup = "Back", Equipment = "Cable" }
        };
        var program = new Program { Id = Guid.NewGuid(), TenantId = tenant.Id, ClientId = clients[0].Id, CoachId = coach.Id, Title = "Demo Strength Plan", Status = "Active", StartDate = DateTime.UtcNow.Date.AddDays(-7), DurationDays = 28 };
        var programExercises = exercises.Select((exercise, index) => new ProgramExercise
        {
            Id = Guid.NewGuid(), ProgramId = program.Id, ExerciseId = exercise.Id,
            Sets = 3, Reps = "10-12", RestSeconds = 90, SortOrder = index + 1, Day = 1
        }).ToArray();
        var mealPlan = new MealPlan { Id = Guid.NewGuid(), TenantId = tenant.Id, ClientId = clients[0].Id, Title = "Balanced Training Day", Status = "Active" };
        var meals = new[]
        {
            new Meal { Id = Guid.NewGuid(), MealPlanId = mealPlan.Id, Name = "Breakfast · Oats and eggs", Time = new TimeOnly(8, 0), Calories = 520, Protein = 32, Carbs = 58, Fat = 18 },
            new Meal { Id = Guid.NewGuid(), MealPlanId = mealPlan.Id, Name = "Lunch · Chicken and rice", Time = new TimeOnly(13, 0), Calories = 680, Protein = 48, Carbs = 76, Fat = 18 },
            new Meal { Id = Guid.NewGuid(), MealPlanId = mealPlan.Id, Name = "Dinner · Tuna salad", Time = new TimeOnly(19, 0), Calories = 460, Protein = 42, Carbs = 30, Fat = 19 }
        };
        var subscription = new Subscription
        {
            Id = Guid.NewGuid(), TenantId = tenant.Id, ClientId = clients[0].Id, PlanId = plans[0].Id,
            Status = SubscriptionStatus.Pending, StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddDays(plans[0].DurationDays), SnapshotPrice = plans[0].Price,
            SnapshotDurationDays = plans[0].DurationDays
        };

        await using var transaction = db.Database.IsRelational() ? await db.Database.BeginTransactionAsync(ct) : null;
        db.Tenants.Add(tenant); db.Users.Add(coach); db.Users.Add(staff); db.Users.AddRange(users);
        await db.SaveChangesAsync(ct);
        db.Clients.AddRange(clients); db.Plans.AddRange(plans); db.Exercises.AddRange(exercises);
        db.Programs.Add(program); db.ProgramExercises.AddRange(programExercises);
        db.MealPlans.Add(mealPlan); db.Meals.AddRange(meals); db.Subscriptions.Add(subscription);
        await db.SaveChangesAsync(ct);
        db.CheckIns.AddRange(clients.Select((client, index) => new CheckIn
        {
            Id = Guid.NewGuid(), TenantId = tenant.Id, ClientId = client.Id, Date = DateTime.UtcNow.AddDays(-index * 7), Weight = 70m + index * 5,
            Measurements = "{\"waistCm\":82,\"chestCm\":96}", CoachFeedback = index == 0 ? "Great consistency this week." : null
        }));
        db.WorkoutLogs.Add(new WorkoutLog
        {
            Id = Guid.NewGuid(), TenantId = tenant.Id, ClientId = clients[0].Id,
            ProgramExerciseId = programExercises[0].Id, PerformedOn = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(-1)),
            SetNumber = 1, Reps = 12, WeightKg = 20, PerformedAt = DateTime.UtcNow.AddDays(-1)
        });
        db.Payments.Add(new Payment
        {
            Id = Guid.NewGuid(), TenantId = tenant.Id, SubscriptionId = subscription.Id,
            Amount = subscription.SnapshotPrice, Currency = tenant.Currency, Method = PaymentMethod.InstaPay,
            Status = PaymentStatus.PendingVerification, ReferenceNumber = "DEMO-TRANSFER-001"
        });
        db.Messages.AddRange(
            new Message { Id = Guid.NewGuid(), TenantId = tenant.Id, SenderId = users[0].Id, ReceiverId = coach.Id, Content = "I completed the first workout. Should I increase the weight next time?", Timestamp = DateTime.UtcNow.AddHours(-2) },
            new Message { Id = Guid.NewGuid(), TenantId = tenant.Id, SenderId = coach.Id, ReceiverId = users[0].Id, Content = "Great work. Keep the same weight until all sets feel controlled.", IsRead = true, ReadAt = DateTime.UtcNow.AddHours(-1), Timestamp = DateTime.UtcNow.AddHours(-1) }
        );
        db.Notifications.AddRange(
            new Notification { Id = Guid.NewGuid(), TenantId = tenant.Id, UserId = coach.Id, Type = "PaymentPending", Title = "Payment needs review", Body = "Demo Client 1 submitted an InstaPay transfer." },
            new Notification { Id = Guid.NewGuid(), TenantId = tenant.Id, UserId = users[0].Id, Type = "ProgramAssigned", Title = "Your program is ready", Body = "Your strength program has been assigned." }
        );
        db.ClientNotes.Add(new ClientNote { Id = Guid.NewGuid(), TenantId = tenant.Id, ClientId = clients[0].Id, AuthorUserId = coach.Id, Content = "Prefers evening training sessions.", CreatedAt = DateTime.UtcNow.AddDays(-2) });
        var generatedDraft = new AIGeneratedProgram
        {
            Id = Guid.NewGuid(), TenantId = tenant.Id, ClientId = clients[0].Id, RequestedByUserId = coach.Id,
            Prompt = "Development sample draft. Review and edit before use.",
            GeneratedContent = "{\"title\":\"Demo Strength Draft\",\"workoutPlan\":[],\"unmatchedExercises\":[],\"disclaimer\":\"Development sample only.\"}",
            Status = "Draft", PromptVersion = "seed-v1", ModelVersion = "demo"
        };
        db.AIGeneratedPrograms.Add(generatedDraft);
        db.AiUsages.Add(new AiUsage { Id = Guid.NewGuid(), TenantId = tenant.Id, UserId = coach.Id, GeneratedProgramId = generatedDraft.Id, CreatedAt = DateTime.UtcNow });
        db.ChurnRiskLogs.Add(new ChurnRiskLog { Id = Guid.NewGuid(), TenantId = tenant.Id, ClientId = clients[0].Id, RiskScore = 0.22m, RiskLevel = "Low", TopFactorsJson = "[{\"name\":\"recent_checkins\",\"impact\":\"positive\"}]", ModelVersion = "demo-seed" });
        db.NotificationPreferences.Add(new NotificationPreference { UserId = users[0].Id, TenantId = tenant.Id, Type = "ProgramAssigned", InAppEnabled = true, EmailEnabled = false });
        db.SecurityAuditLogs.Add(new SecurityAuditLog { Id = Guid.NewGuid(), TenantId = tenant.Id, UserId = coach.Id, ActorUserId = coach.Id, ActorRole = "TenantOwner", Action = "demo.seeded", EventType = "demo.seeded", EntityType = "Tenant", EntityId = tenant.Id.ToString(), NewValue = "Development sample data created", CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync(ct);
        if (transaction is not null) await transaction.CommitAsync(ct);
    }

    private static async Task SeedMissingDemoRecordsAsync(CoachPulseDbContext db, Tenant tenant, IPasswordHasher hasher, string password, CancellationToken ct)
    {
        var coach = await db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.TenantId == tenant.Id && x.Role == UserRole.TenantOwner && !x.IsDeleted, ct);
        var clients = await db.Clients.IgnoreQueryFilters().Include(x => x.User).Where(x => x.TenantId == tenant.Id).OrderBy(x => x.User.Name).ToListAsync(ct);
        if (coach is null || clients.Count == 0) return;
        if (!await db.Users.IgnoreQueryFilters().AnyAsync(x => x.TenantId == tenant.Id && x.Email == "staff@coachpulse-demo.test", ct))
        {
            db.Users.Add(new User { Id = Guid.NewGuid(), TenantId = tenant.Id, Email = "staff@coachpulse-demo.test", Name = "Demo Coach Staff", PasswordHash = hasher.Hash(password), Role = UserRole.CoachStaff });
            await db.SaveChangesAsync(ct);
        }

        var exercises = await db.Exercises.IgnoreQueryFilters().Where(x => x.TenantId == tenant.Id && !x.IsDeleted).ToListAsync(ct);
        if (exercises.Count == 0)
        {
            exercises = new List<Exercise>
            {
                new() { Id = Guid.NewGuid(), TenantId = tenant.Id, Name = "Goblet Squat", MuscleGroup = "Legs", Equipment = "Dumbbell" },
                new() { Id = Guid.NewGuid(), TenantId = tenant.Id, Name = "Dumbbell Bench Press", MuscleGroup = "Chest", Equipment = "Dumbbell" },
                new() { Id = Guid.NewGuid(), TenantId = tenant.Id, Name = "Seated Cable Row", MuscleGroup = "Back", Equipment = "Cable" }
            };
            db.Exercises.AddRange(exercises);
            await db.SaveChangesAsync(ct);
        }
        var program = await db.Programs.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.TenantId == tenant.Id && x.ClientId == clients[0].Id && !x.IsDeleted, ct);
        if (program is null)
        {
            program = new Program { Id = Guid.NewGuid(), TenantId = tenant.Id, ClientId = clients[0].Id, CoachId = coach.Id, Title = "Demo Strength Plan", Status = "Active", StartDate = DateTime.UtcNow.Date.AddDays(-7), DurationDays = 28 };
            db.Programs.Add(program);
            await db.SaveChangesAsync(ct);
        }
        if (!await db.ProgramExercises.AnyAsync(x => x.ProgramId == program.Id, ct))
        {
            db.ProgramExercises.AddRange(exercises.Take(3).Select((exercise, index) => new ProgramExercise { Id = Guid.NewGuid(), ProgramId = program.Id, ExerciseId = exercise.Id, Sets = 3, Reps = "10-12", RestSeconds = 90, SortOrder = index + 1, Day = 1 }));
            await db.SaveChangesAsync(ct);
        }
        if (!await db.MealPlans.IgnoreQueryFilters().AnyAsync(x => x.TenantId == tenant.Id && x.ClientId == clients[0].Id && !x.IsDeleted, ct))
        {
            var mealPlan = new MealPlan { Id = Guid.NewGuid(), TenantId = tenant.Id, ClientId = clients[0].Id, Title = "Balanced Training Day", Status = "Active" };
            db.MealPlans.Add(mealPlan);
            await db.SaveChangesAsync(ct);
            db.Meals.AddRange(
                new Meal { Id = Guid.NewGuid(), MealPlanId = mealPlan.Id, Name = "Breakfast · Oats and eggs", Time = new TimeOnly(8, 0), Calories = 520, Protein = 32, Carbs = 58, Fat = 18 },
                new Meal { Id = Guid.NewGuid(), MealPlanId = mealPlan.Id, Name = "Lunch · Chicken and rice", Time = new TimeOnly(13, 0), Calories = 680, Protein = 48, Carbs = 76, Fat = 18 },
                new Meal { Id = Guid.NewGuid(), MealPlanId = mealPlan.Id, Name = "Dinner · Tuna salad", Time = new TimeOnly(19, 0), Calories = 460, Protein = 42, Carbs = 30, Fat = 19 });
            await db.SaveChangesAsync(ct);
        }
        if (!await db.WorkoutLogs.IgnoreQueryFilters().AnyAsync(x => x.TenantId == tenant.Id && !x.IsDeleted, ct))
        {
            var programExerciseId = await db.ProgramExercises.Where(x => x.ProgramId == program.Id).OrderBy(x => x.SortOrder).Select(x => x.Id).FirstOrDefaultAsync(ct);
            if (programExerciseId != Guid.Empty)
            {
                db.WorkoutLogs.Add(new WorkoutLog { Id = Guid.NewGuid(), TenantId = tenant.Id, ClientId = clients[0].Id, ProgramExerciseId = programExerciseId, PerformedOn = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(-1)), SetNumber = 1, Reps = 12, WeightKg = 20, PerformedAt = DateTime.UtcNow.AddDays(-1) });
                await db.SaveChangesAsync(ct);
            }
        }
        if (!await db.Subscriptions.IgnoreQueryFilters().AnyAsync(x => x.TenantId == tenant.Id, ct))
        {
            var plan = await db.Plans.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.TenantId == tenant.Id && x.IsActive, ct);
            if (plan is null)
            {
                plan = new Plan { Id = Guid.NewGuid(), TenantId = tenant.Id, Name = "Demo Monthly", Price = 1200m, DurationDays = 30 };
                db.Plans.Add(plan);
                await db.SaveChangesAsync(ct);
            }
            var subscription = new Subscription { Id = Guid.NewGuid(), TenantId = tenant.Id, ClientId = clients[0].Id, PlanId = plan.Id, Status = SubscriptionStatus.Pending, StartDate = DateTime.UtcNow.Date, EndDate = DateTime.UtcNow.Date.AddDays(plan.DurationDays), SnapshotPrice = plan.Price, SnapshotDurationDays = plan.DurationDays };
            db.Subscriptions.Add(subscription);
            await db.SaveChangesAsync(ct);
            db.Payments.Add(new Payment { Id = Guid.NewGuid(), TenantId = tenant.Id, SubscriptionId = subscription.Id, Amount = plan.Price, Currency = tenant.Currency, Method = PaymentMethod.InstaPay, Status = PaymentStatus.PendingVerification, ReferenceNumber = "DEMO-TRANSFER-001" });
            await db.SaveChangesAsync(ct);
        }
        if (!await db.CheckIns.IgnoreQueryFilters().AnyAsync(x => x.TenantId == tenant.Id, ct))
        {
            db.CheckIns.AddRange(clients.Select((client, index) => new CheckIn { Id = Guid.NewGuid(), TenantId = tenant.Id, ClientId = client.Id, Date = DateTime.UtcNow.AddDays(-index * 7), Weight = 70m + index * 5, Measurements = "{\"waistCm\":82,\"chestCm\":96}", CoachFeedback = index == 0 ? "Great consistency this week." : null }));
            await db.SaveChangesAsync(ct);
        }
        if (!await db.Messages.IgnoreQueryFilters().AnyAsync(x => x.TenantId == tenant.Id, ct))
        {
            db.Messages.AddRange(
                new Message { Id = Guid.NewGuid(), TenantId = tenant.Id, SenderId = clients[0].UserId, ReceiverId = coach.Id, Content = "I completed the first workout. Should I increase the weight next time?", Timestamp = DateTime.UtcNow.AddHours(-2) },
                new Message { Id = Guid.NewGuid(), TenantId = tenant.Id, SenderId = coach.Id, ReceiverId = clients[0].UserId, Content = "Great work. Keep the same weight until all sets feel controlled.", IsRead = true, ReadAt = DateTime.UtcNow.AddHours(-1), Timestamp = DateTime.UtcNow.AddHours(-1) });
            await db.SaveChangesAsync(ct);
        }
        if (!await db.Notifications.AnyAsync(x => x.TenantId == tenant.Id, ct))
        {
            db.Notifications.AddRange(
                new Notification { Id = Guid.NewGuid(), TenantId = tenant.Id, UserId = coach.Id, Type = "PaymentPending", Title = "Payment needs review", Body = "A demo client submitted an InstaPay transfer." },
                new Notification { Id = Guid.NewGuid(), TenantId = tenant.Id, UserId = clients[0].UserId, Type = "ProgramAssigned", Title = "Your program is ready", Body = "Your strength program has been assigned." });
            await db.SaveChangesAsync(ct);
        }
        if (!await db.ClientNotes.IgnoreQueryFilters().AnyAsync(x => x.TenantId == tenant.Id, ct))
        {
            db.ClientNotes.Add(new ClientNote { Id = Guid.NewGuid(), TenantId = tenant.Id, ClientId = clients[0].Id, AuthorUserId = coach.Id, Content = "Prefers evening training sessions.", CreatedAt = DateTime.UtcNow.AddDays(-2) });
            await db.SaveChangesAsync(ct);
        }
        if (!await db.AIGeneratedPrograms.IgnoreQueryFilters().AnyAsync(x => x.TenantId == tenant.Id, ct))
        {
            var draft = new AIGeneratedProgram { Id = Guid.NewGuid(), TenantId = tenant.Id, ClientId = clients[0].Id, RequestedByUserId = coach.Id, Prompt = "Development sample draft. Review and edit before use.", GeneratedContent = "{\"title\":\"Demo Strength Draft\",\"workoutPlan\":[],\"unmatchedExercises\":[],\"disclaimer\":\"Development sample only.\"}", Status = "Draft", PromptVersion = "seed-v1", ModelVersion = "demo" };
            db.AIGeneratedPrograms.Add(draft);
            db.AiUsages.Add(new AiUsage { Id = Guid.NewGuid(), TenantId = tenant.Id, UserId = coach.Id, GeneratedProgramId = draft.Id });
            await db.SaveChangesAsync(ct);
        }
        if (!await db.ChurnRiskLogs.IgnoreQueryFilters().AnyAsync(x => x.TenantId == tenant.Id, ct))
        {
            db.ChurnRiskLogs.Add(new ChurnRiskLog { Id = Guid.NewGuid(), TenantId = tenant.Id, ClientId = clients[0].Id, RiskScore = 0.22m, RiskLevel = "Low", TopFactorsJson = "[{\"name\":\"recent_checkins\",\"impact\":\"positive\"}]", ModelVersion = "demo-seed" });
            await db.SaveChangesAsync(ct);
        }
        if (!await db.SecurityAuditLogs.IgnoreQueryFilters().AnyAsync(x => x.TenantId == tenant.Id, ct))
        {
            db.SecurityAuditLogs.Add(new SecurityAuditLog { Id = Guid.NewGuid(), TenantId = tenant.Id, UserId = coach.Id, ActorUserId = coach.Id, ActorRole = "TenantOwner", Action = "demo.seeded", EventType = "demo.seeded", EntityType = "Tenant", EntityId = tenant.Id.ToString(), NewValue = "Development sample data created", CreatedAt = DateTime.UtcNow });
            await db.SaveChangesAsync(ct);
        }
    }
}
