-- =====================================================================
-- Migration 003: Drivers management (profiles, documents, vehicle
-- assignments).
--
-- Safe to re-run: every statement is guarded with an existence check.
-- =====================================================================

USE FleetMasterDb;
GO

-- ---------------------------------------------------------------------
-- Drivers
-- ---------------------------------------------------------------------
IF OBJECT_ID('dbo.Drivers', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Drivers
    (
        Id                  INT IDENTITY(1,1) NOT NULL,
        DriverCode          NVARCHAR(450)   NOT NULL,
        FullName            NVARCHAR(MAX)   NOT NULL,
        PhoneNumber         NVARCHAR(MAX)   NOT NULL,
        Email               NVARCHAR(MAX)   NULL,
        DateOfBirth         DATE            NULL,
        Address             NVARCHAR(MAX)   NULL,
        LicenseNumber       NVARCHAR(450)   NOT NULL,
        LicenseType         NVARCHAR(MAX)   NULL,
        LicenseExpiryDate   DATE            NULL,
        Status              INT             NOT NULL,
        CreatedAt           DATETIME2       NOT NULL CONSTRAINT DF_Drivers_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt           DATETIME2       NULL,
        CONSTRAINT PK_Drivers PRIMARY KEY (Id)
    );
    CREATE UNIQUE INDEX IX_Drivers_DriverCode ON dbo.Drivers(DriverCode);
    CREATE UNIQUE INDEX IX_Drivers_LicenseNumber ON dbo.Drivers(LicenseNumber);
END
GO

-- ---------------------------------------------------------------------
-- DriverDocuments
-- ---------------------------------------------------------------------
IF OBJECT_ID('dbo.DriverDocuments', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.DriverDocuments
    (
        Id              INT IDENTITY(1,1) NOT NULL,
        DriverId        INT             NOT NULL,
        Category        INT             NOT NULL,
        FileName        NVARCHAR(MAX)   NOT NULL,
        ContentType     NVARCHAR(MAX)   NOT NULL,
        StoragePath     NVARCHAR(MAX)   NOT NULL,
        FileSizeBytes   BIGINT          NOT NULL,
        UploadedAt      DATETIME2       NOT NULL CONSTRAINT DF_DriverDocuments_UploadedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_DriverDocuments PRIMARY KEY (Id),
        CONSTRAINT FK_DriverDocuments_Drivers_DriverId FOREIGN KEY (DriverId)
            REFERENCES dbo.Drivers(Id) ON DELETE CASCADE
    );
    CREATE INDEX IX_DriverDocuments_DriverId ON dbo.DriverDocuments(DriverId);
END
GO

-- ---------------------------------------------------------------------
-- DriverVehicleAssignments
-- ---------------------------------------------------------------------
IF OBJECT_ID('dbo.DriverVehicleAssignments', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.DriverVehicleAssignments
    (
        Id              INT IDENTITY(1,1) NOT NULL,
        DriverId        INT             NOT NULL,
        VehicleId       INT             NOT NULL,
        StartDate       DATE            NOT NULL,
        EndDate         DATE            NULL,
        Status          INT             NOT NULL,
        Notes           NVARCHAR(MAX)   NULL,
        CONSTRAINT PK_DriverVehicleAssignments PRIMARY KEY (Id),
        CONSTRAINT FK_DriverVehicleAssignments_Drivers_DriverId FOREIGN KEY (DriverId)
            REFERENCES dbo.Drivers(Id) ON DELETE CASCADE,
        CONSTRAINT FK_DriverVehicleAssignments_Vehicles_VehicleId FOREIGN KEY (VehicleId)
            REFERENCES dbo.Vehicles(Id) ON DELETE CASCADE
    );
    CREATE INDEX IX_DriverVehicleAssignments_DriverId ON dbo.DriverVehicleAssignments(DriverId);
    CREATE INDEX IX_DriverVehicleAssignments_VehicleId ON dbo.DriverVehicleAssignments(VehicleId);
END
GO
