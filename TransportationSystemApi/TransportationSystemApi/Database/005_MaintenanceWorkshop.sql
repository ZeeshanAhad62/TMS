-- =====================================================================
-- Migration 005: Maintenance & Workshop.
--
-- Adds standalone workshop job cards (dbo.WorkOrders) plus their parts /
-- materials line items (dbo.WorkOrderItems). A WorkOrder references a
-- Vehicle but is not a nested child of it (mirrors dbo.Trips). The
-- pre-existing dbo.MaintenanceRecords quick-log is left untouched.
--
-- Safe to re-run: every statement is guarded with an existence check.
-- =====================================================================

USE FleetMasterDb;
GO

-- ---------------------------------------------------------------------
-- WorkOrders
-- ---------------------------------------------------------------------
IF OBJECT_ID('dbo.WorkOrders', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.WorkOrders
    (
        Id              INT IDENTITY(1,1) NOT NULL,
        WorkOrderCode   NVARCHAR(450)   NOT NULL,
        VehicleId       INT             NOT NULL,
        Type            INT             NOT NULL,
        Priority        INT             NOT NULL,
        Status          INT             NOT NULL,
        ReportedDate    DATE            NOT NULL,
        ScheduledDate   DATE            NULL,
        CompletedDate   DATE            NULL,
        Odometer        DECIMAL(18,2)   NULL,
        Workshop        NVARCHAR(MAX)   NULL,
        Description     NVARCHAR(MAX)   NULL,
        Notes           NVARCHAR(MAX)   NULL,
        LabourCost      DECIMAL(18,2)   NULL,
        CreatedAt       DATETIME2       NOT NULL CONSTRAINT DF_WorkOrders_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt       DATETIME2       NULL,
        CONSTRAINT PK_WorkOrders PRIMARY KEY (Id),
        CONSTRAINT FK_WorkOrders_Vehicles_VehicleId FOREIGN KEY (VehicleId)
            REFERENCES dbo.Vehicles(Id) ON DELETE CASCADE
    );
    CREATE UNIQUE INDEX IX_WorkOrders_WorkOrderCode ON dbo.WorkOrders(WorkOrderCode);
    CREATE INDEX IX_WorkOrders_VehicleId ON dbo.WorkOrders(VehicleId);
END
GO

-- ---------------------------------------------------------------------
-- WorkOrderItems
-- ---------------------------------------------------------------------
IF OBJECT_ID('dbo.WorkOrderItems', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.WorkOrderItems
    (
        Id              INT IDENTITY(1,1) NOT NULL,
        WorkOrderId     INT             NOT NULL,
        Description     NVARCHAR(MAX)   NOT NULL,
        Quantity        DECIMAL(18,2)   NOT NULL,
        UnitCost        DECIMAL(18,2)   NOT NULL,
        CONSTRAINT PK_WorkOrderItems PRIMARY KEY (Id),
        CONSTRAINT FK_WorkOrderItems_WorkOrders_WorkOrderId FOREIGN KEY (WorkOrderId)
            REFERENCES dbo.WorkOrders(Id) ON DELETE CASCADE
    );
    CREATE INDEX IX_WorkOrderItems_WorkOrderId ON dbo.WorkOrderItems(WorkOrderId);
END
GO
