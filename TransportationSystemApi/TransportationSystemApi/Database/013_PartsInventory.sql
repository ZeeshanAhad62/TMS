-- =====================================================================
-- Migration 013: Spare Parts Inventory / Stores.
--
-- dbo.Parts           -- part master: number, name, unit, reorder level, standard cost
-- dbo.StockMovements  -- receipt / issue / adjust log against a Part; on-hand
--                         qty and stock value are computed at read time
--                         (PartMapper), not stored.
--
-- dbo.WorkOrderItems gains an optional PartId: issuing a work-order line
-- against a stocked part auto-creates an Issue StockMovement, tracked back
-- via the new StockMovementId (soft link) so editing/deleting the line keeps
-- on-hand qty in sync (see WorkOrderItemsController).
--
-- Deferred: a dbo.Suppliers table -- that's Vendor master territory, owned
-- by module 11 (Vendor / Procurement / Purchase Orders) when it lands.
-- StockMovements carries a free-text SupplierName for receipts in the
-- meantime; ReferenceType/ReferenceId is generic so a future PurchaseOrder
-- reference doesn't need a schema change.
--
-- Safe to re-run: every statement is guarded with an existence check.
-- =====================================================================

USE FleetMasterDb;
GO

IF OBJECT_ID('dbo.Parts', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Parts
    (
        Id              INT IDENTITY(1,1) NOT NULL,
        PartNumber      NVARCHAR(100)   NOT NULL,
        Name            NVARCHAR(200)   NOT NULL,
        Unit            NVARCHAR(30)    NOT NULL CONSTRAINT DF_Parts_Unit DEFAULT ('pcs'),
        ReorderLevel    DECIMAL(18,2)   NOT NULL CONSTRAINT DF_Parts_ReorderLevel DEFAULT (0),
        StandardCost    DECIMAL(18,2)   NULL,
        Notes           NVARCHAR(MAX)   NULL,
        CreatedAt       DATETIME2       NOT NULL CONSTRAINT DF_Parts_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt       DATETIME2       NULL,
        CONSTRAINT PK_Parts PRIMARY KEY (Id)
    );
    CREATE UNIQUE INDEX IX_Parts_PartNumber ON dbo.Parts(PartNumber);
END
GO

IF OBJECT_ID('dbo.StockMovements', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.StockMovements
    (
        Id              INT IDENTITY(1,1) NOT NULL,
        PartId          INT             NOT NULL,
        MovementType    INT             NOT NULL,
        Quantity        DECIMAL(18,2)   NOT NULL,
        UnitCost        DECIMAL(18,2)   NULL,
        Date            DATE            NOT NULL,
        ReferenceType   INT             NOT NULL CONSTRAINT DF_StockMovements_ReferenceType DEFAULT (0),
        ReferenceId     INT             NULL,
        SupplierName    NVARCHAR(150)   NULL,
        Notes           NVARCHAR(MAX)   NULL,
        CreatedAt       DATETIME2       NOT NULL CONSTRAINT DF_StockMovements_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_StockMovements PRIMARY KEY (Id),
        CONSTRAINT FK_StockMovements_Parts_PartId FOREIGN KEY (PartId)
            REFERENCES dbo.Parts(Id) ON DELETE CASCADE
    );
    CREATE INDEX IX_StockMovements_PartId ON dbo.StockMovements(PartId);
END
GO

IF COL_LENGTH('dbo.WorkOrderItems', 'PartId') IS NULL
    ALTER TABLE dbo.WorkOrderItems ADD PartId INT NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_WorkOrderItems_Parts_PartId')
    ALTER TABLE dbo.WorkOrderItems ADD CONSTRAINT FK_WorkOrderItems_Parts_PartId FOREIGN KEY (PartId)
        REFERENCES dbo.Parts(Id) ON DELETE SET NULL;
GO
IF COL_LENGTH('dbo.WorkOrderItems', 'StockMovementId') IS NULL
    ALTER TABLE dbo.WorkOrderItems ADD StockMovementId INT NULL; -- soft link, no FK
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_WorkOrderItems_PartId')
    CREATE INDEX IX_WorkOrderItems_PartId ON dbo.WorkOrderItems(PartId);
GO
