-- =====================================================================
-- Migration 007: Link Trips to Customers.
--
-- Adds an optional dbo.Trips.CustomerId -> dbo.Customers(Id). A trip
-- does not require a customer (internal / repositioning runs have none),
-- so the column is nullable and the FK is ON DELETE SET NULL.
--
-- Safe to re-run: every statement is guarded with an existence check.
-- =====================================================================

USE FleetMasterDb;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Trips') AND name = 'CustomerId')
BEGIN
    ALTER TABLE dbo.Trips ADD CustomerId INT NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Trips_Customers_CustomerId')
BEGIN
    ALTER TABLE dbo.Trips
        ADD CONSTRAINT FK_Trips_Customers_CustomerId FOREIGN KEY (CustomerId)
            REFERENCES dbo.Customers(Id) ON DELETE SET NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Trips_CustomerId' AND object_id = OBJECT_ID('dbo.Trips'))
BEGIN
    CREATE INDEX IX_Trips_CustomerId ON dbo.Trips(CustomerId);
END
GO
