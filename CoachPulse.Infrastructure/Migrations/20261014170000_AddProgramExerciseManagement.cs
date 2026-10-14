using CoachPulse.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoachPulse.Infrastructure.Migrations;

[DbContext(typeof(CoachPulseDbContext))]
[Migration("20261014170000_AddProgramExerciseManagement")]
public sealed class AddProgramExerciseManagement : Migration
{
    protected override void Up(MigrationBuilder m)
    {
        m.AddColumn<Guid>("TenantId", "Exercises", type: "uuid", nullable: true);
        m.AddColumn<Guid>("VideoFileId", "Exercises", type: "uuid", nullable: true);
        m.AddColumn<string>("Equipment", "Exercises", type: "character varying(100)", maxLength: 100, nullable: true);
        m.AddColumn<bool>("IsDeleted", "Exercises", type: "boolean", nullable: false, defaultValue: false);
        m.CreateIndex("IX_Exercises_TenantId_MuscleGroup_Equipment", "Exercises", new[] { "TenantId", "MuscleGroup", "Equipment" });

        m.AddColumn<string>("Status", "Programs", type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Draft");
        m.AddColumn<DateTime>("StartDate", "Programs", type: "timestamp with time zone", nullable: true);
        m.AddColumn<int>("DurationDays", "Programs", type: "integer", nullable: false, defaultValue: 7);
        m.AddColumn<byte[]>("RowVersion", "Programs", type: "bytea", nullable: false, defaultValue: new byte[8]);
        m.AddColumn<bool>("IsDeleted", "Programs", type: "boolean", nullable: false, defaultValue: false);
        m.Sql("UPDATE \"Programs\" SET \"Status\" = CASE WHEN \"ClientId\" IS NOT NULL AND \"IsTemplate\" = false THEN 'Active' ELSE 'Draft' END, \"RowVersion\" = substring(decode(md5(\"Id\"::text), 'hex') from 1 for 8);");
        m.Sql("WITH ranked AS (SELECT \"Id\", row_number() OVER (PARTITION BY \"TenantId\", \"ClientId\" ORDER BY \"CreatedAt\" DESC NULLS LAST, \"Id\") rn FROM \"Programs\" WHERE \"ClientId\" IS NOT NULL AND \"IsTemplate\" = false AND \"Status\" = 'Active') UPDATE \"Programs\" p SET \"Status\" = 'Archived' FROM ranked r WHERE p.\"Id\" = r.\"Id\" AND r.rn > 1;");
        m.CreateIndex("IX_Programs_TenantId_ClientId", "Programs", new[] { "TenantId", "ClientId" }, unique: true,
            filter: "\"Status\" = 'Active' AND \"ClientId\" IS NOT NULL AND \"IsTemplate\" = false AND \"IsDeleted\" = false");

        m.Sql("ALTER TABLE \"ProgramExercises\" ALTER COLUMN \"Reps\" TYPE character varying(30) USING \"Reps\"::text;");
        m.AddColumn<int>("RestSeconds", "ProgramExercises", type: "integer", nullable: false, defaultValue: 0);
        m.AddColumn<int>("SortOrder", "ProgramExercises", type: "integer", nullable: false, defaultValue: 0);
    }

    protected override void Down(MigrationBuilder m)
    {
        m.DropIndex("IX_Programs_TenantId_ClientId", "Programs");
        m.DropIndex("IX_Exercises_TenantId_MuscleGroup_Equipment", "Exercises");
        m.DropColumn("TenantId", "Exercises"); m.DropColumn("VideoFileId", "Exercises"); m.DropColumn("Equipment", "Exercises"); m.DropColumn("IsDeleted", "Exercises");
        m.DropColumn("Status", "Programs"); m.DropColumn("StartDate", "Programs"); m.DropColumn("DurationDays", "Programs"); m.DropColumn("RowVersion", "Programs"); m.DropColumn("IsDeleted", "Programs");
        m.DropColumn("RestSeconds", "ProgramExercises"); m.DropColumn("SortOrder", "ProgramExercises");
        m.Sql("ALTER TABLE \"ProgramExercises\" ALTER COLUMN \"Reps\" TYPE integer USING \"Reps\"::integer;");
    }
}
