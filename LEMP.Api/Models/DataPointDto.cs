using System;
using System.Collections.Generic;

namespace LEMP.Api.Models
{

    // Generic InfluxDB data point payload
    public class DataPointDto
    {
        // Measurement name
        public string Measurement { get; set; } = string.Empty;

        // Tags associated with the point
        public IDictionary<string, string>? Tags { get; set; }

        // Fields of the point
        public IDictionary<string, object>? Fields { get; set; }

        // Optional timestamp
        public DateTime? Timestamp { get; set; }
    }
}
