START TRANSACTION;
ALTER TABLE "ChurnRiskLog" ADD "ModelVersion" character varying(100) NOT NULL DEFAULT 'unknown';

ALTER TABLE "ChurnRiskLog" ADD "RiskLevel" character varying(10) NOT NULL DEFAULT 'Low';

ALTER TABLE "ChurnRiskLog" ADD "TopFactorsJson" text NOT NULL DEFAULT '[]';

ALTER TABLE "AIGeneratedPrograms" ADD "ApprovedProgramId" uuid;

ALTER TABLE "AIGeneratedPrograms" ADD "CreatedAt" timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP);

ALTER TABLE "AIGeneratedPrograms" ADD "MealPlanId" uuid;

ALTER TABLE "AIGeneratedPrograms" ADD "ModelVersion" character varying(100) NOT NULL DEFAULT 'unknown';

ALTER TABLE "AIGeneratedPrograms" ADD "ParentGeneratedProgramId" uuid;

ALTER TABLE "AIGeneratedPrograms" ADD "PromptVersion" character varying(50) NOT NULL DEFAULT 'program-v1';

ALTER TABLE "AIGeneratedPrograms" ADD "RequestedByUserId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';

ALTER TABLE "AIGeneratedPrograms" ADD "RowVersion" bytea NOT NULL DEFAULT (gen_random_bytes(8));

ALTER TABLE "AIGeneratedPrograms" ADD "Status" character varying(30) NOT NULL DEFAULT 'Draft';

CREATE TABLE "AiUsages" (
    "Id" uuid NOT NULL,
    "TenantId" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "GeneratedProgramId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_AiUsages" PRIMARY KEY ("Id")
);

CREATE INDEX "IX_ChurnRiskLog_ClientId_ComputedAt" ON "ChurnRiskLog" ("ClientId", "ComputedAt");

CREATE INDEX "IX_ChurnRiskLog_TenantId_RiskLevel_ComputedAt" ON "ChurnRiskLog" ("TenantId", "RiskLevel", "ComputedAt");

CREATE INDEX "IX_AIGeneratedPrograms_TenantId_ClientId_Status_CreatedAt" ON "AIGeneratedPrograms" ("TenantId", "ClientId", "Status", "CreatedAt");

CREATE INDEX "IX_AiUsages_TenantId_CreatedAt" ON "AiUsages" ("TenantId", "CreatedAt");

CREATE UNIQUE INDEX "IX_AiUsages_TenantId_GeneratedProgramId" ON "AiUsages" ("TenantId", "GeneratedProgramId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261014200000_AddAiChurnBatch8', '9.0.20');

COMMIT;


