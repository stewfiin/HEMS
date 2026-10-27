namespace HEMS.Application.DTOs;

public record TopBreakdownEquipmentDto(
    string EquipmentCode,
    string Model,
    int BreakdownCount
);

public record DashboardSummaryDto(
    int TotalEquipment,
    int OperatingCount,
    int BreakdownCount,
    int MaintenanceCount,
    decimal PhysicalAvailability, // Persentase PA %
    int ActiveWorkOrders,
    List<TopBreakdownEquipmentDto> TopBreakdownUnits,
    bool FromCache = false
);
