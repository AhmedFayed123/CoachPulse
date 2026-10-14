using CoachPulse.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoachPulse.Infrastructure.Migrations;

[DbContext(typeof(CoachPulseDbContext))]
[Migration("20261014150000_AddAuthAccountSecurity")]
public partial class AddAuthAccountSecurity : Migration
{
    protected override void Up(MigrationBuilder m)
    {
        m.Sql("CREATE EXTENSION IF NOT EXISTS pgcrypto;");
        m.AddColumn<string>("TokenHash", "RefreshTokens", type: "character varying(500)", maxLength: 500, nullable: true);
        m.Sql("UPDATE \"RefreshTokens\" SET \"TokenHash\" = encode(digest(convert_to(\"Token\", 'UTF8'), 'sha256'), 'hex');");
        m.DropIndex("IX_RefreshTokens_Token", "RefreshTokens"); m.DropColumn("Token", "RefreshTokens");
        m.AlterColumn<string>("TokenHash", "RefreshTokens", type: "character varying(500)", maxLength: 500, nullable: false, oldClrType: typeof(string), oldType: "character varying(500)", oldMaxLength: 500, oldNullable: true);
        m.CreateIndex("IX_RefreshTokens_TokenHash", "RefreshTokens", "TokenHash", unique: true);
        m.AddColumn<string>("Name", "Users", type: "text", nullable: true); m.AddColumn<string>("Phone", "Users", type: "text", nullable: true);
        m.AddColumn<string>("Language", "Users", type: "text", nullable: false, defaultValue: "en"); m.AddColumn<Guid>("AvatarFileId", "Users", type: "uuid", nullable: true);
        m.AddColumn<bool>("IsDeleted", "Users", type: "boolean", nullable: false, defaultValue: false); m.AddColumn<DateTime>("DeletedAt", "Users", type: "timestamp with time zone", nullable: true);
        m.AddColumn<bool>("EmailConfirmed", "Users", type: "boolean", nullable: false, defaultValue: false);
        m.CreateTable(name: "StaffInvitations", columns: t => new { Id = t.Column<Guid>(type: "uuid", nullable: false), TenantId = t.Column<Guid>(type: "uuid", nullable: false), Email = t.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false), TokenHash = t.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false), CreatedAt = t.Column<DateTime>(type: "timestamp with time zone", nullable: false), ExpiresAt = t.Column<DateTime>(type: "timestamp with time zone", nullable: false), AcceptedAt = t.Column<DateTime>(type: "timestamp with time zone", nullable: true) }, constraints: t => t.PrimaryKey("PK_StaffInvitations", x => x.Id));
        m.CreateIndex("IX_StaffInvitations_TokenHash", "StaffInvitations", "TokenHash", unique: true);
        m.CreateTable(name: "UserSecurityTokens", columns: t => new { Id = t.Column<Guid>(type: "uuid", nullable: false), UserId = t.Column<Guid>(type: "uuid", nullable: false), TenantId = t.Column<Guid>(type: "uuid", nullable: true), Purpose = t.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false), TokenHash = t.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false), ExpiresAt = t.Column<DateTime>(type: "timestamp with time zone", nullable: false), CreatedAt = t.Column<DateTime>(type: "timestamp with time zone", nullable: false), UsedAt = t.Column<DateTime>(type: "timestamp with time zone", nullable: true) }, constraints: t => t.PrimaryKey("PK_UserSecurityTokens", x => x.Id));
        m.CreateIndex("IX_UserSecurityTokens_TokenHash", "UserSecurityTokens", "TokenHash", unique: true);
        m.CreateTable(name: "SecurityAuditLogs", columns: t => new { Id = t.Column<Guid>(type: "uuid", nullable: false), UserId = t.Column<Guid>(type: "uuid", nullable: true), TenantId = t.Column<Guid>(type: "uuid", nullable: true), EventType = t.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false), Identifier = t.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true), IpAddress = t.Column<string>(type: "text", nullable: true), UserAgent = t.Column<string>(type: "text", nullable: true), CreatedAt = t.Column<DateTime>(type: "timestamp with time zone", nullable: false) }, constraints: t => t.PrimaryKey("PK_SecurityAuditLogs", x => x.Id));
        m.CreateIndex("IX_SecurityAuditLogs_UserId_CreatedAt", "SecurityAuditLogs", new[] { "UserId", "CreatedAt" });
    }
    protected override void Down(MigrationBuilder m) { throw new NotSupportedException("This security migration is intentionally irreversible because stored refresh tokens are hashed."); }
}
