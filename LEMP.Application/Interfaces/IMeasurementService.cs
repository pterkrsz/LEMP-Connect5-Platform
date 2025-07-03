using LEMP.Application.DTOs;

namespace LEMP.Application.Interfaces;

public interface IMeasurementService
{
    Task AddMeasurementAsync(MeasurementDto dto);
    Task<IEnumerable<MeasurementDto>> GetAllAsync();
}
