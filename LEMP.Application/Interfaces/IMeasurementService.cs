using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LEMP.Application.DTOs;

namespace LEMP.Application.Interfaces;

public interface IMeasurementService
{
    Task AddMeasurementAsync(MeasurementDto dto);
    Task<IEnumerable<MeasurementDto>> GetAllAsync();
}
