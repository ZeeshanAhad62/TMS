-- =====================================================================
-- Transportation Management System - FleetMasterDb schema (DB-first)
--
-- This script is the single source of truth for the database schema.
-- The EF Core entity classes under Models/ and the FleetDbContext
-- mapping are hand-kept in sync with this script. There are no EF
-- Core migrations in this project -- schema changes are made here
-- first, then applied with:
--
--   sqlcmd -S "(localdb)\MSSQLLocalDB" -d FleetMasterDb -i 001_Schema.sql
--
-- Safe to re-run: every statement is guarded with an existence check.
-- =====================================================================

IF DB_ID('FleetMasterDb') IS NULL
BEGIN
    CREATE DATABASE FleetMasterDb;
END
GO

USE FleetMasterDb;
GO

-- ---------------------------------------------------------------------
-- Vehicles (Fleet Master)
-- ---------------------------------------------------------------------
IF OBJECT_ID('dbo.Vehicles', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Vehicles
    (
        Id                          INT IDENTITY(1,1) NOT NULL,
        VehicleCode                 NVARCHAR(450)   NOT NULL,
        RegistrationNumber          NVARCHAR(450)   NOT NULL,
        VehicleType                 INT             NOT NULL,
        Make                        NVARCHAR(MAX)   NULL,
        Model                       NVARCHAR(MAX)   NULL,
        Variant                     NVARCHAR(MAX)   NULL,
        YearOfManufacture           INT             NULL,
        OwnershipType               INT             NOT NULL,
        FuelType                    INT             NOT NULL,
        LoadCapacity                DECIMAL(18,2)   NULL,
        LoadCapacityUnit            NVARCHAR(MAX)   NULL,
        ChassisNumber               NVARCHAR(MAX)   NULL,
        EngineNumber                NVARCHAR(MAX)   NULL,
        BodyType                    NVARCHAR(MAX)   NULL,
        AxleCount                   INT             NULL,
        TrailerType                 NVARCHAR(MAX)   NULL,
        ContainerLiftCapacity       DECIMAL(18,2)   NULL,
        SeatingCapacity             INT             NULL,
        RCNumber                    NVARCHAR(MAX)   NULL,
        RCExpiryDate                DATE            NULL,
        FitnessCertificateNo        NVARCHAR(MAX)   NULL,
        FitnessExpiryDate           DATE            NULL,
        RoutePermitNo               NVARCHAR(MAX)   NULL,
        PermitExpiryDate            DATE            NULL,
        InsurancePolicyNo           NVARCHAR(MAX)   NULL,
        InsuranceProvider           NVARCHAR(MAX)   NULL,
        InsuranceExpiryDate         DATE            NULL,
        PollutionCertNo             NVARCHAR(MAX)   NULL,
        PollutionCertExpiryDate     DATE            NULL,
        TaxPaidTill                 DATE            NULL,
        CurrentStatus                INT            NOT NULL,
        CurrentLocation              NVARCHAR(MAX)  NULL,
        AssignedDriver               NVARCHAR(MAX)  NULL,
        CurrentBookingReference      NVARCHAR(MAX)  NULL,
        CurrentOdometerReading       DECIMAL(18,2)  NULL,
        FuelConsumptionAverage       DECIMAL(18,2)  NULL,
        LastOilChangeDate            DATE           NULL,
        LastOilChangeOdometer        DECIMAL(18,2)  NULL,
        NextOilChangeDueDate         DATE           NULL,
        NextOilChangeDueOdometer     DECIMAL(18,2)  NULL,
        LastServiceDate              DATE           NULL,
        ServiceIntervalKm            DECIMAL(18,2)  NULL,
        ServiceIntervalMonths        INT            NULL,
        BatteryReplacementDate       DATE           NULL,
        NumberOfTyres                INT            NULL,
        PurchasePrice                DECIMAL(18,2)  NULL,
        DepreciationInfo             NVARCHAR(MAX)  NULL,
        RunningCostPerKm             DECIMAL(18,2)  NULL,
        FuelCostTracking             DECIMAL(18,2)  NULL,
        CreatedAt                    DATETIME2      NOT NULL CONSTRAINT DF_Vehicles_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt                    DATETIME2      NULL,
        CONSTRAINT PK_Vehicles PRIMARY KEY (Id)
    );
    CREATE UNIQUE INDEX IX_Vehicles_VehicleCode ON dbo.Vehicles(VehicleCode);
    CREATE UNIQUE INDEX IX_Vehicles_RegistrationNumber ON dbo.Vehicles(RegistrationNumber);
END
GO

-- ---------------------------------------------------------------------
-- AlertRules
-- ---------------------------------------------------------------------
IF OBJECT_ID('dbo.AlertRules', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AlertRules
    (
        Id                  INT IDENTITY(1,1) NOT NULL,
        VehicleId           INT             NOT NULL,
        DocumentCategory    INT             NOT NULL,
        ThresholdDays       INT             NOT NULL,
        Channel             INT             NOT NULL,
        RecipientRole       NVARCHAR(MAX)   NULL,
        Status              INT             NOT NULL,
        CONSTRAINT PK_AlertRules PRIMARY KEY (Id),
        CONSTRAINT FK_AlertRules_Vehicles_VehicleId FOREIGN KEY (VehicleId)
            REFERENCES dbo.Vehicles(Id) ON DELETE CASCADE
    );
    CREATE INDEX IX_AlertRules_VehicleId ON dbo.AlertRules(VehicleId);
END
GO

-- ---------------------------------------------------------------------
-- BookingRecords
-- ---------------------------------------------------------------------
IF OBJECT_ID('dbo.BookingRecords', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.BookingRecords
    (
        Id              INT IDENTITY(1,1) NOT NULL,
        VehicleId       INT             NOT NULL,
        TripReference   NVARCHAR(MAX)   NOT NULL,
        StartDate       DATE            NOT NULL,
        EndDate         DATE            NULL,
        Status          INT             NOT NULL,
        Notes           NVARCHAR(MAX)   NULL,
        CONSTRAINT PK_BookingRecords PRIMARY KEY (Id),
        CONSTRAINT FK_BookingRecords_Vehicles_VehicleId FOREIGN KEY (VehicleId)
            REFERENCES dbo.Vehicles(Id) ON DELETE CASCADE
    );
    CREATE INDEX IX_BookingRecords_VehicleId ON dbo.BookingRecords(VehicleId);
END
GO

-- ---------------------------------------------------------------------
-- MaintenanceRecords
-- ---------------------------------------------------------------------
IF OBJECT_ID('dbo.MaintenanceRecords', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.MaintenanceRecords
    (
        Id              INT IDENTITY(1,1) NOT NULL,
        VehicleId       INT             NOT NULL,
        Type            INT             NOT NULL,
        Date            DATE            NOT NULL,
        Odometer        DECIMAL(18,2)   NULL,
        Description     NVARCHAR(MAX)   NULL,
        ServiceVendor   NVARCHAR(MAX)   NULL,
        Cost            DECIMAL(18,2)   NULL,
        CONSTRAINT PK_MaintenanceRecords PRIMARY KEY (Id),
        CONSTRAINT FK_MaintenanceRecords_Vehicles_VehicleId FOREIGN KEY (VehicleId)
            REFERENCES dbo.Vehicles(Id) ON DELETE CASCADE
    );
    CREATE INDEX IX_MaintenanceRecords_VehicleId ON dbo.MaintenanceRecords(VehicleId);
END
GO

-- ---------------------------------------------------------------------
-- Tyres
-- ---------------------------------------------------------------------
IF OBJECT_ID('dbo.Tyres', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Tyres
    (
        Id                      INT IDENTITY(1,1) NOT NULL,
        VehicleId               INT             NOT NULL,
        Position                INT             NOT NULL,
        BrandAndSize            NVARCHAR(MAX)   NULL,
        InstallationDate        DATE            NULL,
        InstallationOdometer    DECIMAL(18,2)   NULL,
        CurrentCondition        NVARCHAR(MAX)   NULL,
        LastRotationDate        DATE            NULL,
        CONSTRAINT PK_Tyres PRIMARY KEY (Id),
        CONSTRAINT FK_Tyres_Vehicles_VehicleId FOREIGN KEY (VehicleId)
            REFERENCES dbo.Vehicles(Id) ON DELETE CASCADE
    );
    CREATE INDEX IX_Tyres_VehicleId ON dbo.Tyres(VehicleId);
END
GO

-- ---------------------------------------------------------------------
-- VehicleDocuments
-- ---------------------------------------------------------------------
IF OBJECT_ID('dbo.VehicleDocuments', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.VehicleDocuments
    (
        Id              INT IDENTITY(1,1) NOT NULL,
        VehicleId       INT             NOT NULL,
        Category        INT             NOT NULL,
        FileName        NVARCHAR(MAX)   NOT NULL,
        ContentType     NVARCHAR(MAX)   NOT NULL,
        StoragePath     NVARCHAR(MAX)   NOT NULL,
        FileSizeBytes   BIGINT          NOT NULL,
        UploadedAt      DATETIME2       NOT NULL CONSTRAINT DF_VehicleDocuments_UploadedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_VehicleDocuments PRIMARY KEY (Id),
        CONSTRAINT FK_VehicleDocuments_Vehicles_VehicleId FOREIGN KEY (VehicleId)
            REFERENCES dbo.Vehicles(Id) ON DELETE CASCADE
    );
    CREATE INDEX IX_VehicleDocuments_VehicleId ON dbo.VehicleDocuments(VehicleId);
END
GO

-- ---------------------------------------------------------------------
-- TyreReplacementHistories
-- ---------------------------------------------------------------------
IF OBJECT_ID('dbo.TyreReplacementHistories', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.TyreReplacementHistories
    (
        Id                      INT IDENTITY(1,1) NOT NULL,
        TyreId                  INT             NOT NULL,
        ReplacedDate            DATE            NOT NULL,
        OdometerAtReplacement   DECIMAL(18,2)   NULL,
        OldBrandAndSize         NVARCHAR(MAX)   NULL,
        NewBrandAndSize         NVARCHAR(MAX)   NULL,
        Reason                  NVARCHAR(MAX)   NULL,
        CONSTRAINT PK_TyreReplacementHistories PRIMARY KEY (Id),
        CONSTRAINT FK_TyreReplacementHistories_Tyres_TyreId FOREIGN KEY (TyreId)
            REFERENCES dbo.Tyres(Id) ON DELETE CASCADE
    );
    CREATE INDEX IX_TyreReplacementHistories_TyreId ON dbo.TyreReplacementHistories(TyreId);
END
GO

-- ---------------------------------------------------------------------
-- Users (per-deployment SaaS tenant users)
-- ---------------------------------------------------------------------
IF OBJECT_ID('dbo.Users', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Users
    (
        Id              INT IDENTITY(1,1) NOT NULL,
        Username        NVARCHAR(100)   NOT NULL,
        Email           NVARCHAR(200)   NOT NULL,
        PasswordHash    NVARCHAR(MAX)   NOT NULL,
        FullName        NVARCHAR(200)   NOT NULL,
        Role            INT             NOT NULL, -- 0=Admin, 1=FleetManager, 2=Viewer
        IsActive        BIT             NOT NULL CONSTRAINT DF_Users_IsActive DEFAULT (1),
        CreatedAt       DATETIME2       NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT (SYSUTCDATETIME()),
        LastLoginAt     DATETIME2       NULL,
        CONSTRAINT PK_Users PRIMARY KEY (Id)
    );
    CREATE UNIQUE INDEX IX_Users_Username ON dbo.Users(Username);
    CREATE UNIQUE INDEX IX_Users_Email ON dbo.Users(Email);
END
GO

-- ---------------------------------------------------------------------
-- CompanyProfile (single-row branding for this client's deployment)
-- ---------------------------------------------------------------------
IF OBJECT_ID('dbo.CompanyProfile', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.CompanyProfile
    (
        Id              INT IDENTITY(1,1) NOT NULL,
        CompanyName     NVARCHAR(200)   NOT NULL,
        LogoPath        NVARCHAR(MAX)   NULL,
        Address         NVARCHAR(500)   NULL,
        ContactEmail    NVARCHAR(200)   NULL,
        ContactPhone    NVARCHAR(50)    NULL,
        UpdatedAt       DATETIME2       NULL,
        CONSTRAINT PK_CompanyProfile PRIMARY KEY (Id)
    );
END
GO
