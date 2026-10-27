using HEMS.Domain.Enums;

namespace HEMS.Domain.Entities;

public class Equipment
{
    public int Id { get; set; }
    public string EquipmentCode { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public decimal CurrentHourMeter { get; set; }
    public decimal LastServiceHourMeter { get; set; }
    public EquipmentStatus Status { get; set; } = EquipmentStatus.Operating;
    public string SiteLocation { get; set; } = string.Empty;

    // Relasi navigasi
    public ICollection<HourMeterLog> HourMeterLogs { get; set; } = new List<HourMeterLog>();
    public ICollection<WorkOrder> WorkOrders { get; set; } = new List<WorkOrder>();
}