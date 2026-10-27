using HEMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace HEMS.Application.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Equipment> Equipments { get; }
    DbSet<HourMeterLog> HourMeterLogs { get; }
    DbSet<WorkOrder> WorkOrders { get; }
    DbSet<SparePart> SpareParts { get; }
    DbSet<WorkOrderPart> WorkOrderParts { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}
