namespace HEMS.Application.DTOs;

public record WorkOrderPartItemDto(
    int SparePartId,
    string PartNumber,
    string PartName,
    int Quantity,
    decimal UnitPrice
);

public record WorkOrderDto(
    int Id,
    string WorkOrderNumber,
    int EquipmentId,
    string EquipmentCode,
    string MaintenanceType,
    string ProblemDescription,
    string Status,
    string? AssignedMechanic,
    DateTime? BreakdownStartTime,
    DateTime? CompletedTime,
    List<WorkOrderPartItemDto> PartsUsed
);

public record CreateWorkOrderRequest(
    int EquipmentId,
    string MaintenanceType, // "Preventive" atau "Corrective"
    string ProblemDescription,
    string? AssignedMechanic
);

public record AddPartToWorkOrderRequest(
    int SparePartId,
    int Quantity
);

public record CompleteWorkOrderRequest(
    List<AddPartToWorkOrderRequest> PartsUsed
);

public record CompleteWorkOrderResponse(
    bool Success,
    string Message
);
