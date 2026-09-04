-- =====================================================================
-- Migration 011: Compliance Alert Configuration + Delivery Log.
--
-- dbo.AlertConfigs  -- which document types / entity types to watch,
--                       lead-time (threshold days), recipient emails
-- dbo.AlertLog      -- what was actually emailed, when -- dedupe so the
--                       daily hosted service doesn't resend the same
--                       (entity, document, expiry date, severity) twice
--
-- Safe to re-run: every statement is guarded with an existence check.
-- =====================================================================

USE FleetMasterDb;
GO

IF OBJECT_ID('dbo.AlertConfigs', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AlertConfigs
    (
        Id              INT IDENTITY(1,1) NOT NULL,
        EntityType      INT             NULL,   -- NULL = Vehicle + Driver
        DocumentType    NVARCHAR(100)   NULL,   -- NULL = every document type
        ThresholdDays   INT             NOT NULL,
        RecipientEmails NVARCHAR(500)   NOT NULL,
        IsActive        BIT             NOT NULL CONSTRAINT DF_AlertConfigs_IsActive DEFAULT (1),
        CreatedAt       DATETIME2       NOT NULL CONSTRAINT DF_AlertConfigs_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt       DATETIME2       NULL,
        CONSTRAINT PK_AlertConfigs PRIMARY KEY (Id)
    );
END
GO

IF OBJECT_ID('dbo.AlertLog', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AlertLog
    (
        Id              INT IDENTITY(1,1) NOT NULL,
        EntityType      INT             NOT NULL,
        EntityId        INT             NOT NULL,
        DocumentType    NVARCHAR(100)   NOT NULL,
        ExpiryDate      DATE            NOT NULL,
        Severity        INT             NOT NULL,
        RecipientEmails NVARCHAR(500)   NOT NULL,
        SentAt          DATETIME2       NOT NULL CONSTRAINT DF_AlertLog_SentAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_AlertLog PRIMARY KEY (Id)
    );
    -- Dedupe key: never re-send the same (entity, document, expiry, severity) combo.
    CREATE UNIQUE INDEX IX_AlertLog_Dedupe ON dbo.AlertLog(EntityType, EntityId, DocumentType, ExpiryDate, Severity);
END
GO
