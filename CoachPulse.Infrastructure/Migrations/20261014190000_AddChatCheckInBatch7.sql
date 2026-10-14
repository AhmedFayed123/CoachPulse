START TRANSACTION;
ALTER TABLE "CheckIns" ADD "CoachFeedback" character varying(2000);
ALTER TABLE "CheckIns" ADD "IsDeleted" boolean NOT NULL DEFAULT FALSE;
ALTER TABLE "CheckIns" ADD "ReviewedAt" timestamp with time zone;
ALTER TABLE "CheckIns" ADD "ReviewedByUserId" uuid;
ALTER TABLE "Messages" ADD "IsDeleted" boolean NOT NULL DEFAULT FALSE;
ALTER TABLE "Messages" ADD "IsRead" boolean NOT NULL DEFAULT FALSE;
ALTER TABLE "Messages" ADD "ReadAt" timestamp with time zone;
CREATE INDEX "IX_CheckIns_TenantId_ReviewedAt_Date" ON "CheckIns" ("TenantId", "ReviewedAt", "Date");
CREATE INDEX "IX_Messages_TenantId_SenderId_ReceiverId_Timestamp" ON "Messages" ("TenantId", "SenderId", "ReceiverId", "Timestamp");
CREATE INDEX "IX_Messages_TenantId_ReceiverId_IsRead" ON "Messages" ("TenantId", "ReceiverId", "IsRead");
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261014190000_AddChatCheckInBatch7', '9.0.20');
COMMIT;

