-- =====================================================================
-- Migration 015: GPS / Live Tracking integration.
--
-- dbo.VehiclePositions -- raw position reports, one row per fix. The webhook
--                          (POST api/tracking/ingest) writes here; older rows
--                          past Tracking:MaxHotPositionsPerVehicle are pruned
--                          on ingest so the "hot" set stays small.
-- dbo.Geofences        -- named Circle (centre + radius) or Polygon (JSON
--                          [{lat,lng},...]) areas.
-- dbo.GeofenceEvents   -- Enter / Exit transitions, derived on ingest by
--                          comparing each fix against the previous state.
--
-- Provider-agnostic: the ingest endpoint takes a normalised report shape and
-- matches on VehicleId. A real telematics provider is wired by an adapter
-- that maps its payload/device-id to that shape -- no schema change needed.
-- The polling BackgroundService in the roadmap stays deferred until a
-- provider is chosen (nothing to poll yet), same as the SMS channel.
--
-- Safe to re-run: every statement is guarded with an existence check.
-- =====================================================================

USE FleetMasterDb;
GO

IF OBJECT_ID('dbo.VehiclePositions', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.VehiclePositions
    (
        Id              BIGINT IDENTITY(1,1) NOT NULL,
        VehicleId       INT             NOT NULL,
        Latitude        DECIMAL(9,6)    NOT NULL,
        Longitude       DECIMAL(9,6)    NOT NULL,
        SpeedKph        DECIMAL(6,2)    NULL,
        Heading         DECIMAL(6,2)    NULL,
        Ignition        BIT             NULL,
        DeviceTimeUtc   DATETIME2       NOT NULL,
        Source          NVARCHAR(50)    NULL,
        CreatedAt       DATETIME2       NOT NULL CONSTRAINT DF_VehiclePositions_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_VehiclePositions PRIMARY KEY (Id),
        CONSTRAINT FK_VehiclePositions_Vehicles_VehicleId FOREIGN KEY (VehicleId)
            REFERENCES dbo.Vehicles(Id) ON DELETE CASCADE
    );
    CREATE INDEX IX_VehiclePositions_Vehicle_DeviceTime
        ON dbo.VehiclePositions(VehicleId, DeviceTimeUtc DESC);
END
GO

IF OBJECT_ID('dbo.Geofences', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Geofences
    (
        Id              INT IDENTITY(1,1) NOT NULL,
        Name            NVARCHAR(150)   NOT NULL,
        Shape           INT             NOT NULL CONSTRAINT DF_Geofences_Shape DEFAULT (0), -- 0 Circle, 1 Polygon
        CenterLat       DECIMAL(9,6)    NULL,
        CenterLng       DECIMAL(9,6)    NULL,
        RadiusMeters    DECIMAL(10,2)   NULL,
        PolygonJson     NVARCHAR(MAX)   NULL,   -- [{"lat":..,"lng":..}, ...]
        IsActive        BIT             NOT NULL CONSTRAINT DF_Geofences_IsActive DEFAULT (1),
        Notes           NVARCHAR(MAX)   NULL,
        CreatedAt       DATETIME2       NOT NULL CONSTRAINT DF_Geofences_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt       DATETIME2       NULL,
        CONSTRAINT PK_Geofences PRIMARY KEY (Id)
    );
    CREATE UNIQUE INDEX IX_Geofences_Name ON dbo.Geofences(Name);
END
GO

IF OBJECT_ID('dbo.GeofenceEvents', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.GeofenceEvents
    (
        Id              BIGINT IDENTITY(1,1) NOT NULL,
        GeofenceId      INT             NOT NULL,
        VehicleId       INT             NOT NULL,
        EventType       INT             NOT NULL,   -- 0 Enter, 1 Exit
        OccurredAtUtc   DATETIME2       NOT NULL,
        Latitude        DECIMAL(9,6)    NOT NULL,
        Longitude       DECIMAL(9,6)    NOT NULL,
        CreatedAt       DATETIME2       NOT NULL CONSTRAINT DF_GeofenceEvents_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_GeofenceEvents PRIMARY KEY (Id),
        CONSTRAINT FK_GeofenceEvents_Geofences_GeofenceId FOREIGN KEY (GeofenceId)
            REFERENCES dbo.Geofences(Id) ON DELETE CASCADE,
        CONSTRAINT FK_GeofenceEvents_Vehicles_VehicleId FOREIGN KEY (VehicleId)
            REFERENCES dbo.Vehicles(Id) ON DELETE NO ACTION  -- avoid multi-cascade path (Geofences already cascades)
    );
    CREATE INDEX IX_GeofenceEvents_Vehicle_OccurredAt
        ON dbo.GeofenceEvents(VehicleId, OccurredAtUtc DESC);
    CREATE INDEX IX_GeofenceEvents_Geofence_Vehicle
        ON dbo.GeofenceEvents(GeofenceId, VehicleId, OccurredAtUtc DESC);
END
GO
