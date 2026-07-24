using System.Text;
using BarRecoveryApp.Api.Data;
using BarRecoveryApp.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();

var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Falta configurar Jwt:Key en appsettings.");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "BarRecoveryApp.Api";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "BarRecoveryApp.Tablets";

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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

builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    // Aplica migraciones automáticamente solo en desarrollo. En producción,
    // las migraciones se aplican explícitamente (dotnet ef database update),
    // nunca automático contra la base real.
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

// Solo se fuerza HTTPS fuera de Development. En el piloto local (emulador
// Android + certificado autofirmado), la app le habla al puerto HTTP a
// propósito — ver la nota en network_security_config.xml del lado MAUI.
// Antes de un despliegue real, esto debe volver a exigir HTTPS siempre.
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();