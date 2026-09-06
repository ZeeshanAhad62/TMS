-- =====================================================================
-- Migration 017: Vendor / Procurement / Purchase Orders.
--
-- dbo.Vendors             -- supplier master (parts supplier, workshop, fuel
--                            station, transporter, other). This is the
--                            "Suppliers" table module 13 deferred to here.
-- dbo.PurchaseOrders      -- one PO raised against a vendor
-- dbo.PurchaseOrderLines  -- child lines (ON DELETE CASCADE); optional PartId
--                            link to the Parts master (ON DELETE SET NULL).
--                            QuantityReceived tracks partial goods receipts.
--
-- Goods receipt: POST api/purchase-orders/{id}/receive increments each line's
-- QuantityReceived and, for lines linked to a stocked Part, writes a Receipt
-- StockMovement with ReferenceType = PurchaseOrder (2) / ReferenceId = the PO.
-- No schema change was needed for that -- StockMovements.ReferenceType/
-- ReferenceId were already generic (see migration 013).
--
-- Money (SubTotal / TaxAmount / Total) and the effective received status are
-- computed at read time (PurchaseOrderMapper); only raw inputs are stored.
--
-- Safe to re-run: every statement is guarded with an existence check.
-- =====================================================================

USE FleetMasterDb;
GO

IF OBJECT_ID('dbo.Vendors', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Vendors
    (
        Id                  INT IDENTITY(1,1) NOT NULL,
        VendorCode          NVARCHAR(450)   NOT NULL,
        Name                NVARCHAR(200)   NOT NULL,
        VendorType          INT             NOT NULL CONSTRAINT DF_Vendors_Type DEFAULT (0),
        ContactPerson       NVARCHAR(150)   NULL,
        Phone               NVARCHAR(40)    NULL,
        Email               NVARCHAR(150)   NULL,
        Address             NVARCHAR(MAX)   NULL,
        TaxNumber           NVARCHAR(60)    NULL,
        PaymentTermsDays    INT             NULL,
        IsActive            BIT             NOT NULL CONSTRAINT DF_Vendors_IsActive DEFAULT (1),
        Notes               NVARCHAR(MAX)   NULL,
        CreatedAt           DATETIME2       NOT NULL CONSTRAINT DF_Vendors_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt           DATETIME2       NULL,
        CONSTRAINT PK_Vendors PRIMARY KEY (Id)
    );
    CREATE UNIQUE INDEX IX_Vendors_VendorCode ON dbo.Vendors(VendorCode);
    CREATE INDEX IX_Vendors_VendorType ON dbo.Vendors(VendorType);
END
GO

IF OBJECT_ID('dbo.PurchaseOrders', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.PurchaseOrders
    (
        Id              INT IDENTITY(1,1) NOT NULL,
        PoNumber        NVARCHAR(450)   NOT NULL,
        VendorId        INT             NOT NULL,
        OrderDate       DATE            NOT NULL,
        ExpectedDate    DATE            NULL,
        Status          INT             NOT NULL CONSTRAINT DF_PurchaseOrders_Status DEFAULT (0),
        TaxPercent      DECIMAL(9,4)    NOT NULL CONSTRAINT DF_PurchaseOrders_TaxPercent DEFAULT (0),
        Notes           NVARCHAR(MAX)   NULL,
        CreatedAt       DATETIME2       NOT NULL CONSTRAINT DF_PurchaseOrders_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt       DATETIME2       NULL,
        CONSTRAINT PK_PurchaseOrders PRIMARY KEY (Id),
        CONSTRAINT FK_PurchaseOrders_Vendors_VendorId FOREIGN KEY (VendorId)
            REFERENCES dbo.Vendors(Id) ON DELETE CASCADE
    );
    CREATE UNIQUE INDEX IX_PurchaseOrders_PoNumber ON dbo.PurchaseOrders(PoNumber);
    CREATE INDEX IX_PurchaseOrders_VendorId ON dbo.PurchaseOrders(VendorId);
END
GO

IF OBJECT_ID('dbo.PurchaseOrderLines', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.PurchaseOrderLines
    (
        Id                  INT IDENTITY(1,1) NOT NULL,
        PurchaseOrderId     INT             NOT NULL,
        PartId              INT             NULL,
        Description         NVARCHAR(300)   NOT NULL,
        Quantity            DECIMAL(18,2)   NOT NULL,
        UnitPrice           DECIMAL(18,2)   NOT NULL,
        QuantityReceived    DECIMAL(18,2)   NOT NULL CONSTRAINT DF_PurchaseOrderLines_QtyRecd DEFAULT (0),
        CONSTRAINT PK_PurchaseOrderLines PRIMARY KEY (Id),
        CONSTRAINT FK_PurchaseOrderLines_PurchaseOrders_PurchaseOrderId FOREIGN KEY (PurchaseOrderId)
            REFERENCES dbo.PurchaseOrders(Id) ON DELETE CASCADE,
        CONSTRAINT FK_PurchaseOrderLines_Parts_PartId FOREIGN KEY (PartId)
            REFERENCES dbo.Parts(Id) ON DELETE SET NULL
    );
    CREATE INDEX IX_PurchaseOrderLines_PurchaseOrderId ON dbo.PurchaseOrderLines(PurchaseOrderId);
    CREATE INDEX IX_PurchaseOrderLines_PartId ON dbo.PurchaseOrderLines(PartId);
END
GO
