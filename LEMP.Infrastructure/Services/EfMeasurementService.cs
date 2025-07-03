using LEMP.Application.DTOs;
using LEMP.Application.Interfaces;
using LEMP.Domain;
using LEMP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LEMP.Infrastructure.Services;

public class EfMeasurementService : IMeasurementService
{
    private readonly MeasurementDbContext _db;

    public EfMeasurementService(MeasurementDbContext db)
    {
        _db = db;
    }

    public async Task AddMeasurementAsync(MeasurementDto dto)
    {
        var entity = new Measurement
        {
            SourceType = dto.SourceType,
            SourceId = dto.SourceId,
            Timestamp = dto.Timestamp,
            Values = new Dictionary<string, double>(dto.Values)
        };

        _db.Measurements.Add(entity);
        await _db.SaveChangesAsync();
    }

    public async Task<IEnumerable<MeasurementDto>> GetAllAsync()
    {
        return await _db.Measurements
            .AsNoTracking()
            .Select(m => new MeasurementDto
            {
                SourceType = m.SourceType,
                SourceId = m.SourceId,
                Timestamp = m.Timestamp,
                Values = new Dictionary<string, double>(m.Values)
            })
            .ToListAsync();
    }
}
