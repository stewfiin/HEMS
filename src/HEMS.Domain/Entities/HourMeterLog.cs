namespace HEMS.Domain.Entities;

public class HourMeterLog
{
    public long Id { get; set; }
    public int EquipmentId { get; set; }
    public DateTime RecordedDate { get; set; } = DateTime.UtcNow;
    public decimal PreviousHM { get; set; }
    public decimal CurrentHM { get; set; }
    public string RecordedBy { get; set; } = string.Empty;

    // Relasi navigasi
    public Equipment? Equipment { get; set; }
}