using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Jx3.Admin.Controllers;
using Jx3.Common.Config;
using Jx3.Common.Utils;

GameConfigLoader.Load();
var jwtKey = Environment.GetEnvironmentVariable("JX3_JWT_SECRET")
    ?? "Jx3AdminSecretKey2026TestKey";
var corsOrigins = Environment.GetEnvironmentVariable("JX3_CORS_ORIGINS")
    ?? "http://localhost:5000,http://localhost:9100";
var b = WebApplication.CreateBuilder(args);
b.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o => { o.TokenValidationParameters = new TokenValidationParameters {
        ValidateIssuer = false, ValidateAudience = false, ValidateLifetime = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };});
b.Services.AddAuthorization(); b.Services.AddControllers();
b.Services.AddCors(o => o.AddDefaultPolicy(p => p
    .WithOrigins(corsOrigins.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
    .AllowAnyMethod().AllowAnyHeader()));
b.Services.AddSingleton<AdminService>();
var app = b.Build();
app.UseCors(); app.UseAuthentication(); app.UseAuthorization();
app.UseDefaultFiles(); app.UseStaticFiles();
app.MapControllers();
app.MapFallbackToFile("index.html");
Logger.Info("Admin", "Starting on port " + GameConfig.AdminPort);
app.Run("http://0.0.0.0:" + GameConfig.AdminPort);
