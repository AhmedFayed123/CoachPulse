using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using CoachPulse.Infrastructure.Persistence;

#nullable disable

namespace CoachPulse.Infrastructure.Migrations;

[DbContext(typeof(CoachPulseDbContext))]
[Migration("20261014190000_AddChatCheckInBatch7")]
public sealed class AddChatCheckInBatch7 : Migration
{
    protected override void Up(MigrationBuilder m)
    {
        m.AddColumn<string>("CoachFeedback", "CheckIns", type: "character varying(2000)", maxLength: 2000, nullable: true);
        m.AddColumn<bool>("IsDeleted", "CheckIns", type: "boolean", nullable: false, defaultValue: false);
        m.AddColumn<DateTime>("ReviewedAt", "CheckIns", type: "timestamp with time zone", nullable: true);
        m.AddColumn<Guid>("ReviewedByUserId", "CheckIns", type: "uuid", nullable: true);
        m.AddColumn<bool>("IsDeleted", "Messages", type: "boolean", nullable: false, defaultValue: false);
        m.AddColumn<bool>("IsRead", "Messages", type: "boolean", nullable: false, defaultValue: false);
        m.AddColumn<DateTime>("ReadAt", "Messages", type: "timestamp with time zone", nullable: true);
        m.CreateIndex("IX_CheckIns_TenantId_ReviewedAt_Date", "CheckIns", new[] { "TenantId", "ReviewedAt", "Date" });
        m.CreateIndex("IX_Messages_TenantId_SenderId_ReceiverId_Timestamp", "Messages", new[] { "TenantId", "SenderId", "ReceiverId", "Timestamp" });
        m.CreateIndex("IX_Messages_TenantId_ReceiverId_IsRead", "Messages", new[] { "TenantId", "ReceiverId", "IsRead" });
    }
    protected override void Down(MigrationBuilder m)
    {
        m.DropIndex("IX_CheckIns_TenantId_ReviewedAt_Date", "CheckIns");
        m.DropIndex("IX_Messages_TenantId_SenderId_ReceiverId_Timestamp", "Messages");
        m.DropIndex("IX_Messages_TenantId_ReceiverId_IsRead", "Messages");
        m.DropColumn("CoachFeedback", "CheckIns"); m.DropColumn("IsDeleted", "CheckIns"); m.DropColumn("ReviewedAt", "CheckIns"); m.DropColumn("ReviewedByUserId", "CheckIns");
        m.DropColumn("IsDeleted", "Messages"); m.DropColumn("IsRead", "Messages"); m.DropColumn("ReadAt", "Messages");
    }
}

