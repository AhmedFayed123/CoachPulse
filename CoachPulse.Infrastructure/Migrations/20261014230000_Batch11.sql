START TRANSACTION;
ALTER TABLE "Payments" ADD "InvoiceNumber" integer;

ALTER TABLE "AiUsages" ALTER COLUMN "GeneratedProgramId" DROP NOT NULL;

CREATE TABLE "InvoiceSequences" (
    "TenantId" uuid NOT NULL,
    "NextNumber" integer NOT NULL,
    CONSTRAINT "PK_InvoiceSequences" PRIMARY KEY ("TenantId")
);

CREATE UNIQUE INDEX "IX_Payments_TenantId_InvoiceNumber" ON "Payments" ("TenantId", "InvoiceNumber") WHERE "InvoiceNumber" IS NOT NULL;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261014230000_Batch11PaymentsInvoicesAndProgressQuota', '9.0.20');

CREATE TABLE "MealLogs" (
    "Id" uuid NOT NULL,
    "TenantId" uuid NOT NULL,
    "ClientId" uuid NOT NULL,
    "MealId" uuid NOT NULL,
    "LoggedOn" date NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_MealLogs" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_MealLogs_Meals_MealId" FOREIGN KEY ("MealId") REFERENCES "Meals" ("Id") ON DELETE RESTRICT
);

CREATE INDEX "IX_MealLogs_MealId" ON "MealLogs" ("MealId");

CREATE UNIQUE INDEX "IX_MealLogs_TenantId_ClientId_MealId_LoggedOn" ON "MealLogs" ("TenantId", "ClientId", "MealId", "LoggedOn");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261014230100_Batch11MealLogs', '9.0.20');

CREATE TABLE "DataExportRequests" (
    "Id" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "TenantId" uuid,
    "Status" character varying(24) NOT NULL,
    "StoredFileId" uuid,
    "StoragePath" text,
    "SizeBytes" bigint,
    "DownloadTokenHash" character varying(64) NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "ExpiresAt" timestamp with time zone,
    "DownloadedAt" timestamp with time zone,
    "FailureCode" text,
    CONSTRAINT "PK_DataExportRequests" PRIMARY KEY ("Id")
);

CREATE INDEX "IX_DataExportRequests_UserId_CreatedAt" ON "DataExportRequests" ("UserId", "CreatedAt");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261014230200_Batch11DataExports', '9.0.20');

ALTER TABLE "Payments" ADD CONSTRAINT "CK_Payments_Method" CHECK ("Method" IN (1, 2, 3));

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261014230300_Batch11CashPaymentConstraint', '9.0.20');

COMMIT;
