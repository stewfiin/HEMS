using HEMS.Application.DTOs;
using HEMS.Application.Interfaces;
using HEMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HEMS.Application.Services;

public class SparePartService : ISparePartService
{
    private readonly IApplicationDbContext _context;

    public SparePartService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<SparePartDto>> GetAllAsync()
    {
        return await _context.SpareParts
            .AsNoTracking()
            .Select(s => new SparePartDto(s.Id, s.PartNumber, s.PartName, s.StockQuantity, s.UnitPrice))
            .ToListAsync();
    }

    public async Task<IEnumerable<SparePartDto>> SearchAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return await GetAllAsync();

        string lower = query.Trim().ToLower();

        return await _context.SpareParts
            .AsNoTracking()
            .Where(s => s.PartNumber.ToLower().Contains(lower) || s.PartName.ToLower().Contains(lower))
            .Select(s => new SparePartDto(s.Id, s.PartNumber, s.PartName, s.StockQuantity, s.UnitPrice))
            .ToListAsync();
    }

    public async Task<SparePartDto?> GetByIdAsync(int id)
    {
        var s = await _context.SpareParts.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        return s == null ? null : new SparePartDto(s.Id, s.PartNumber, s.PartName, s.StockQuantity, s.UnitPrice);
    }

    public async Task<SparePartDto> CreateAsync(CreateSparePartRequest request)
    {
        var part = new SparePart
        {
            PartNumber = request.PartNumber,
            PartName = request.PartName,
            StockQuantity = request.StockQuantity,
            UnitPrice = request.UnitPrice
        };

        _context.SpareParts.Add(part);
        await _context.SaveChangesAsync();

        return new SparePartDto(part.Id, part.PartNumber, part.PartName, part.StockQuantity, part.UnitPrice);
    }
}

