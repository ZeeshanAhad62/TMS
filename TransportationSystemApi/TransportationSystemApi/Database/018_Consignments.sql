-- =====================================================================
-- Migration 018: Consignment note / LR / Proof-of-Delivery.
--
-- dbo.Consignments -- one lorry-receipt / consignment note carried on a trip.
--                     A trip can carry many (ON DELETE CASCADE from Trips).
--                     POD (proof-of-delivery) file metadata lives on the row;
--                     the file itself is stored on disk under
--                     wwwroot/uploads/consignments/{id}/ like driver/vehicle
--                     documents (see ConsignmentsController).
--
-- Delivery details (DeliveredAt / ReceivedBy / DeliveryNotes) and Status are
-- set together via POST api/consignments/{id}/deliver or a plain PUT.
--
-- Safe to re-run: every statement is guarded with an existence check.
-- =====================================================================

USE FleetMasterDb;
GO

IF OBJECT_ID('dbo.Consignments', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Consignments
    (
        Id                  INT IDENTITY(1,1) NOT NULL,
        ConsignmentCode     NVARCHAR(450)   NOT NULL,
        TripId              INT             NOT NULL,
        LrNumber            NVARCHAR(80)    NULL,
        BookingDate         DATE            NOT NULL,

        ConsignorName       NVARCHAR(200)   NOT NULL,
        ConsignorAddress    NVARCHAR(MAX)   NULL,
        ConsignorPhone      NVARCHAR(40)    NULL,

        ConsigneeName       NVARCHAR(200)   NOT NULL,
        ConsigneeAddress    NVARCHAR(MAX)   NULL,
        ConsigneePhone      NVARCHAR(40)    NULL,

        GoodsDescription    NVARCHAR(MAX)   NULL,
        Packages            INT             NULL,
        WeightKg            DECIMAL(18,2)   NULL,
        DeclaredValue       DECIMAL(18,2)   NULL,
        FreightAmount       DECIMAL(18,2)   NULL,
        FreightTerms        INT             NOT NULL CONSTRAINT DF_Consignments_FreightTerms DEFAULT (0),

        Status              INT             NOT NULL CONSTRAINT DF_Consignments_Status DEFAULT (0),
        DeliveredAt         DATETIME2       NULL,
        ReceivedBy          NVARCHAR(150)   NULL,
        DeliveryNotes       NVARCHAR(MAX)   NULL,

        PodFileName         NVARCHAR(260)   NULL,
        PodContentType      NVARCHAR(120)   NULL,
        PodStoragePath      NVARCHAR(400)   NULL,
        PodFileSizeBytes    BIGINT          NULL,
        PodUploadedAt       DATETIME2       NULL,

        CreatedAt           DATETIME2       NOT NULL CONSTRAINT DF_Consignments_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt           DATETIME2       NULL,
        CONSTRAINT PK_Consignments PRIMARY KEY (Id),
        CONSTRAINT FK_Consignments_Trips_TripId FOREIGN KEY (TripId)
            REFERENCES dbo.Trips(Id) ON DELETE CASCADE
    );
    CREATE UNIQUE INDEX IX_Consignments_ConsignmentCode ON dbo.Consignments(ConsignmentCode);
    CREATE INDEX IX_Consignments_TripId ON dbo.Consignments(TripId);
    CREATE INDEX IX_Consignments_LrNumber ON dbo.Consignments(LrNumber);
    CREATE INDEX IX_Consignments_Status ON dbo.Consignments(Status);
END
GO
