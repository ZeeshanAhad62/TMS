namespace TransportationSystemApi.Models;

public enum VehicleType
{
    Truck,
    Trailer,
    ContainerCarrier,
    Van,
    Bus,
    Other
}

public enum OwnershipType
{
    Owned,
    Leased,
    Rented,
    Contracted
}

public enum FuelType
{
    Diesel,
    Petrol,
    CNG,
    EV,
    Hybrid
}

public enum OperationalStatus
{
    Available,
    OnTrip,
    UnderMaintenance,
    OutOfService
}

public enum DocumentCategory
{
    RegistrationCertificate,
    FitnessCertificate,
    RoutePermit,
    Insurance,
    PollutionCertificate,
    RoadTax,
    Other
}

[Flags]
public enum NotificationChannel
{
    None = 0,
    Email = 1,
    InApp = 2,
    SMS = 4
}

public enum AlertStatus
{
    Pending,
    Acknowledged,
    Resolved
}

public enum TyrePosition
{
    FrontLeft,
    FrontRight,
    RearLeft,
    RearRight,
    RearLeftInner,
    RearLeftOuter,
    RearRightInner,
    RearRightOuter,
    Spare,
    Other
}

public enum TyreStatus
{
    InStock,
    Fitted,
    Scrapped
}

public enum TyreEventType
{
    Fit,
    Remove,
    Rotate,
    Retread,
    Inspect,
    Scrap
}

public enum MaintenanceType
{
    OilChange,
    GeneralService,
    MajorRepair,
    BrakeService,
    BatteryReplacement
}

public enum TripStatus
{
    Scheduled,
    Active,
    Completed,
    Cancelled
}

public enum WorkOrderStatus
{
    Open,
    InProgress,
    Completed,
    Cancelled
}

public enum WorkOrderPriority
{
    Low,
    Medium,
    High
}

public enum UserRole
{
    Admin,
    FleetManager,
    Viewer
}

public enum DriverStatus
{
    Active,
    OnLeave,
    Suspended,
    Inactive
}

public enum DriverDocumentCategory
{
    DrivingLicense,
    MedicalCertificate,
    PoliceVerification,
    IdProof,
    Other
}

public enum AssignmentStatus
{
    Active,
    Completed,
    Cancelled
}

public enum CustomerStatus
{
    Active,
    Inactive
}

public enum FuelPaymentMode
{
    Cash,
    FuelCard,
    Credit
}

public enum TripExpenseCategory
{
    Toll,
    Parking,
    LoadingUnloading,
    DriverAllowance,
    EnRouteRepair,
    Fine,
    Weighbridge,
    Misc
}

public enum ExpensePaidBy
{
    Company,
    Driver
}

public enum InvoiceStatus
{
    Draft,
    Sent,
    PartiallyPaid,
    Paid,
    Cancelled
}

public enum PaymentMode
{
    Cash,
    Bank,
    Cheque,
    Online
}

public enum PartMovementType
{
    Receipt,
    Issue,
    Adjust
}

public enum StockMovementReferenceType
{
    Manual,
    WorkOrder
}

// Per-driver pay basis (Driver.PayType).
public enum DriverPayType
{
    PerTrip,
    PerKm,
    Monthly,
    Percentage
}

// User-set intent on a pay run. Gross / net figures are always derived.
public enum PayRunStatus
{
    Draft,
    Approved,
    Paid,
    Cancelled
}

// How a single pay-run line's amount was arrived at. Manual = entered by hand.
public enum PayRunLineBasis
{
    PerTrip,
    PerKm,
    Monthly,
    Percentage,
    Manual
}
