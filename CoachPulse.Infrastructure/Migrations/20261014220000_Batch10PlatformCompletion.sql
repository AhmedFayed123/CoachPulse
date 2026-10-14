START TRANSACTION;
ALTER TABLE "Tenants" ADD "DeletionScheduledAt" timestamp with time zone;

ALTER TABLE "Tenants" ADD "IsDeleted" boolean NOT NULL DEFAULT FALSE;

ALTER TABLE "Tenants" ADD "PrimaryColor" text;

ALTER TABLE "Tenants" ADD "SecondaryColor" text;

ALTER TABLE "SecurityAuditLogs" ADD "Action" text;

ALTER TABLE "SecurityAuditLogs" ADD "ActorRole" text;

ALTER TABLE "SecurityAuditLogs" ADD "ActorUserId" uuid;

ALTER TABLE "SecurityAuditLogs" ADD "EntityId" text;

ALTER TABLE "SecurityAuditLogs" ADD "EntityType" text;

ALTER TABLE "SecurityAuditLogs" ADD "NewValue" text;

ALTER TABLE "SecurityAuditLogs" ADD "OldValue" text;

CREATE TABLE "NotificationPreferences" (
    "UserId" uuid NOT NULL,
    "TenantId" uuid NOT NULL,
    "Type" character varying(80) NOT NULL,
    "InAppEnabled" boolean NOT NULL,
    "EmailEnabled" boolean NOT NULL,
    CONSTRAINT "PK_NotificationPreferences" PRIMARY KEY ("UserId", "TenantId", "Type")
);

CREATE TABLE "Notifications" (
    "Id" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "TenantId" uuid NOT NULL,
    "Type" character varying(80) NOT NULL,
    "Title" character varying(200) NOT NULL,
    "Body" character varying(2000) NOT NULL,
    "DataJson" text,
    "IsRead" boolean NOT NULL,
    "IsDeleted" boolean NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_Notifications" PRIMARY KEY ("Id")
);

CREATE INDEX "IX_SecurityAuditLogs_TenantId_CreatedAt" ON "SecurityAuditLogs" ("TenantId", "CreatedAt");

CREATE INDEX "IX_Notifications_UserId_IsRead_CreatedAt" ON "Notifications" ("UserId", "IsRead", "CreatedAt");

UPDATE "SecurityAuditLogs" SET "ActorUserId" = COALESCE("ActorUserId", "UserId"), "Action" = COALESCE("Action", "EventType"), "EntityId" = COALESCE("EntityId", "Identifier");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261014220000_Batch10PlatformCompletion', '9.0.20');

ALTER TABLE "Users" ADD "IsActive" boolean NOT NULL DEFAULT TRUE;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261014220100_Batch10CoachActive', '9.0.20');

ALTER TABLE "ClientInvitations" ADD "InvitedName" character varying(150);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261014220200_Batch10InvitationName', '9.0.20');

COMMIT;

