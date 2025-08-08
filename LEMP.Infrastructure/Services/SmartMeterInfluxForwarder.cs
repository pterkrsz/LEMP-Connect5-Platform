using System;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LEMP.Application.SmartMeter;
using LEMP.Domain.SmartMeter;
using LEMP.Application.Modbus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LEMP.Infrastructure.Services;

public class SmartMeterInfluxForwarder : BackgroundService
{
    private readonly IHttpClientFactory _factory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SmartMeterInfluxForwarder> _logger;
    private readonly string _serialPort;

    public SmartMeterInfluxForwarder(
        IHttpClientFactory factory,
        IConfiguration configuration,
        ILogger<SmartMeterInfluxForwarder> logger)
    {
        _factory = factory;
        _configuration = configuration;
        _logger = logger;
        _serialPort = _configuration["SmartMeter:SerialPort"] ?? "COM9";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        ModbusRTUReader reader = new(_serialPort);
        var adapter = new SmartMeterAdapter(reader);

        var client = _factory.CreateClient("Influx");
        var token = _configuration["InfluxDB:Token"];
        var db = _configuration["InfluxDB:Bucket"] ?? "default";
        var node = _configuration["InfluxDB:NodeId"] ?? "node0";
        var url = $"/api/v3/write_lp?db={Uri.EscapeDataString(db)}&precision=nanosecond&accept_partial=true";


        client.DefaultRequestHeaders.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        if (!string.IsNullOrEmpty(token))
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", token);
        }

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var state = adapter.ReadSmartMeterState();
                    if (state.SmartMeterAlive)
                    {
                        var line = BuildLineProtocol(node, state);


                        var content = new StringContent(line, Encoding.UTF8, "text/plain");
                        var res = await client.PostAsync(url, content, stoppingToken);



                        if (!res.IsSuccessStatusCode)
                        {
                            var body = await res.Content.ReadAsStringAsync(stoppingToken);

                        }
                    }
                    else
                    {
                        _logger.LogWarning("Smart meter read failed; attempting to reconnect");
                        try
                        {
                            reader.Dispose();
                            reader = new ModbusRTUReader(_serialPort);
                            adapter = new SmartMeterAdapter(reader);
                            _logger.LogInformation("Smart meter connection re-established");
                        }
                        catch (Exception reconnectionException)
                        {
                            _logger.LogError(reconnectionException, "Failed to re-establish smart meter connection");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error forwarding smart meter data");
                }

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
                catch (TaskCanceledException) { }
            }
        }
        finally
        {
            reader.Dispose();
        }
    }

    private static string BuildLineProtocol(string node, SmartMeterState s)
    {
        var inv = CultureInfo.InvariantCulture;
        var sb = new StringBuilder();

        sb.Append("smartmeter,node=").Append(node).Append(' ');
        sb.Append("voltage=").Append(s.VoltageLineToNeutral.ToString(inv)).Append(',');
        sb.Append("current=").Append(s.Current.ToString(inv)).Append(',');
        sb.Append("activePower=").Append(s.ActivePower.ToString(inv)).Append(',');
        sb.Append("apparentPower=").Append(s.ApparentPower.ToString(inv)).Append(',');
        sb.Append("reactivePower=").Append(s.ReactivePower.ToString(inv)).Append(',');
        sb.Append("powerFactor=").Append(s.PowerFactor.ToString(inv)).Append(',');
        sb.Append("frequency=").Append(s.Frequency.ToString(inv)).Append(',');
        sb.Append("importedActiveEnergy=").Append(s.ImportedActiveEnergy.ToString(inv)).Append(',');
        sb.Append("exportedActiveEnergy=").Append(s.ExportedActiveEnergy.ToString(inv)).Append(',');
        sb.Append("importedReactiveEnergy=").Append(s.ImportedReactiveEnergy.ToString(inv)).Append(',');
        sb.Append("exportedReactiveEnergy=").Append(s.ExportedReactiveEnergy.ToString(inv)).Append(',');
        sb.Append("totalActiveEnergy=").Append(s.TotalActiveEnergy.ToString(inv)).Append(' ');
        sb.Append(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1_000_000);


        return sb.ToString();
    }
}
