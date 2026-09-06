-- =====================================================================
-- Migration 021: Audit trail (data-change log).
--
-- dbo.AuditLog -- one row per inserted / updated / deleted entity, written by
--                 AuditSaveChangesInterceptor on every SaveChanges. ChangesJson
--                 holds { field: {old, new} } for updates, or a full snapshot
--                 for inserts / deletes. Append-only; no FK to Users (the row
--                 must survive the user being deleted).
--
-- High-volume / low-value tables are skipped by the interceptor:
-- AuditLog itself, LoginHistory, VehiclePositions, GeofenceEvents.
--
-- Safe to re-run: guarded with an existence check.
-- =====================================================================

USE FleetMasterDb;
GO

IF OBJECT_ID('dbo.AuditLog', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AuditLog
    (
        Id          BIGINT IDENTITY(1,1) NOT NULL,
        Timestamp   DATETIME2       NOT NULL CONSTRAINT DF_AuditLog_Timestamp DEFAULT (SYSUTCDATETIME()),
        UserId      INT             NULL,
        UserName    NVARCHAR(150)   NULL,
        Action      NVARCHAR(20)    NOT NULL,   -- Insert / Update / Delete
        EntityName  NVARCHAR(120)   NOT NULL,
        EntityId    NVARCHAR(50)    NULL,
        ChangesJson NVARCHAR(MAX)   NULL,
        CONSTRAINT PK_AuditLog PRIMARY KEY (Id)
    );
    CREATE INDEX IX_AuditLog_Timestamp ON dbo.AuditLog(Timestamp DESC);
    CREATE INDEX IX_AuditLog_Entity ON dbo.AuditLog(EntityName, EntityId);
    CREATE INDEX IX_AuditLog_UserId ON dbo.AuditLog(UserId);
END
GO
