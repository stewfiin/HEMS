namespace HEMS.Application.DTOs;

public record SparePartDto(
    int Id,
    string PartNumber,
    string PartName,
    int StockQuantity,
    decimal UnitPrice
);

public record CreateSparePartRequest(
    string PartNumber,
    string PartName,
    int StockQuantity,
    decimal UnitPrice
);

public record UpdateSparePartStockRequest(
    int AdditionalQuantity
);

