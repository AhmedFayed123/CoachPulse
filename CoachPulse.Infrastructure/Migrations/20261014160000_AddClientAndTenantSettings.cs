using CoachPulse.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoachPulse.Infrastructure.Migrations;

[DbContext(typeof(CoachPulseDbContext))]
[Migration("20261014160000_AddClientAndTenantSettings")]
public partial class AddClientAndTenantSettings : Migration
{
    protected override void Up(MigrationBuilder m)
    {
        m.AddColumn<DateTime>("CreatedAt", "Programs", type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP");
        m.AddColumn<int>("Status", "Clients", type: "integer", nullable: false, defaultValue: 1);
        m.AddColumn<int>("CheckInFrequency", "Clients", type: "integer", nullable: true);
        m.AddColumn<int>("CheckInDayOfWeek", "Clients", type: "integer", nullable: true);
        m.CreateIndex("IX_Clients_TenantId_Status", "Clients", new[] { "TenantId", "Status" });

        m.AddColumn<string>("Currency", "Tenants", type: "character varying(3)", maxLength: 3, nullable: false, defaultValue: "EGP");
        m.AddColumn<string>("TimeZone", "Tenants", type: "character varying(100)", maxLength: 100, nullable: false, defaultValue: "Africa/Cairo");
        m.AddColumn<int>("ReminderDays", "Tenants", type: "integer", nullable: false, defaultValue: 7);
        m.AddColumn<string>("DefaultLanguage", "Tenants", type: "character varying(16)", maxLength: 16, nullable: false, defaultValue: "ar");
        m.AddColumn<bool>("PaymobEnabled", "Tenants", type: "boolean", nullable: false, defaultValue: false);
        m.AddColumn<bool>("InstaPayEnabled", "Tenants", type: "boolean", nullable: false, defaultValue: false);
        m.AddColumn<string>("InstaPayId", "Tenants", type: "character varying(64)", maxLength: 64, nullable: true);
        m.AddColumn<string>("InstaPayAccountNumber", "Tenants", type: "character varying(34)", maxLength: 34, nullable: true);

        m.CreateTable(name: "ClientNotes", columns: t => new
        {
            Id = t.Column<Guid>(type: "uuid", nullable: false), TenantId = t.Column<Guid>(type: "uuid", nullable: false),
            ClientId = t.Column<Guid>(type: "uuid", nullable: false), AuthorUserId = t.Column<Guid>(type: "uuid", nullable: false),
            Content = t.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
            CreatedAt = t.Column<DateTime>(type: "timestamp with time zone", nullable: false)
        }, constraints: t => t.PrimaryKey("PK_ClientNotes", x => x.Id));
        m.CreateIndex("IX_ClientNotes_TenantId_ClientId_CreatedAt", "ClientNotes", new[] { "TenantId", "ClientId", "CreatedAt" });
    }

    protected override void Down(MigrationBuilder m)
    {
        m.DropTable("ClientNotes"); m.DropIndex("IX_Clients_TenantId_Status", "Clients");
        m.DropColumn("CreatedAt", "Programs");
        m.DropColumn("Status", "Clients"); m.DropColumn("CheckInFrequency", "Clients"); m.DropColumn("CheckInDayOfWeek", "Clients");
        m.DropColumn("Currency", "Tenants"); m.DropColumn("TimeZone", "Tenants"); m.DropColumn("ReminderDays", "Tenants");
        m.DropColumn("DefaultLanguage", "Tenants"); m.DropColumn("PaymobEnabled", "Tenants"); m.DropColumn("InstaPayEnabled", "Tenants");
        m.DropColumn("InstaPayId", "Tenants"); m.DropColumn("InstaPayAccountNumber", "Tenants");
    }
}
