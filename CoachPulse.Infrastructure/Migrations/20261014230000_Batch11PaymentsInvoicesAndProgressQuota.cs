using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoachPulse.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Batch11PaymentsInvoicesAndProgressQuota : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "InvoiceNumber",
                table: "Payments",
                type: "integer",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "GeneratedProgramId",
                table: "AiUsages",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.CreateTable(
                name: "InvoiceSequences",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    NextNumber = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceSequences", x => x.TenantId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_TenantId_InvoiceNumber",
                table: "Payments",
                columns: new[] { "TenantId", "InvoiceNumber" },
                unique: true,
                filter: "\"InvoiceNumber\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InvoiceSequences");

            migrationBuilder.DropIndex(
                name: "IX_Payments_TenantId_InvoiceNumber",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "InvoiceNumber",
                table: "Payments");

            migrationBuilder.AlterColumn<Guid>(
                name: "GeneratedProgramId",
                table: "AiUsages",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
