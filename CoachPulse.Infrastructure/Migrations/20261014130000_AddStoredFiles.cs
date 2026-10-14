using CoachPulse.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoachPulse.Infrastructure.Migrations;

[DbContext(typeof(CoachPulseDbContext))]
[Migration("20261014130000_AddStoredFiles")]
public partial class AddStoredFiles : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "StoredFiles",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                OwnerUserId = table.Column<Guid>(type: "uuid", nullable: false),
                Purpose = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                OriginalName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                ContentType = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                StoragePath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                Sha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                LinkedEntityType = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                LinkedEntityId = table.Column<Guid>(type: "uuid", nullable: true),
                IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_StoredFiles", x => x.Id));

        migrationBuilder.CreateIndex("IX_StoredFiles_TenantId_OwnerUserId_CreatedAt", "StoredFiles", new[] { "TenantId", "OwnerUserId", "CreatedAt" });
        migrationBuilder.CreateIndex("IX_StoredFiles_TenantId_LinkedEntityType_LinkedEntityId", "StoredFiles", new[] { "TenantId", "LinkedEntityType", "LinkedEntityId" });
        migrationBuilder.CreateIndex("IX_StoredFiles_TenantId_IsDeleted_CreatedAt", "StoredFiles", new[] { "TenantId", "IsDeleted", "CreatedAt" });
    }

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable(name: "StoredFiles");
}
