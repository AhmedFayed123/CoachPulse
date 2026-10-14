BEGIN;

CREATE TABLE "StoredFiles" (
    "Id" uuid NOT NULL,
    "TenantId" uuid NOT NULL,
    "OwnerUserId" uuid NOT NULL,
    "Purpose" character varying(40) NOT NULL,
    "OriginalName" character varying(255) NOT NULL,
    "ContentType" character varying(150) NOT NULL,
    "SizeBytes" bigint NOT NULL,
    "StoragePath" character varying(500) NOT NULL,
    "Sha256" character varying(64) NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "LinkedEntityType" character varying(80),
    "LinkedEntityId" uuid,
    "IsDeleted" boolean NOT NULL,
    CONSTRAINT "PK_StoredFiles" PRIMARY KEY ("Id")
);

CREATE INDEX "IX_StoredFiles_TenantId_OwnerUserId_CreatedAt"
    ON "StoredFiles" ("TenantId", "OwnerUserId", "CreatedAt");
CREATE INDEX "IX_StoredFiles_TenantId_LinkedEntityType_LinkedEntityId"
    ON "StoredFiles" ("TenantId", "LinkedEntityType", "LinkedEntityId");
CREATE INDEX "IX_StoredFiles_TenantId_IsDeleted_CreatedAt"
    ON "StoredFiles" ("TenantId", "IsDeleted", "CreatedAt");

COMMIT;

-- Down migration:
-- DROP TABLE "StoredFiles";
