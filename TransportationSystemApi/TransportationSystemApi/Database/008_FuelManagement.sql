-- =====================================================================
-- Migration 008: Fuel Management.
--
-- Adds dbo.FuelEntries -- one row per fuel fill. References a Vehicle
-- (required, cascade) and optionally a Driver and a Trip (both nullable,
-- ON DELETE SET NULL so removing a driver/trip keeps the fuel history).
--
-- Mileage / cost-per-km are derived at read time from the odometer gap
-- to the previous entry, so no computed columns are stored here beyond
-- TotalCost (Litres * RatePerLitre), which the API always recomputes.
--
-- FK note: TripId uses ON DELETE NO ACTION (not SET NULL) to avoid a
-- multiple-cascade-path conflict with Vehicles/Drivers, which already
-- cascade into Trips. TripsController nulls FuelEntries.TripId in code
-- before deleting a trip.
--
-- Safe to re-run: every statement is guarded with an existence check.
-- =====================================================================

USE FleetMasterDb;
GO

IF OBJECT_ID('dbo.FuelEntries', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.FuelEntries
    (
        Id              INT IDENTITY(1,1) NOT NULL,
        FuelEntryCode   NVARCHAR(450)   NOT NULL,
        VehicleId       INT             NOT NULL,
        DriverId        INT             NULL,
        TripId          INT             NULL,
        Date            DATE            NOT NULL,
        OdometerReading DECIMAL(18,2)   NOT NULL,
        Litres          DECIMAL(18,2)   NOT NULL,
        RatePerLitre    DECIMAL(18,2)   NOT NULL,
        TotalCost       DECIMAL(18,2)   NOT NULL,
        FuelType        INT             NOT NULL,
        PaymentMode     INT             NOT NULL,
        StationName     NVARCHAR(MAX)   NULL,
        SlipNumber      NVARCHAR(MAX)   NULL,
        IsTankFull      BIT             NOT NULL CONSTRAINT DF_FuelEntries_IsTankFull DEFAULT (1),
        Notes           NVARCHAR(MAX)   NULL,
        CreatedAt       DATETIME2       NOT NULL CONSTRAINT DF_FuelEntries_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt       DATETIME2       NULL,
        CONSTRAINT PK_FuelEntries PRIMARY KEY (Id),
        CONSTRAINT FK_FuelEntries_Vehicles_VehicleId FOREIGN KEY (VehicleId)
            REFERENCES dbo.Vehicles(Id) ON DELETE CASCADE,
        CONSTRAINT FK_FuelEntries_Drivers_DriverId FOREIGN KEY (DriverId)
            REFERENCES dbo.Drivers(Id) ON DELETE SET NULL,
        CONSTRAINT FK_FuelEntries_Trips_TripId FOREIGN KEY (TripId)
            REFERENCES dbo.Trips(Id) ON DELETE NO ACTION
    );
    CREATE UNIQUE INDEX IX_FuelEntries_FuelEntryCode ON dbo.FuelEntries(FuelEntryCode);
    CREATE INDEX IX_FuelEntries_VehicleId_Date ON dbo.FuelEntries(VehicleId, Date);
    CREATE INDEX IX_FuelEntries_DriverId ON dbo.FuelEntries(DriverId);
    CREATE INDEX IX_FuelEntries_TripId ON dbo.FuelEntries(TripId);
END
GO
