
using InfluxDB3.Client;
using InfluxDB3.Client.Write;
using LEMP.Application.Interfaces;
using LEMP.Domain.DataPoints;
using Microsoft.Extensions.Logging;

namespace LEMP.Infrastructure.Services;

public class InfluxDataPointService : IDataPointService
{

    private readonly IInfluxDBClient _client;
    private readonly ILogger<InfluxDataPointService>? _logger;

    public InfluxDataPointService(IInfluxDBClient client, ILogger<InfluxDataPointService>? logger = null)
    {
        _client = client;
        _logger = logger;
    }

    public async Task WriteAsync<T>(T point)
    {

        if (point is null) throw new ArgumentNullException(nameof(point));

        PointData data = point switch
        {
            InverterDataPoint inv => BuildInverter(inv),
            BmsDataPoint bms => BuildBms(bms),
            SmartMeterDataPoint sm => BuildSmartMeter(sm),
            MetaDataPoint meta => BuildMeta(meta),
            _ => throw new ArgumentException($"Unsupported data point type: {typeof(T).Name}")
        };

        try
        {
            await _client.WritePointAsync(data);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to write data point to InfluxDB");
            throw;
        }
    }

    private static PointData BuildInverter(InverterDataPoint p) =>
        PointData.Measurement("inverter_data")
            .SetTag("BuildingId", p.BuildingId)
            .SetTag("InverterId", p.InverterId)
            .SetField("power_active", p.PowerActive)
            .SetField("power_reactive", p.PowerReactive)
            .SetField("Frequency", p.Frequency)
            .SetField("voltage_l1", p.VoltageL1)
            .SetField("voltage_l2", p.VoltageL2)
            .SetField("voltage_l3", p.VoltageL3)
            .SetField("current_l1", p.CurrentL1)
            .SetField("current_l2", p.CurrentL2)
            .SetField("current_l3", p.CurrentL3)
            .SetTimestamp(p.Timestamp);

    private static PointData BuildBms(BmsDataPoint p) =>
        PointData.Measurement("bms_data")
            .SetTag("BuildingId", p.BuildingId)
            .SetTag("BatteryId", p.BatteryId)
            .SetField("charge_current", p.ChargeCurrent)
            .SetField("discharge_current", p.DischargeCurrent)
            .SetField("temperature_avg", p.Temperature)
            .SetField("active_protection", p.ActiveProtection)
            .SetField("error_code", p.ErrorCode)
            .SetField("soc_limit", p.SocLimit)
            .SetField("charge_discharge_ok", p.ChargeDischargeOk)
            .SetField("relay_status", p.RelayStatus)
            .SetField("cell_balancing", p.CellBalancingStatus)
            .SetTimestamp(p.Timestamp);

    private static PointData BuildSmartMeter(SmartMeterDataPoint p) =>
        PointData.Measurement("smartmeter_data")
            .SetTag("BuildingId", p.BuildingId)
            .SetTag("MeterId", p.MeterId)
            .SetField("total_import_energy", p.TotalImportEnergy)
            .SetField("total_export_energy", p.TotalExportEnergy)
            .SetField("current_power", p.CurrentPower)
            .SetField("reactive_power", p.ReactivePower)
            .SetField("power_factor", p.PowerFactor)
            .SetField("voltage_l1", p.VoltageL1)
            .SetField("voltage_l2", p.VoltageL2)
            .SetField("voltage_l3", p.VoltageL3)
            .SetField("current_l1", p.CurrentL1)
            .SetField("current_l2", p.CurrentL2)
            .SetField("current_l3", p.CurrentL3)
            .SetField("phase_sequence", p.PhaseSequence)
            .SetField("power_direction", p.PowerDirection)
            .SetTimestamp(p.Timestamp);

    private static PointData BuildMeta(MetaDataPoint p) =>
        PointData.Measurement("meta_data")
            .SetTag("BuildingId", p.BuildingId)
            .SetTag("DeviceId", p.DeviceId)
            .SetField("firmware_version", p.FirmwareVersion)
            .SetField("comm_status", p.CommunicationStatus)
            .SetField("last_update_time", p.LastUpdateTime)
            .SetTimestamp(p.Timestamp);

}
