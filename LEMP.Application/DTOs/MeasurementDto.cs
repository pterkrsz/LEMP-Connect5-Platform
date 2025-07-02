using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LEMP.Application.DTOs;

public class MeasurementDto
{
    public string SourceType { get; set; } = string.Empty; 
    public string SourceId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public Dictionary<string, double> Values { get; set; } = new();
}
