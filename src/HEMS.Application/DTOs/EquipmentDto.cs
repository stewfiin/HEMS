namespace HEMS.Application.DTOs;

public record EquipmentDto(
    int Id,
    string EquipmentCode,
    string SerialNumber,
    string Brand,
    string Model,
    decimal CurrentHourMeter,
    decimal LastServiceHourMeter,
    string Status,
    string SiteLocation,
    bool NeedsServiceAlert // Alert kelipatan 250 HM
);

public record CreateEquipmentRequest(
    string EquipmentCode,
    string SerialNumber,
    string Brand,
    string Model,
    decimal InitialHourMeter,
    string SiteLocation
);

public record LogHourMeterRequest(
    decimal NewHourMeter,
    string RecordedBy
);

public record LogHourMeterResponse(
    bool Success,
    string Message,
    decimal PreviousHM,
    decimal CurrentHM,
    bool PreventiveMaintenanceDue
);