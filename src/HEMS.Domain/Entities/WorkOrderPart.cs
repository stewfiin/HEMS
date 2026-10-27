namespace HEMS.Domain.Entities;

public class WorkOrderPart
{
    public int Id { get; set; }
    public int WorkOrderId { get; set; }
    public int SparePartId { get; set; }
    public int QuantityUsed { get; set; }
    public decimal UnitPriceAtTime { get; set; }

    // Relasi navigasi
    public WorkOrder? WorkOrder { get; set; }
    public SparePart? SparePart { get; set; }
}