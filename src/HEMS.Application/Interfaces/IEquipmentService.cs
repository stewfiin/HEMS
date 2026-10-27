using HEMS.Application.DTOs;

namespace HEMS.Application.Interfaces;

public interface IEquipmentService
{
    Task<IEnumerable<EquipmentDto>> GetAllAsync(string? siteLocation = null, string? status = null);
    Task<EquipmentDto?> GetByIdAsync(int id);
    Task<EquipmentDto> CreateAsync(CreateEquipmentRequest request);
    Task<LogHourMeterResponse> RecordHourMeterAsync(int equipmentId, LogHourMeterRequest request);
}