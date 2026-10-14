using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoachPulse.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNewEntitiesToModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Programs_TenantId",
                table: "Programs");

            migrationBuilder.DropIndex(
                name: "IX_Clients_TenantId",
                table: "Clients");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Programs_TenantId",
                table: "Programs",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Clients_TenantId",
                table: "Clients",
                column: "TenantId");
        }
    }
}
