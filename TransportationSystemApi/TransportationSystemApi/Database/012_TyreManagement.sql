-- =====================================================================
-- Migration 012: Tyre Management (promote to full module).
--
-- Extends dbo.Tyres into a standalone asset that can exist off any
-- vehicle ("in stock") -- VehicleId becomes nullable, ON DELETE switches
-- from CASCADE to SET NULL so a tyre's life history outlives the vehicle
-- it was last fitted to. App code (VehiclesController.Delete) unassigns a
-- vehicle's tyres back to stock before the delete, so Status stays
-- consistent with VehicleId even though the FK itself only nulls the column.
--
-- New dbo.TyreEvents is the full lifecycle log (Fit/Remove/Rotate/Retread/
-- Inspect/Scrap) for the new standalone Tyre module. The existing
-- dbo.TyreReplacementHistory table is untouched -- it keeps feeding the
-- "Replacements" list already on the vehicle editor's Tyres tab.
--
-- "Stock" (module spec's dbo.TyreStock) is modeled as Tyre rows with
-- VehicleId IS NULL rather than a separate table -- one asset table for a
-- tyre's whole life, no copy-in/copy-out dance when it's pulled or refitted.
--
-- Safe to re-run: every statement is guarded with an existence check.
-- =====================================================================

USE FleetMasterDb;
GO

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Tyres') AND name = 'VehicleId' AND is_nullable = 0)
BEGIN
    ALTER TABLE dbo.Tyres DROP CONSTRAINT FK_Tyres_Vehicles_VehicleId;
    ALTER TABLE dbo.Tyres ALTER COLUMN VehicleId INT NULL;
    ALTER TABLE dbo.Tyres ADD CONSTRAINT FK_Tyres_Vehicles_VehicleId FOREIGN KEY (VehicleId)
        REFERENCES dbo.Vehicles(Id) ON DELETE SET NULL;
END
GO

IF COL_LENGTH('dbo.Tyres', 'SerialNumber') IS NULL
    ALTER TABLE dbo.Tyres ADD SerialNumber NVARCHAR(100) NULL;
GO
IF COL_LENGTH('dbo.Tyres', 'Pattern') IS NULL
    ALTER TABLE dbo.Tyres ADD Pattern NVARCHAR(100) NULL;
GO
IF COL_LENGTH('dbo.Tyres', 'PurchaseDate') IS NULL
    ALTER TABLE dbo.Tyres ADD PurchaseDate DATE NULL;
GO
IF COL_LENGTH('dbo.Tyres', 'PurchaseCost') IS NULL
    ALTER TABLE dbo.Tyres ADD PurchaseCost DECIMAL(18,2) NULL;
GO
IF COL_LENGTH('dbo.Tyres', 'Status') IS NULL
    ALTER TABLE dbo.Tyres ADD Status INT NOT NULL CONSTRAINT DF_Tyres_Status DEFAULT (1); -- 1 = Fitted (existing rows are all currently-fitted tyres)
GO
IF COL_LENGTH('dbo.Tyres', 'TotalDistanceRunCarried') IS NULL
    ALTER TABLE dbo.Tyres ADD TotalDistanceRunCarried DECIMAL(18,2) NOT NULL CONSTRAINT DF_Tyres_TotalDistanceRunCarried DEFAULT (0);
GO
IF COL_LENGTH('dbo.Tyres', 'CreatedAt') IS NULL
    ALTER TABLE dbo.Tyres ADD CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Tyres_CreatedAt DEFAULT (SYSUTCDATETIME());
GO
IF COL_LENGTH('dbo.Tyres', 'UpdatedAt') IS NULL
    ALTER TABLE dbo.Tyres ADD UpdatedAt DATETIME2 NULL;
GO

IF OBJECT_ID('dbo.TyreEvents', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.TyreEvents
    (
        Id          INT IDENTITY(1,1) NOT NULL,
        TyreId      INT             NOT NULL,
        EventType   INT             NOT NULL,
        EventDate   DATE            NOT NULL,
        VehicleId   INT             NULL,   -- soft snapshot, no FK
        Position    INT             NULL,
        Odometer    DECIMAL(18,2)   NULL,
        Cost        DECIMAL(18,2)   NULL,
        Notes       NVARCHAR(MAX)   NULL,
        CreatedAt   DATETIME2       NOT NULL CONSTRAINT DF_TyreEvents_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_TyreEvents PRIMARY KEY (Id),
        CONSTRAINT FK_TyreEvents_Tyres_TyreId FOREIGN KEY (TyreId)
            REFERENCES dbo.Tyres(Id) ON DELETE CASCADE
    );
    CREATE INDEX IX_TyreEvents_TyreId ON dbo.TyreEvents(TyreId);
END
GO
