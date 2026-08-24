-- =====================================================================
-- Migration 004: Trip / Booking management.
--
-- Replaces the old dbo.BookingRecords stub (a placeholder that predated
-- the real Drivers module) with a proper dbo.Trips table that references
-- both a Vehicle and a Driver.
--
-- Safe to re-run: every statement is guarded with an existence check.
-- =====================================================================

USE FleetMasterDb;
GO

-- ---------------------------------------------------------------------
-- Trips
-- ---------------------------------------------------------------------
IF OBJECT_ID('dbo.Trips', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Trips
    (
        Id              INT IDENTITY(1,1) NOT NULL,
        TripCode        NVARCHAR(450)   NOT NULL,
        VehicleId       INT             NOT NULL,
        DriverId        INT             NOT NULL,
        Origin          NVARCHAR(MAX)   NOT NULL,
        Destination     NVARCHAR(MAX)   NOT NULL,
        StartDate       DATE            NOT NULL,
        EndDate         DATE            NULL,
        Status          INT             NOT NULL,
        Notes           NVARCHAR(MAX)   NULL,
        Revenue         DECIMAL(18,2)   NULL,
        CreatedAt       DATETIME2       NOT NULL CONSTRAINT DF_Trips_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt       DATETIME2       NULL,
        CONSTRAINT PK_Trips PRIMARY KEY (Id),
        CONSTRAINT FK_Trips_Vehicles_VehicleId FOREIGN KEY (VehicleId)
            REFERENCES dbo.Vehicles(Id) ON DELETE CASCADE,
        CONSTRAINT FK_Trips_Drivers_DriverId FOREIGN KEY (DriverId)
            REFERENCES dbo.Drivers(Id) ON DELETE CASCADE
    );
    CREATE UNIQUE INDEX IX_Trips_TripCode ON dbo.Trips(TripCode);
    CREATE INDEX IX_Trips_VehicleId ON dbo.Trips(VehicleId);
    CREATE INDEX IX_Trips_DriverId ON dbo.Trips(DriverId);
END
GO

-- ---------------------------------------------------------------------
-- Drop the old BookingRecords stub -- superseded by dbo.Trips above.
-- ---------------------------------------------------------------------
IF OBJECT_ID('dbo.BookingRecords', 'U') IS NOT NULL
BEGIN
    DROP TABLE dbo.BookingRecords;
END
GO
