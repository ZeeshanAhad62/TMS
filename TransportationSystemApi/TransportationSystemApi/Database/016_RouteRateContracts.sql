-- =====================================================================
-- Migration 016: Route & Rate-Contract Management.
--
-- dbo.Routes         -- lane master: origin, destination, distance, a
--                        standard rate used as the trip-revenue default when
--                        no customer contract applies.
-- dbo.RateContracts  -- customer x route (x optional vehicle type) -> rate,
--                        with a validity window. The most specific contract
--                        valid on the trip's start date wins.
--
-- dbo.Trips gains a nullable RouteId (ON DELETE SET NULL): picking a route on
-- the trip editor fills origin/destination and suggests revenue from the
-- resolved rate (see RateContractsController.Quote). Existing trips keep
-- their free-text origin/destination and are unaffected.
--
-- The entity is modelled as `RouteMaster` in code (table still `Routes`) to
-- avoid clashing with Microsoft.AspNetCore.Mvc.RouteAttribute in controllers.
--
-- Safe to re-run: every statement is guarded with an existence check.
-- =====================================================================

USE FleetMasterDb;
GO

IF OBJECT_ID('dbo.Routes', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Routes
    (
        Id                      INT IDENTITY(1,1) NOT NULL,
        RouteCode               NVARCHAR(450)   NOT NULL,
        Name                    NVARCHAR(200)   NULL,
        Origin                  NVARCHAR(150)   NOT NULL,
        Destination             NVARCHAR(150)   NOT NULL,
        DistanceKm              DECIMAL(10,2)   NULL,
        EstimatedDurationHours  DECIMAL(6,2)    NULL,
        StandardRate            DECIMAL(18,2)   NULL,
        IsActive                BIT             NOT NULL CONSTRAINT DF_Routes_IsActive DEFAULT (1),
        Notes                   NVARCHAR(MAX)   NULL,
        CreatedAt               DATETIME2       NOT NULL CONSTRAINT DF_Routes_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt               DATETIME2       NULL,
        CONSTRAINT PK_Routes PRIMARY KEY (Id)
    );
    CREATE UNIQUE INDEX IX_Routes_RouteCode ON dbo.Routes(RouteCode);
    CREATE INDEX IX_Routes_OriginDestination ON dbo.Routes(Origin, Destination);
END
GO

IF OBJECT_ID('dbo.RateContracts', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.RateContracts
    (
        Id              INT IDENTITY(1,1) NOT NULL,
        ContractCode    NVARCHAR(450)   NOT NULL,
        CustomerId      INT             NOT NULL,
        RouteId         INT             NOT NULL,
        VehicleType     INT             NULL,   -- NULL = applies to any vehicle type
        Rate            DECIMAL(18,2)   NOT NULL,
        RateBasis       INT             NOT NULL CONSTRAINT DF_RateContracts_Basis DEFAULT (0), -- 0 PerTrip, 1 PerKm
        ValidFrom       DATE            NOT NULL,
        ValidTo         DATE            NULL,
        IsActive        BIT             NOT NULL CONSTRAINT DF_RateContracts_IsActive DEFAULT (1),
        Notes           NVARCHAR(MAX)   NULL,
        CreatedAt       DATETIME2       NOT NULL CONSTRAINT DF_RateContracts_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt       DATETIME2       NULL,
        CONSTRAINT PK_RateContracts PRIMARY KEY (Id),
        CONSTRAINT FK_RateContracts_Customers_CustomerId FOREIGN KEY (CustomerId)
            REFERENCES dbo.Customers(Id) ON DELETE CASCADE,
        CONSTRAINT FK_RateContracts_Routes_RouteId FOREIGN KEY (RouteId)
            REFERENCES dbo.Routes(Id) ON DELETE CASCADE
    );
    CREATE UNIQUE INDEX IX_RateContracts_ContractCode ON dbo.RateContracts(ContractCode);
    CREATE INDEX IX_RateContracts_Customer_Route ON dbo.RateContracts(CustomerId, RouteId);
END
GO

IF COL_LENGTH('dbo.Trips', 'RouteId') IS NULL
    ALTER TABLE dbo.Trips ADD RouteId INT NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Trips_Routes_RouteId')
    ALTER TABLE dbo.Trips ADD CONSTRAINT FK_Trips_Routes_RouteId FOREIGN KEY (RouteId)
        REFERENCES dbo.Routes(Id) ON DELETE SET NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Trips_RouteId')
    CREATE INDEX IX_Trips_RouteId ON dbo.Trips(RouteId);
GO
