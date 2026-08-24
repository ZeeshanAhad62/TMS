namespace TransportationSystemApi.Models;

public class DriverVehicleAssignment
{
    public int Id { get; set; }
    public int DriverId { get; set; }
    public Driver? Driver { get; set; }

    public int VehicleId { get; set; }
    public Vehicle? Vehicle { get; set; }

    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public AssignmentStatus Status { get; set; } = AssignmentStatus.Active;
    public string? Notes { get; set; }
}
