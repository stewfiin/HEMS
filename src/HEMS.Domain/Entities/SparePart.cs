namespace HEMS.Domain.Entities;

public class SparePart
{
    public int Id { get; set; }
    public string PartNumber { get; set; } = string.Empty;
    public string PartName { get; set; } = string.Empty;
    public int StockQuantity { get; set; }
    public decimal UnitPrice { get; set; }

    // Relasi navigasi
    public ICollection<WorkOrderPart> WorkOrderParts { get; set; } = new List<WorkOrderPart>();
}