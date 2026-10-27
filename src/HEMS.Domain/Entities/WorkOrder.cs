using HEMS.Domain.Enums;

namespace HEMS.Domain.Entities;

public class WorkOrder
{
    public int Id { get; set; }
    public string WorkOrderNumber { get; set; } = string.Empty;
    public int EquipmentId { get; set; }
    public MaintenanceType MaintenanceType { get; set; }
    public string ProblemDescription { get; set; } = string.Empty;
    public WorkOrderStatus Status { get; set; } = WorkOrderStatus.Draft;
    public string? AssignedMechanic { get; set; }
    public DateTime? BreakdownStartTime { get; set; }
    public DateTime? CompletedTime { get; set; }

    // Relasi navigasi
    public Equipment? Equipment { get; set; }
    public ICollection<WorkOrderPart> WorkOrderParts { get; set; } = new List<WorkOrderPart>();
}