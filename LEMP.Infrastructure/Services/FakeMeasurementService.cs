using LEMP.Application.DTOs;
using LEMP.Application.Interfaces;

namespace LEMP.Infrastructure.Services;

public class FakeMeasurementService : IMeasurementService
{
    private readonly List<MeasurementDto> _measurements = new();

    public Task AddMeasurementAsync(MeasurementDto dto)
    {
        _measurements.Add(dto);
        return Task.CompletedTask;
    }

    public Task<IEnumerable<MeasurementDto>> GetAllAsync()
    {
        return Task.FromResult(_measurements.AsEnumerable());
    }
}
