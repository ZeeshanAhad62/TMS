-- =====================================================================
-- Migration 010: Billing & Invoicing (Accounts Receivable).
--
-- dbo.Invoices          -- one invoice raised against a customer
-- dbo.InvoiceLines      -- child lines (ON DELETE CASCADE)
-- dbo.Payments          -- receipts against an invoice (ON DELETE CASCADE)
--
-- Money fields (SubTotal / TaxAmount / Total / AmountPaid / Balance) and
-- the effective status (Paid / PartiallyPaid) are computed at read time
-- by InvoiceMapper, so only the raw inputs are stored: line qty/price,
-- Invoice.TaxPercent, payment amounts, and the user-set Status
-- (Draft / Sent / Cancelled).
--
-- InvoiceLines.TripId is a SOFT link (indexed, no FK): an issued invoice
-- is a frozen record, so deleting a trip must not cascade into or mutate
-- it, and there is no multi-cascade-path conflict with Trips.
--
-- Safe to re-run: every statement is guarded with an existence check.
-- =====================================================================

USE FleetMasterDb;
GO

IF OBJECT_ID('dbo.Invoices', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Invoices
    (
        Id              INT IDENTITY(1,1) NOT NULL,
        InvoiceNumber   NVARCHAR(450)   NOT NULL,
        CustomerId      INT             NOT NULL,
        InvoiceDate     DATE            NOT NULL,
        DueDate         DATE            NOT NULL,
        Status          INT             NOT NULL,
        TaxPercent      DECIMAL(9,4)    NOT NULL CONSTRAINT DF_Invoices_TaxPercent DEFAULT (0),
        Notes           NVARCHAR(MAX)   NULL,
        CreatedAt       DATETIME2       NOT NULL CONSTRAINT DF_Invoices_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt       DATETIME2       NULL,
        CONSTRAINT PK_Invoices PRIMARY KEY (Id),
        CONSTRAINT FK_Invoices_Customers_CustomerId FOREIGN KEY (CustomerId)
            REFERENCES dbo.Customers(Id) ON DELETE CASCADE
    );
    CREATE UNIQUE INDEX IX_Invoices_InvoiceNumber ON dbo.Invoices(InvoiceNumber);
    CREATE INDEX IX_Invoices_CustomerId ON dbo.Invoices(CustomerId);
END
GO

IF OBJECT_ID('dbo.InvoiceLines', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.InvoiceLines
    (
        Id              INT IDENTITY(1,1) NOT NULL,
        InvoiceId       INT             NOT NULL,
        TripId          INT             NULL,
        Description     NVARCHAR(MAX)   NOT NULL,
        Quantity        DECIMAL(18,2)   NOT NULL,
        UnitPrice       DECIMAL(18,2)   NOT NULL,
        CONSTRAINT PK_InvoiceLines PRIMARY KEY (Id),
        CONSTRAINT FK_InvoiceLines_Invoices_InvoiceId FOREIGN KEY (InvoiceId)
            REFERENCES dbo.Invoices(Id) ON DELETE CASCADE
    );
    CREATE INDEX IX_InvoiceLines_InvoiceId ON dbo.InvoiceLines(InvoiceId);
    CREATE INDEX IX_InvoiceLines_TripId ON dbo.InvoiceLines(TripId);
END
GO

IF OBJECT_ID('dbo.Payments', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Payments
    (
        Id              INT IDENTITY(1,1) NOT NULL,
        InvoiceId       INT             NOT NULL,
        Date            DATE            NOT NULL,
        Amount          DECIMAL(18,2)   NOT NULL,
        Mode            INT             NOT NULL,
        Reference       NVARCHAR(MAX)   NULL,
        Notes           NVARCHAR(MAX)   NULL,
        CONSTRAINT PK_Payments PRIMARY KEY (Id),
        CONSTRAINT FK_Payments_Invoices_InvoiceId FOREIGN KEY (InvoiceId)
            REFERENCES dbo.Invoices(Id) ON DELETE CASCADE
    );
    CREATE INDEX IX_Payments_InvoiceId ON dbo.Payments(InvoiceId);
END
GO
