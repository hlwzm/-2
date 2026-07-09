using Jx3.AdminWeb;
using Jx3.AdminWeb.Endpoints;

var builder = WebApplication.CreateBuilder(args);
var corsOrigins = Environment.GetEnvironmentVariable("JX3_CORS_ORIGINS")
    ?? "http://localhost:5000,http://localhost:9100";
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
    .WithOrigins(corsOrigins.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
    .AllowAnyMethod().AllowAnyHeader()));
var app = builder.Build();
app.UseCors();
app.UseDefaultFiles();
app.UseStaticFiles();

var ctx = new AdminWebContext();
ctx.InitReports();
ctx.SeedLogData();

DashboardEndpoints.Map(app, ctx);
UserEndpoints.Map(app, ctx);
LogEndpoints.Map(app, ctx);
TradeEndpoints.Map(app, ctx);
MailEndpoints.Map(app, ctx);
ReportEndpoints.Map(app, ctx);
AnnouncementEndpoints.Map(app, ctx);
ServerEndpoints.Map(app, ctx);

app.MapFallbackToFile("index.html");

Console.WriteLine("╔══════════════════════════════════════════════════════╗");
Console.WriteLine("║  指尖江湖2 AdminWeb Server                         ║");
Console.WriteLine("║  http://localhost:5000                              ║");
Console.WriteLine("╚══════════════════════════════════════════════════════╝");
app.Run("http://0.0.0.0:5000");
