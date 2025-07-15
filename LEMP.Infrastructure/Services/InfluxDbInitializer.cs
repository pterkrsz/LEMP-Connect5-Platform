
using System.Net.Http;
using System.Text;
using System.Text.Json;


namespace LEMP.Infrastructure.Services;

public class InfluxDbInitializer
{

    private readonly HttpClient _client;


    private const string Database = "local_system";
    private const string Schema = "autogen";
    private const string Table = "ems_metrics";

    private const string CreateTableSql = @"CREATE TABLE local_system.autogen.ems_metrics (
  time TIMESTAMP NOT NULL,
  device_id STRING,
  source STRING,
  location STRING,
  active_power DOUBLE,
  reactive_power DOUBLE,
  voltage_l1_l3 DOUBLE,
  current_l1_l3 DOUBLE,
  frequency DOUBLE,
  power_factor DOUBLE,
  dc_input_voltage DOUBLE,
  setpoint_feedback DOUBLE,
  soc DOUBLE,
  soh DOUBLE,
  charge_discharge_power DOUBLE,
  voltage_total_or_cell DOUBLE,
  current_charge_or_discharge DOUBLE,
  temperature DOUBLE,
  soc_limits DOUBLE,
  imported_energy DOUBLE,
  exported_energy DOUBLE,
  total_power DOUBLE,
  sm_reactive_power DOUBLE,
  sm_power_factor DOUBLE,
  cycle_count INT,
  fault_warning_code INT,
  smart_direction INT,
  protection_active INT,
  bms_fault_code INT,
  operating_mode STRING,
  mppt_status STRING,
  phase_order STRING,
  firmware_version STRING,
  relay_status BOOLEAN,
  export_import_limit BOOLEAN,
  on_off_status BOOLEAN,
  charge_enable BOOLEAN,
  bms_relay_status BOOLEAN,
  cell_balancing BOOLEAN,
  meta_comm_status BOOLEAN,
  meta_last_update_time TIMESTAMP
)";


    public InfluxDbInitializer(HttpClient client)
    {
        _client = client;

    }

    public async Task InitializeAsync()
    {

        if (!await TableExistsAsync())
        {
            await ExecuteAsync(CreateTableSql);

        }
    }

    private async Task<bool> TableExistsAsync()
    {

        const string sql = "SELECT table_name FROM information_schema.tables WHERE table_catalog=$db AND table_schema=$schema AND table_name=$table";
        var payload = new
        {
            query = sql,
            parameters = new Dictionary<string, object>
            {
                ["db"] = Database,
                ["schema"] = Schema,
                ["table"] = Table
            }
        };
        var response = await SendQueryAsync(payload);
        return response.Contains(Table);
    }

    private async Task ExecuteAsync(string sql)
    {
        var payload = new { query = sql };
        await SendQueryAsync(payload);
    }

    private async Task<string> SendQueryAsync(object payload)
    {
        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var response = await _client.PostAsync("/api/v2/query?org=local_org", content);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();

    }
}
