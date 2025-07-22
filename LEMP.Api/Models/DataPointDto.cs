using System;
using System.Collections.Generic;

namespace LEMP.Api.Models
{
    /// <summary>
    /// Represents a generic InfluxDB data point.
    /// </summary>
    public class DataPointDto
    {
        /// <summary>
        /// Measurement name for the point.
        /// </summary>
        public string Measurement { get; set; } = string.Empty;

        /// <summary>
        /// Tags associated with the point.
        /// </summary>
        public IDictionary<string, string>? Tags { get; set; }

        /// <summary>
        /// Fields of the point.
        /// </summary>
        public IDictionary<string, object>? Fields { get; set; }

        /// <summary>
        /// Optional timestamp for the point.
        /// </summary>
        public DateTime? Timestamp { get; set; }
    }
}
