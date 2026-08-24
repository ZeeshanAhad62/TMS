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

public enum MaintenanceType
{
    OilChange,
    GeneralService,
    MajorRepair,
    BrakeService,
    BatteryReplacement
}

public enum BookingStatus
{
    Scheduled,
    Active,
    Completed,
    Cancelled
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
