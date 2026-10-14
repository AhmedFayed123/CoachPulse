using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoachPulse.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAiChurnBatch8 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ModelVersion",
                table: "ChurnRiskLog",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "unknown");

            migrationBuilder.AddColumn<string>(
                name: "RiskLevel",
                table: "ChurnRiskLog",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "Low");

            migrationBuilder.AddColumn<string>(
                name: "TopFactorsJson",
                table: "ChurnRiskLog",
                type: "text",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<Guid>(
                name: "ApprovedProgramId",
                table: "AIGeneratedPrograms",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "AIGeneratedPrograms",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<Guid>(
                name: "MealPlanId",
                table: "AIGeneratedPrograms",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModelVersion",
                table: "AIGeneratedPrograms",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "unknown");

            migrationBuilder.AddColumn<Guid>(
                name: "ParentGeneratedProgramId",
                table: "AIGeneratedPrograms",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PromptVersion",
                table: "AIGeneratedPrograms",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "program-v1");

            migrationBuilder.AddColumn<Guid>(
                name: "RequestedByUserId",
                table: "AIGeneratedPrograms",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "AIGeneratedPrograms",
                type: "bytea",
                nullable: false,
                defaultValueSql: "gen_random_bytes(8)");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "AIGeneratedPrograms",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Draft");

            migrationBuilder.CreateTable(
                name: "AiUsages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    GeneratedProgramId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiUsages", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChurnRiskLog_ClientId_ComputedAt",
                table: "ChurnRiskLog",
                columns: new[] { "ClientId", "ComputedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ChurnRiskLog_TenantId_RiskLevel_ComputedAt",
                table: "ChurnRiskLog",
                columns: new[] { "TenantId", "RiskLevel", "ComputedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AIGeneratedPrograms_TenantId_ClientId_Status_CreatedAt",
                table: "AIGeneratedPrograms",
                columns: new[] { "TenantId", "ClientId", "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AiUsages_TenantId_CreatedAt",
                table: "AiUsages",
                columns: new[] { "TenantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AiUsages_TenantId_GeneratedProgramId",
                table: "AiUsages",
                columns: new[] { "TenantId", "GeneratedProgramId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AiUsages");

            migrationBuilder.DropIndex(
                name: "IX_ChurnRiskLog_ClientId_ComputedAt",
                table: "ChurnRiskLog");

            migrationBuilder.DropIndex(
                name: "IX_ChurnRiskLog_TenantId_RiskLevel_ComputedAt",
                table: "ChurnRiskLog");

            migrationBuilder.DropIndex(
                name: "IX_AIGeneratedPrograms_TenantId_ClientId_Status_CreatedAt",
                table: "AIGeneratedPrograms");

            migrationBuilder.DropColumn(
                name: "ModelVersion",
                table: "ChurnRiskLog");

            migrationBuilder.DropColumn(
                name: "RiskLevel",
                table: "ChurnRiskLog");

            migrationBuilder.DropColumn(
                name: "TopFactorsJson",
                table: "ChurnRiskLog");

            migrationBuilder.DropColumn(
                name: "ApprovedProgramId",
                table: "AIGeneratedPrograms");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "AIGeneratedPrograms");

            migrationBuilder.DropColumn(
                name: "MealPlanId",
                table: "AIGeneratedPrograms");

            migrationBuilder.DropColumn(
                name: "ModelVersion",
                table: "AIGeneratedPrograms");

            migrationBuilder.DropColumn(
                name: "ParentGeneratedProgramId",
                table: "AIGeneratedPrograms");

            migrationBuilder.DropColumn(
                name: "PromptVersion",
                table: "AIGeneratedPrograms");

            migrationBuilder.DropColumn(
                name: "RequestedByUserId",
                table: "AIGeneratedPrograms");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "AIGeneratedPrograms");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "AIGeneratedPrograms");

            migrationBuilder.AddColumn<string>(
                name: "CoachFeedback",
                table: "AIGeneratedPrograms",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Messages_TenantId",
                table: "Messages",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ChurnRiskLog_ClientId",
                table: "ChurnRiskLog",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_ChurnRiskLog_TenantId",
                table: "ChurnRiskLog",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_CheckIns_TenantId",
                table: "CheckIns",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_AIGeneratedPrograms_TenantId",
                table: "AIGeneratedPrograms",
                column: "TenantId");
        }
    }
}
