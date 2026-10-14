using CoachPulse.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace CoachPulse.Infrastructure.Migrations;

public sealed partial class AddSubscriptionsPaymentsBatch6 : Migration
{
    protected override void Up(MigrationBuilder m)
    {
        m.AddColumn<bool>("IsActive", "Plans", type: "boolean", nullable: false, defaultValue: true);
        m.AddColumn<decimal>("SnapshotPrice", "Subscriptions", type: "numeric(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m);
        m.AddColumn<int>("SnapshotDurationDays", "Subscriptions", type: "integer", nullable: false, defaultValue: 0);
        m.AddColumn<int>("RemainingDays", "Subscriptions", type: "integer", nullable: true);
        m.AddColumn<DateTime>("PausedAt", "Subscriptions", type: "timestamp with time zone", nullable: true);
        m.AddColumn<string>("StateChangeReason", "Subscriptions", type: "text", nullable: true);
        m.Sql("UPDATE \"Subscriptions\" s SET \"SnapshotPrice\"=p.\"Price\", \"SnapshotDurationDays\"=p.\"DurationDays\" FROM \"Plans\" p WHERE p.\"Id\"=s.\"PlanId\";");
        m.AddColumn<DateTime>("CreatedAt", "Payments", type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP");
        m.AddColumn<string>("ReferenceNumber", "Payments", type: "character varying(120)", maxLength: 120, nullable: true);
        m.AddColumn<string>("GatewayTransactionId", "Payments", type: "character varying(100)", maxLength: 100, nullable: true);
        m.AddColumn<Guid>("ReceiptFileId", "Payments", type: "uuid", nullable: true);
        m.AddColumn<string>("RejectionReason", "Payments", type: "text", nullable: true);
        m.AddColumn<byte[]>("Version", "Payments", type: "bytea", nullable: false, defaultValue: new byte[16]);
        m.CreateIndex("IX_Payments_TenantId_ReferenceNumber", "Payments", new[] { "TenantId", "ReferenceNumber" }, unique: true, filter: "\"ReferenceNumber\" IS NOT NULL");
        m.CreateIndex("IX_Payments_TenantId_GatewayTransactionId", "Payments", new[] { "TenantId", "GatewayTransactionId" }, unique: true, filter: "\"GatewayTransactionId\" IS NOT NULL");
        m.CreateTable("PaymentRefunds", t => new { Id = t.Column<Guid>(type:"uuid", nullable:false), TenantId=t.Column<Guid>(type:"uuid",nullable:false), PaymentId=t.Column<Guid>(type:"uuid",nullable:false), Amount=t.Column<decimal>(type:"numeric(18,2)",precision:18,scale:2,nullable:false), Currency=t.Column<string>(type:"character varying(3)",maxLength:3,nullable:false), GatewayReference=t.Column<string>(type:"text",nullable:true), CreatedAt=t.Column<DateTime>(type:"timestamp with time zone",nullable:false), CreatedByUserId=t.Column<Guid>(type:"uuid",nullable:false) }, constraints: t => { t.PrimaryKey("PK_PaymentRefunds",x=>x.Id); t.ForeignKey("FK_PaymentRefunds_Payments_PaymentId",x=>x.PaymentId,"Payments","Id",onDelete: ReferentialAction.Restrict); });
        m.CreateIndex("IX_PaymentRefunds_TenantId_PaymentId", "PaymentRefunds", new[] { "TenantId", "PaymentId" });
        m.CreateTable("IdempotencyKeys", t => new { Id=t.Column<Guid>(type:"uuid",nullable:false), TenantId=t.Column<Guid>(type:"uuid",nullable:false), Key=t.Column<string>(type:"character varying(128)",maxLength:128,nullable:false), UserId=t.Column<Guid>(type:"uuid",nullable:false), Endpoint=t.Column<string>(type:"character varying(200)",maxLength:200,nullable:false), RequestHash=t.Column<string>(type:"character varying(64)",maxLength:64,nullable:false), ResponseSnapshot=t.Column<string>(type:"text",nullable:false), CreatedAt=t.Column<DateTime>(type:"timestamp with time zone",nullable:false) }, constraints:t=>t.PrimaryKey("PK_IdempotencyKeys",x=>x.Id));
        m.CreateIndex("IX_IdempotencyKeys_TenantId_UserId_Endpoint_Key", "IdempotencyKeys", new[] { "TenantId", "UserId", "Endpoint", "Key" }, unique:true);
    }
    protected override void Down(MigrationBuilder m)
    {
        m.DropTable("PaymentRefunds"); m.DropTable("IdempotencyKeys"); m.DropIndex("IX_Payments_TenantId_ReferenceNumber","Payments"); m.DropIndex("IX_Payments_TenantId_GatewayTransactionId","Payments");
        m.DropColumn("IsActive","Plans"); foreach(var c in new[]{"SnapshotPrice","SnapshotDurationDays","RemainingDays","PausedAt","StateChangeReason"}) m.DropColumn(c,"Subscriptions");
        foreach(var c in new[]{"CreatedAt","ReferenceNumber","GatewayTransactionId","ReceiptFileId","RejectionReason","Version"}) m.DropColumn(c,"Payments");
    }
}
