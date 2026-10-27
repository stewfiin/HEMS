using HEMS.Application.DTOs;
using HEMS.Application.Interfaces;
using HEMS.Domain.Entities;
using HEMS.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace HEMS.Application.Services;

public class WorkOrderService : IWorkOrderService
{
    private readonly IApplicationDbContext _context;

    public WorkOrderService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<WorkOrderDto>> GetAllAsync(string? status = null)
    {
        var query = _context.WorkOrders
            .Include(w => w.Equipment)
            .Include(w => w.WorkOrderParts)
                .ThenInclude(wp => wp.SparePart)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<WorkOrderStatus>(status, true, out var parsedStatus))
            query = query.Where(w => w.Status == parsedStatus);

        var list = await query.ToListAsync();

        return list.Select(MapToDto);
    }

    public async Task<WorkOrderDto?> GetByIdAsync(int id)
    {
        var wo = await _context.WorkOrders
            .Include(w => w.Equipment)
            .Include(w => w.WorkOrderParts)
                .ThenInclude(wp => wp.SparePart)
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == id);

        return wo == null ? null : MapToDto(wo);
    }

    public async Task<WorkOrderDto> CreateAsync(CreateWorkOrderRequest request)
    {
        var equipment = await _context.Equipments.FirstOrDefaultAsync(e => e.Id == request.EquipmentId);
        if (equipment == null)
            throw new KeyNotFoundException("Equipment tidak ditemukan.");

        var maintenanceType = Enum.Parse<MaintenanceType>(request.MaintenanceType, true);

        // Generate Nomor WO otomatis (misal: WO-202610-XXXX)
        string woNumber = $"WO-{DateTime.UtcNow:yyyyMM}-{Guid.NewGuid().ToString()[..4].ToUpper()}";

        var workOrder = new WorkOrder
        {
            WorkOrderNumber = woNumber,
            EquipmentId = equipment.Id,
            MaintenanceType = maintenanceType,
            ProblemDescription = request.ProblemDescription,
            Status = WorkOrderStatus.Assigned,
            AssignedMechanic = request.AssignedMechanic,
            BreakdownStartTime = maintenanceType == MaintenanceType.Corrective ? DateTime.UtcNow : null
        };

        // FR-2.3: Jika jenis Corrective (Breakdown), ubah status unit otomatis jadi Breakdown
        if (maintenanceType == MaintenanceType.Corrective)
        {
            equipment.Status = EquipmentStatus.Breakdown;
        }

        _context.WorkOrders.Add(workOrder);
        await _context.SaveChangesAsync();

        return (await GetByIdAsync(workOrder.Id))!;
    }

    public async Task<CompleteWorkOrderResponse> CompleteWorkOrderAsync(int id, CompleteWorkOrderRequest request)
    {
        // FR-3.3 & SIT-01: Eksekusi Transaksi Database Atomik
        await using var transaction = await _context.BeginTransactionAsync();

        try
        {
            var workOrder = await _context.WorkOrders
                .Include(w => w.Equipment)
                .Include(w => w.WorkOrderParts)
                .FirstOrDefaultAsync(w => w.Id == id);

            if (workOrder == null)
                return new CompleteWorkOrderResponse(false, "Work Order tidak ditemukan.");

            if (workOrder.Status == WorkOrderStatus.Completed || workOrder.Status == WorkOrderStatus.Closed)
                return new CompleteWorkOrderResponse(false, "Work Order sudah dalam status selesai/tertutup.");

            // Potong stok suku cadang
            foreach (var partItem in request.PartsUsed)
            {
                var sparePart = await _context.SpareParts.FirstOrDefaultAsync(s => s.Id == partItem.SparePartId);
                if (sparePart == null)
                {
                    await transaction.RollbackAsync();
                    return new CompleteWorkOrderResponse(false, $"Suku cadang dengan ID {partItem.SparePartId} tidak ditemukan.");
                }

                // Validasi kecukupan stok gudang
                if (sparePart.StockQuantity < partItem.Quantity)
                {
                    await transaction.RollbackAsync();
                    return new CompleteWorkOrderResponse(false, 
                        $"Gagal: Stok untuk '{sparePart.PartName}' tidak mencukupi! (Diminta: {partItem.Quantity}, Tersedia: {sparePart.StockQuantity}). Transaksi dibatalkan.");
                }

                // Kurangi stok
                sparePart.StockQuantity -= partItem.Quantity;

                // Catat ke WorkOrderParts
                workOrder.WorkOrderParts.Add(new WorkOrderPart
                {
                    WorkOrderId = workOrder.Id,
                    SparePartId = sparePart.Id,
                    QuantityUsed = partItem.Quantity,
                    UnitPriceAtTime = sparePart.UnitPrice
                });
            }

            // Update status WO menjadi Completed
            workOrder.Status = WorkOrderStatus.Completed;
            workOrder.CompletedTime = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return new CompleteWorkOrderResponse(true, "Work Order berhasil diselesaikan dan stok suku cadang telah terpotong.");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return new CompleteWorkOrderResponse(false, $"Terjadi kesalahan transaksi: {ex.Message}");
        }
    }

    public async Task<bool> CloseWorkOrderAsync(int id)
    {
        var workOrder = await _context.WorkOrders
            .Include(w => w.Equipment)
            .FirstOrDefaultAsync(w => w.Id == id);

        if (workOrder == null) return false;

        workOrder.Status = WorkOrderStatus.Closed;

        // FR-2.3: Saat WO ditutup, kembalikan status unit ke Operating
        if (workOrder.Equipment != null)
        {
            workOrder.Equipment.Status = EquipmentStatus.Operating;
            // Jika ini servis berkala, perbarui HM servis terakhir ke HM saat ini
            if (workOrder.MaintenanceType == MaintenanceType.Preventive)
            {
                workOrder.Equipment.LastServiceHourMeter = workOrder.Equipment.CurrentHourMeter;
            }
        }

        await _context.SaveChangesAsync();
        return true;
    }

    private static WorkOrderDto MapToDto(WorkOrder w) => new(
        w.Id,
        w.WorkOrderNumber,
        w.EquipmentId,
        w.Equipment?.EquipmentCode ?? string.Empty,
        w.MaintenanceType.ToString(),
        w.ProblemDescription,
        w.Status.ToString(),
        w.AssignedMechanic,
        w.BreakdownStartTime,
        w.CompletedTime,
        w.WorkOrderParts.Select(wp => new WorkOrderPartItemDto(
            wp.SparePartId,
            wp.SparePart?.PartNumber ?? string.Empty,
            wp.SparePart?.PartName ?? string.Empty,
            wp.QuantityUsed,
            wp.UnitPriceAtTime
        )).ToList()
    );
}
