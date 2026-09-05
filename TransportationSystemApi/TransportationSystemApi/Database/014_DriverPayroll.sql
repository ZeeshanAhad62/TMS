-- =====================================================================
-- Migration 014: Driver Payroll / Settlements & Advances.
--
-- dbo.Drivers gains PayType + PayRate  -- per-driver pay configuration
-- dbo.DriverAdvances                   -- cash advances (khata); RecoveredAmount
--                                         tracks how much a pay run has clawed back
-- dbo.PayRuns                          -- one settlement for a driver over a period
-- dbo.PayRunLines                      -- per-trip / per-basis pay lines (child,
--                                         ON DELETE CASCADE)
--
-- Gross pay, net pay and advance-outstanding figures are computed at read
-- time by PayrollMapper, so only the raw inputs are stored: line qty/rate/
-- amount, PayRun.AllowancesTotal, PayRun.AdvanceRecovery, advance amounts
-- and their RecoveredAmount, and the user-set PayRun.Status.
--
-- PayRunLines.TripId is a SOFT link (indexed, no FK): a settled pay run is a
-- frozen record, so deleting a trip must not cascade into or mutate it, and
-- there is no multi-cascade-path conflict with Trips (same rationale as
-- InvoiceLines.TripId in migration 010).
--
-- The roadmap listed this as "013_DriverPayroll" -- renumbered to 014 because
-- 013 shipped as Parts Inventory.
--
-- Safe to re-run: every statement is guarded with an existence check.
-- =====================================================================

USE FleetMasterDb;
GO

IF COL_LENGTH('dbo.Drivers', 'PayType') IS NULL
    ALTER TABLE dbo.Drivers ADD PayType INT NOT NULL CONSTRAINT DF_Drivers_PayType DEFAULT (0);
GO
IF COL_LENGTH('dbo.Drivers', 'PayRate') IS NULL
    ALTER TABLE dbo.Drivers ADD PayRate DECIMAL(18,2) NULL;
GO

IF OBJECT_ID('dbo.DriverAdvances', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.DriverAdvances
    (
        Id              INT IDENTITY(1,1) NOT NULL,
        DriverId        INT             NOT NULL,
        Date            DATE            NOT NULL,
        Amount          DECIMAL(18,2)   NOT NULL,
        RecoveredAmount DECIMAL(18,2)   NOT NULL CONSTRAINT DF_DriverAdvances_Recovered DEFAULT (0),
        Reason          NVARCHAR(300)   NULL,
        Notes           NVARCHAR(MAX)   NULL,
        CreatedAt       DATETIME2       NOT NULL CONSTRAINT DF_DriverAdvances_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt       DATETIME2       NULL,
        CONSTRAINT PK_DriverAdvances PRIMARY KEY (Id),
        CONSTRAINT FK_DriverAdvances_Drivers_DriverId FOREIGN KEY (DriverId)
            REFERENCES dbo.Drivers(Id) ON DELETE CASCADE
    );
    CREATE INDEX IX_DriverAdvances_DriverId ON dbo.DriverAdvances(DriverId);
END
GO

IF OBJECT_ID('dbo.PayRuns', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.PayRuns
    (
        Id              INT IDENTITY(1,1) NOT NULL,
        PayRunCode      NVARCHAR(450)   NOT NULL,
        DriverId        INT             NOT NULL,
        PeriodStart     DATE            NOT NULL,
        PeriodEnd       DATE            NOT NULL,
        Status          INT             NOT NULL CONSTRAINT DF_PayRuns_Status DEFAULT (0),
        AllowancesTotal DECIMAL(18,2)   NOT NULL CONSTRAINT DF_PayRuns_Allowances DEFAULT (0),
        AdvanceRecovery DECIMAL(18,2)   NOT NULL CONSTRAINT DF_PayRuns_AdvanceRecovery DEFAULT (0),
        Notes           NVARCHAR(MAX)   NULL,
        CreatedAt       DATETIME2       NOT NULL CONSTRAINT DF_PayRuns_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt       DATETIME2       NULL,
        CONSTRAINT PK_PayRuns PRIMARY KEY (Id),
        CONSTRAINT FK_PayRuns_Drivers_DriverId FOREIGN KEY (DriverId)
            REFERENCES dbo.Drivers(Id) ON DELETE CASCADE
    );
    CREATE UNIQUE INDEX IX_PayRuns_PayRunCode ON dbo.PayRuns(PayRunCode);
    CREATE INDEX IX_PayRuns_DriverId ON dbo.PayRuns(DriverId);
END
GO

IF OBJECT_ID('dbo.PayRunLines', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.PayRunLines
    (
        Id              INT IDENTITY(1,1) NOT NULL,
        PayRunId        INT             NOT NULL,
        TripId          INT             NULL,
        Description     NVARCHAR(300)   NOT NULL,
        Basis           INT             NOT NULL CONSTRAINT DF_PayRunLines_Basis DEFAULT (4),
        Quantity        DECIMAL(18,2)   NOT NULL CONSTRAINT DF_PayRunLines_Quantity DEFAULT (1),
        Rate            DECIMAL(18,2)   NOT NULL CONSTRAINT DF_PayRunLines_Rate DEFAULT (0),
        Amount          DECIMAL(18,2)   NOT NULL,
        CONSTRAINT PK_PayRunLines PRIMARY KEY (Id),
        CONSTRAINT FK_PayRunLines_PayRuns_PayRunId FOREIGN KEY (PayRunId)
            REFERENCES dbo.PayRuns(Id) ON DELETE CASCADE
    );
    CREATE INDEX IX_PayRunLines_PayRunId ON dbo.PayRunLines(PayRunId);
    CREATE INDEX IX_PayRunLines_TripId ON dbo.PayRunLines(TripId);
END
GO
