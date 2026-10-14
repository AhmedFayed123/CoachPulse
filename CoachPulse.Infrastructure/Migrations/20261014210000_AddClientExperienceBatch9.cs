using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using CoachPulse.Infrastructure.Persistence;

#nullable disable

namespace CoachPulse.Infrastructure.Persistence.Migrations;

[DbContext(typeof(CoachPulseDbContext))]
[Migration("20261014210000_AddClientExperienceBatch9")]
public partial class AddClientExperienceBatch9 : Migration
{
    protected override void Up(MigrationBuilder m)
    {
        m.AddColumn<bool>(name: "OnboardingCompleted", table: "Clients", type: "boolean", nullable: false, defaultValue: false);
        m.Sql("UPDATE \"Clients\" SET \"OnboardingCompleted\" = TRUE;");
        m.DropForeignKey(name: "FK_MealPlans_Clients_ClientId", table: "MealPlans");
        m.DropIndex(name: "IX_MealPlans_ClientId", table: "MealPlans");
        m.AlterColumn<Guid>(name: "ClientId", table: "MealPlans", type: "uuid", nullable: true, oldClrType: typeof(Guid), oldType: "uuid");
        m.AddColumn<string>(name: "Title", table: "MealPlans", type: "character varying(200)", maxLength: 200, nullable: false, defaultValue: "Meal plan");
        m.AddColumn<bool>(name: "IsTemplate", table: "MealPlans", type: "boolean", nullable: false, defaultValue: false);
        m.AddColumn<string>(name: "Status", table: "MealPlans", type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Draft");
        m.AddColumn<byte[]>(name: "RowVersion", table: "MealPlans", type: "bytea", nullable: false, defaultValueSql: "gen_random_bytes(8)");
        m.AddColumn<bool>(name: "IsDeleted", table: "MealPlans", type: "boolean", nullable: false, defaultValue: false);
        m.AddColumn<DateTime>(name: "CreatedAt", table: "MealPlans", type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP");
        m.Sql("WITH ranked AS (SELECT \"Id\", row_number() OVER (PARTITION BY \"TenantId\", \"ClientId\" ORDER BY \"CreatedAt\" DESC, \"Id\") AS rn FROM \"MealPlans\" WHERE \"ClientId\" IS NOT NULL) UPDATE \"MealPlans\" p SET \"Status\" = CASE WHEN ranked.rn = 1 THEN 'Active' ELSE 'Archived' END FROM ranked WHERE p.\"Id\" = ranked.\"Id\";");
        m.CreateIndex(name: "IX_MealPlans_ClientId", table: "MealPlans", column: "ClientId");
        m.CreateIndex(name: "IX_MealPlans_TenantId_ClientId", table: "MealPlans", columns: new[] { "TenantId", "ClientId" }, unique: true, filter: "\"Status\" = 'Active' AND \"ClientId\" IS NOT NULL AND \"IsTemplate\" = false AND \"IsDeleted\" = false");
        m.AddForeignKey(name: "FK_MealPlans_Clients_ClientId", table: "MealPlans", column: "ClientId", principalTable: "Clients", principalColumn: "Id", onDelete: ReferentialAction.Restrict);

        m.CreateTable(name: "Meals", columns: table => new
        {
            Id = table.Column<Guid>(type: "uuid", nullable: false), MealPlanId = table.Column<Guid>(type: "uuid", nullable: false),
            Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false), Time = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
            Calories = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: false), Protein = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: false),
            Carbs = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: false), Fat = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: false)
        }, constraints: table => { table.PrimaryKey("PK_Meals", x => x.Id); table.ForeignKey("FK_Meals_MealPlans_MealPlanId", x => x.MealPlanId, "MealPlans", "Id", onDelete: ReferentialAction.Cascade); });
        m.CreateIndex(name: "IX_Meals_MealPlanId", table: "Meals", column: "MealPlanId");

        m.CreateTable(name: "WorkoutLogs", columns: table => new
        {
            Id = table.Column<Guid>(type: "uuid", nullable: false), TenantId = table.Column<Guid>(type: "uuid", nullable: false), ClientId = table.Column<Guid>(type: "uuid", nullable: false),
            ProgramExerciseId = table.Column<Guid>(type: "uuid", nullable: false), PerformedOn = table.Column<DateOnly>(type: "date", nullable: false), SetNumber = table.Column<int>(type: "integer", nullable: false), Reps = table.Column<int>(type: "integer", nullable: false),
            WeightKg = table.Column<decimal>(type: "numeric(7,2)", precision: 7, scale: 2, nullable: false), PerformedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false), IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
        }, constraints: table => { table.PrimaryKey("PK_WorkoutLogs", x => x.Id); table.ForeignKey("FK_WorkoutLogs_Clients_ClientId", x => x.ClientId, "Clients", "Id", onDelete: ReferentialAction.Restrict); table.ForeignKey("FK_WorkoutLogs_ProgramExercises_ProgramExerciseId", x => x.ProgramExerciseId, "ProgramExercises", "Id", onDelete: ReferentialAction.Restrict); table.ForeignKey("FK_WorkoutLogs_Tenants_TenantId", x => x.TenantId, "Tenants", "Id", onDelete: ReferentialAction.Restrict); });
        m.CreateIndex(name: "IX_WorkoutLogs_TenantId_ClientId_ProgramExerciseId_PerformedOn_SetNumber", table: "WorkoutLogs", columns: new[] { "TenantId", "ClientId", "ProgramExerciseId", "PerformedOn", "SetNumber" }, unique: true);
        m.CreateIndex(name: "IX_WorkoutLogs_TenantId_ClientId_PerformedAt", table: "WorkoutLogs", columns: new[] { "TenantId", "ClientId", "PerformedAt" });
        m.CreateIndex(name: "IX_WorkoutLogs_ClientId", table: "WorkoutLogs", column: "ClientId");
        m.CreateIndex(name: "IX_WorkoutLogs_ProgramExerciseId", table: "WorkoutLogs", column: "ProgramExerciseId");
    }

    protected override void Down(MigrationBuilder m)
    {
        m.DropTable("Meals"); m.DropTable("WorkoutLogs");
        m.DropIndex("IX_MealPlans_TenantId_ClientId", "MealPlans"); m.DropIndex("IX_MealPlans_ClientId", "MealPlans");
        m.DropColumn("Title", "MealPlans"); m.DropColumn("IsTemplate", "MealPlans"); m.DropColumn("Status", "MealPlans"); m.DropColumn("RowVersion", "MealPlans"); m.DropColumn("IsDeleted", "MealPlans"); m.DropColumn("CreatedAt", "MealPlans");
        m.DropForeignKey("FK_MealPlans_Clients_ClientId", "MealPlans"); m.AlterColumn<Guid>("ClientId", "MealPlans", type: "uuid", nullable: false, oldClrType: typeof(Guid), oldType: "uuid", oldNullable: true);
        m.CreateIndex("IX_MealPlans_ClientId", "MealPlans", "ClientId"); m.AddForeignKey("FK_MealPlans_Clients_ClientId", "MealPlans", "ClientId", "Clients", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        m.DropColumn("OnboardingCompleted", "Clients");
    }
}
