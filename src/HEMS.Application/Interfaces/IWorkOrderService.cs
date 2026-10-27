using HEMS.Application.DTOs;

namespace HEMS.Application.Interfaces;

public interface IWorkOrderService
{
    Task<IEnumerable<WorkOrderDto>> GetAllAsync(string? status = null);
    Task<WorkOrderDto?> GetByIdAsync(int id);
    Task<WorkOrderDto> CreateAsync(CreateWorkOrderRequest request);
    Task<CompleteWorkOrderResponse> CompleteWorkOrderAsync(int id, CompleteWorkOrderRequest request);
    Task<bool> CloseWorkOrderAsync(int id);
}

