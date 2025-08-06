using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace LEMP.Api.Middleware
{
    /// <summary>
    /// Middleware responsible for auditing HTTP requests and writing audit logs to InfluxDB using line protocol.
    /// </summary>
    public class RequestAuditMiddleware
    {
        private const string InfluxWriteUrl = "http://localhost:8181/api/v3/write_lp?db=audit&precision=nanosecond";

        private readonly RequestDelegate _next;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<RequestAuditMiddleware> _logger;

        public RequestAuditMiddleware(RequestDelegate next, IHttpClientFactory httpClientFactory, ILogger<RequestAuditMiddleware> logger)
        {
            _next = next;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                await _next(context);
            }
            finally
            {
                sw.Stop();
                await WriteAuditLogAsync(context, sw.Elapsed.TotalMilliseconds);
            }
        }

        private async Task WriteAuditLogAsync(HttpContext context, double durationMs)
        {
            var user = context.User?.Identity?.IsAuthenticated == true
                ? context.User.Identity!.Name ?? "anonymous"
                : "anonymous";

            var role = context.User?.Identity?.IsAuthenticated == true
                ? context.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value ?? "none"
                : "none";

            var method = context.Request.Method;
            var path = context.Request.Path.HasValue ? context.Request.Path.Value! : string.Empty;
            var status = context.Response.StatusCode;
            var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var id = Guid.NewGuid().ToString("N");

            var timestamp = (DateTime.UtcNow - DateTime.UnixEpoch).Ticks * 100; // nanoseconds

            var line = BuildLineProtocol(id, user, role, method, path, status, durationMs, ip, timestamp);

            var client = _httpClientFactory.CreateClient();
            using var content = new StringContent(line, Encoding.UTF8);
            try
            {
                using var response = await client.PostAsync(InfluxWriteUrl, content);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send audit log to InfluxDB.");
            }
        }

        private static string BuildLineProtocol(string id, string user, string role, string method, string path, int status, double durationMs, string ip, long timestamp)
        {
            var sb = new StringBuilder();
            sb.Append("auditlog");
            sb.Append(",user=").Append(EscapeTag(user));
            sb.Append(",role=").Append(EscapeTag(role));
            sb.Append(",method=").Append(EscapeTag(method));
            sb.Append(",path=").Append(EscapeTag(path));
            sb.Append(",ip=").Append(EscapeTag(ip));
            sb.Append(' ');
            sb.Append("id=\"").Append(EscapeField(id)).Append('\"');
            sb.Append(",status=").Append(status).Append('i');
            sb.Append(",durationMs=").Append(durationMs.ToString(CultureInfo.InvariantCulture));
            sb.Append(' ');
            sb.Append(timestamp.ToString(CultureInfo.InvariantCulture));
            return sb.ToString();
        }

        private static string EscapeTag(string value) => value
            .Replace("\\", "\\\\")
            .Replace(",", "\\,")
            .Replace(" ", "\\ ")
            .Replace("=", "\\=");

        private static string EscapeField(string value) => value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"");
    }
}

