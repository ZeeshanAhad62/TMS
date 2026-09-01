-- =====================================================================
-- Migration 006: Customer / Client Master.
--
-- Adds a standalone dbo.Customers table -- the master list of billing
-- customers. Trips and (later) invoices reference a customer, so this
-- table has no parent of its own (mirrors dbo.Drivers).
--
-- Safe to re-run: every statement is guarded with an existence check.
-- =====================================================================

USE FleetMasterDb;
GO

-- ---------------------------------------------------------------------
-- Customers
-- ---------------------------------------------------------------------
IF OBJECT_ID('dbo.Customers', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Customers
    (
        Id                INT IDENTITY(1,1) NOT NULL,
        CustomerCode      NVARCHAR(450)   NOT NULL,
        Name              NVARCHAR(MAX)   NOT NULL,
        ContactPerson     NVARCHAR(MAX)   NULL,
        Phone             NVARCHAR(30)    NOT NULL,
        Email             NVARCHAR(MAX)   NULL,
        BillingAddress    NVARCHAR(MAX)   NULL,
        TaxNumber         NVARCHAR(MAX)   NULL,
        CreditLimit       DECIMAL(18,2)   NULL,
        PaymentTermsDays  INT             NULL,
        Status            INT             NOT NULL,
        Notes             NVARCHAR(MAX)   NULL,
        CreatedAt         DATETIME2       NOT NULL CONSTRAINT DF_Customers_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt         DATETIME2       NULL,
        CONSTRAINT PK_Customers PRIMARY KEY (Id)
    );
    CREATE UNIQUE INDEX IX_Customers_CustomerCode ON dbo.Customers(CustomerCode);
END
GO
