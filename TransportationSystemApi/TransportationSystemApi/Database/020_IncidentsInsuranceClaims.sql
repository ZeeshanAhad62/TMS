-- =====================================================================
-- Migration 020: Accident / Incident & Insurance-Claims tracking.
--
-- dbo.Incidents        -- an accident / theft / damage event on a vehicle
--                         (ON DELETE CASCADE from Vehicles, matching the
--                         Documents / Tyres / WorkOrders pattern). DriverId is
--                         ON DELETE SET NULL; TripId is a soft link (no FK,
--                         frozen record, same rationale as InvoiceLines.TripId).
-- dbo.InsuranceClaims  -- one or more claims per incident (own-damage,
--                         third-party, ...). Amounts approved / received and a
--                         settlement date; NetReceivable is derived at read
--                         time (IncidentMapper).
-- dbo.IncidentPhotos   -- scene / damage photos; files on disk under
--                         wwwroot/uploads/incidents/{id}/ like driver docs.
--
-- Safe to re-run: every statement is guarded with an existence check.
-- =====================================================================

USE FleetMasterDb;
GO

IF OBJECT_ID('dbo.Incidents', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Incidents
    (
        Id                      INT IDENTITY(1,1) NOT NULL,
        IncidentCode            NVARCHAR(450)   NOT NULL,
        VehicleId               INT             NOT NULL,
        DriverId                INT             NULL,
        TripId                  INT             NULL,   -- soft link, no FK

        OccurredAt              DATETIME2       NOT NULL,
        Location                NVARCHAR(300)   NULL,
        Latitude                DECIMAL(9,6)    NULL,
        Longitude               DECIMAL(9,6)    NULL,

        IncidentType            INT             NOT NULL CONSTRAINT DF_Incidents_Type DEFAULT (0),
        Severity                INT             NOT NULL CONSTRAINT DF_Incidents_Severity DEFAULT (0),
        Status                  INT             NOT NULL CONSTRAINT DF_Incidents_Status DEFAULT (0),

        Description             NVARCHAR(MAX)   NULL,
        ThirdPartyInvolved      BIT             NOT NULL CONSTRAINT DF_Incidents_ThirdParty DEFAULT (0),
        ThirdPartyDetails       NVARCHAR(MAX)   NULL,
        PoliceReportNumber      NVARCHAR(100)   NULL,
        EstimatedRepairCost     DECIMAL(18,2)   NULL,
        Notes                   NVARCHAR(MAX)   NULL,

        CreatedAt               DATETIME2       NOT NULL CONSTRAINT DF_Incidents_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt               DATETIME2       NULL,
        CONSTRAINT PK_Incidents PRIMARY KEY (Id),
        CONSTRAINT FK_Incidents_Vehicles_VehicleId FOREIGN KEY (VehicleId)
            REFERENCES dbo.Vehicles(Id) ON DELETE CASCADE,
        CONSTRAINT FK_Incidents_Drivers_DriverId FOREIGN KEY (DriverId)
            REFERENCES dbo.Drivers(Id) ON DELETE SET NULL
    );
    CREATE UNIQUE INDEX IX_Incidents_IncidentCode ON dbo.Incidents(IncidentCode);
    CREATE INDEX IX_Incidents_VehicleId ON dbo.Incidents(VehicleId);
    CREATE INDEX IX_Incidents_DriverId ON dbo.Incidents(DriverId);
    CREATE INDEX IX_Incidents_Status ON dbo.Incidents(Status);
END
GO

IF OBJECT_ID('dbo.InsuranceClaims', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.InsuranceClaims
    (
        Id                  INT IDENTITY(1,1) NOT NULL,
        ClaimCode           NVARCHAR(450)   NOT NULL,
        IncidentId          INT             NOT NULL,
        InsurerName         NVARCHAR(200)   NULL,
        PolicyNumber        NVARCHAR(100)   NULL,
        ClaimNumber         NVARCHAR(100)   NULL,   -- the insurer's own reference
        ClaimDate           DATE            NOT NULL,
        AmountClaimed       DECIMAL(18,2)   NOT NULL CONSTRAINT DF_InsuranceClaims_Claimed DEFAULT (0),
        AmountApproved      DECIMAL(18,2)   NULL,
        AmountReceived      DECIMAL(18,2)   NULL,
        Deductible          DECIMAL(18,2)   NULL,
        Status              INT             NOT NULL CONSTRAINT DF_InsuranceClaims_Status DEFAULT (0),
        SettledDate         DATE            NULL,
        Notes               NVARCHAR(MAX)   NULL,
        CreatedAt           DATETIME2       NOT NULL CONSTRAINT DF_InsuranceClaims_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt           DATETIME2       NULL,
        CONSTRAINT PK_InsuranceClaims PRIMARY KEY (Id),
        CONSTRAINT FK_InsuranceClaims_Incidents_IncidentId FOREIGN KEY (IncidentId)
            REFERENCES dbo.Incidents(Id) ON DELETE CASCADE
    );
    CREATE UNIQUE INDEX IX_InsuranceClaims_ClaimCode ON dbo.InsuranceClaims(ClaimCode);
    CREATE INDEX IX_InsuranceClaims_IncidentId ON dbo.InsuranceClaims(IncidentId);
END
GO

IF OBJECT_ID('dbo.IncidentPhotos', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.IncidentPhotos
    (
        Id              INT IDENTITY(1,1) NOT NULL,
        IncidentId      INT             NOT NULL,
        FileName        NVARCHAR(260)   NOT NULL,
        ContentType     NVARCHAR(120)   NOT NULL,
        StoragePath     NVARCHAR(400)   NOT NULL,
        FileSizeBytes   BIGINT          NOT NULL,
        Caption         NVARCHAR(200)   NULL,
        UploadedAt      DATETIME2       NOT NULL CONSTRAINT DF_IncidentPhotos_UploadedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_IncidentPhotos PRIMARY KEY (Id),
        CONSTRAINT FK_IncidentPhotos_Incidents_IncidentId FOREIGN KEY (IncidentId)
            REFERENCES dbo.Incidents(Id) ON DELETE CASCADE
    );
    CREATE INDEX IX_IncidentPhotos_IncidentId ON dbo.IncidentPhotos(IncidentId);
END
GO
