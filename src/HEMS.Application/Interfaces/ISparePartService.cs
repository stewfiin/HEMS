using HEMS.Application.DTOs;

namespace HEMS.Application.Interfaces;

public interface ISparePartService
{
    Task<IEnumerable<SparePartDto>> GetAllAsync();
    Task<IEnumerable<SparePartDto>> SearchAsync(string query);
    Task<SparePartDto?> GetByIdAsync(int id);
    Task<SparePartDto> CreateAsync(CreateSparePartRequest request);
}
