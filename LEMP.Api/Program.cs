using LEMP.Infrastructure.Extensions;
using LEMP.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// JWT konfiguráció
var jwtKey = builder.Configuration["Jwt:Key"] ?? "NagyonTitkosKulcsValtoztasdMeg123";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "LEMP.API";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "KEP.Client";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization(); // RBAC támogatás

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "LEMP API", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "JWT token megadása (Bearer <token>)",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddInfluxDbClient(builder.Configuration);
builder.Services.AddInfluxRawHttpClient(builder.Configuration);

// Service hosting raw HTTP tests if needed.
builder.Services.AddTransient<InfluxRawTestService>();
// Background service pushing smart meter measurements to InfluxDB.
builder.Services.AddHostedService<SmartMeterInfluxForwarder>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication(); // Fontos: Authentication mindig Authorization elõtt
app.UseAuthorization();

app.MapControllers();

app.Run();
