-- =====================================================================
-- Migration 009: Trip expenses.
--
-- Adds dbo.TripExpenses -- cost lines against a trip (toll, parking,
-- loading/unloading, driver allowance, en-route repair, fine, etc.).
-- A child of dbo.Trips, ON DELETE CASCADE (mirrors dbo.WorkOrderItems).
--
-- Per-trip P&L (revenue - fuel - expenses - driver pay) is computed at
-- read time by TripsController, so nothing is stored for it here.
--
-- Safe to re-run: every statement is guarded with an existence check.
-- =====================================================================

USE FleetMasterDb;
GO

IF OBJECT_ID('dbo.TripExpenses', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.TripExpenses
    (
        Id              INT IDENTITY(1,1) NOT NULL,
        TripId          INT             NOT NULL,
        Category        INT             NOT NULL,
        Amount          DECIMAL(18,2)   NOT NULL,
        Date            DATE            NOT NULL,
        PaidBy          INT             NOT NULL,
        ReceiptNumber   NVARCHAR(MAX)   NULL,
        Notes           NVARCHAR(MAX)   NULL,
        CONSTRAINT PK_TripExpenses PRIMARY KEY (Id),
        CONSTRAINT FK_TripExpenses_Trips_TripId FOREIGN KEY (TripId)
            REFERENCES dbo.Trips(Id) ON DELETE CASCADE
    );
    CREATE INDEX IX_TripExpenses_TripId ON dbo.TripExpenses(TripId);
END
GO
