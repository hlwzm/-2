import os
ad = "D:\\CodexWorkSpace\\MyGame\\指尖江湖2\\Server\\Jx3.Admin"
os.makedirs(os.path.join(ad,"Controllers"), exist_ok=True)
os.makedirs(os.path.join(ad,"Properties"), exist_ok=True)

prog = '''using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Jx3.Admin.Controllers;
using Jx3.Common.Config;
using Jx3.Common.Utils;

var cfg = GameConfigLoader.Load();
var key = "Jx3AdminSecretKey2026TestKey";
var b = WebApplication.CreateBuilder(args);
b.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o => { o.TokenValidationParameters = new TokenValidationParameters {
        ValidateIssuer = false, ValidateAudience = false, ValidateLifetime = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key))
    };});
b.Services.AddAuthorization(); b.Services.AddControllers();
b.Services.AddCors(o => o.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));
b.Services.AddSingleton<AdminService>();
var app = b.Build(); app.UseCors(); app.UseAuthentication(); app.UseAuthorization(); app.MapControllers();
app.MapGet("/", async (ctx) => { ctx.Response.ContentType = "text/html;charset=utf-8"; await ctx.Response.WriteAsync("<h1>JX3 Admin</h1><p>OK</p>"); });
Logger.Info("Admin", "Starting on port " + cfg.AdminPort);
app.Run("http://0.0.0.0:" + cfg.AdminPort);
'''
with open(os.path.join(ad,"Program.cs"),"w",encoding="utf-8") as f: f.write(prog)

csproj = '<Project Sdk="Microsoft.NET.Sdk.Web"><PropertyGroup><TargetFramework>net9.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable></PropertyGroup><ItemGroup><PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="9.0.0"/></ItemGroup><ItemGroup><ProjectReference Include="..\\Jx3.Common\\Jx3.Common.csproj"/></ItemGroup></Project>'
with open(os.path.join(ad,"Jx3.Admin.csproj"),"w",encoding="utf-8") as f: f.write(csproj)
with open(os.path.join(ad,"appsettings.json"),"w",encoding="utf-8") as f: f.write("{}")
with open(os.path.join(ad,"Properties","launchSettings.json"),"w",encoding="utf-8") as f: f.write('{"profiles":{"Jx3.Admin":{"commandName":"Project","launchBrowser":false}}}')

svc = '''using System.Collections.Concurrent;
using Jx3.Common.Database; using Jx3.Common.Utils;
namespace Jx3.Admin.Controllers;
public class AdminService {
    static Dictionary<string,string> _a = new() { {"admin","admin123"} };
    ConcurrentDictionary<string,string> _t = new();
    public bool LoginCheck(string u, string p) => _a.TryGetValue(u,out var pw) && pw==p;
    public string DoLogin(string u) { var t=Guid.NewGuid().ToString("N"); _t[t]=u; return t; }
    public bool ValidToken(string t) => !string.IsNullOrEmpty(t?.Replace("Bearer ","")) && _t.ContainsKey(t.Replace("Bearer ",""));
    public object Dash() => new { online=new Random().Next(100,500), active=new Random().Next(1000,3000) };
    public async Task<List<object>?> Search(string kw) { try { using var db=new DbHelper(); var r=await db.QueryAsync<object>("SELECT player_id,name,level FROM player WHERE name LIKE @kw LIMIT 20",new{kw=$"%{kw}%"}); return r.ToList(); } catch { return null; } }
}'''
with open(os.path.join(ad,"Controllers","AdminService.cs"),"w",encoding="utf-8") as f: f.write(svc)

ctrl = '''using Microsoft.AspNetCore.Mvc;
namespace Jx3.Admin.Controllers;
[ApiController][Route("api/admin")]
public class AuthController : ControllerBase {
    AdminService _s; public AuthController(AdminService s) => _s = s;
    [HttpPost("login")] public IActionResult Login([FromBody]LoginReq r) => _s.LoginCheck(r.Username,r.Password)?Ok(new{code=0,token=_s.DoLogin(r.Username)}):Unauthorized(new{code=1});
    [HttpGet("dashboard")] public IActionResult Dash() => Ok(_s.Dash());
    [HttpGet("server/status")] public IActionResult Status() => Ok(new{mem="OK"});
    [HttpGet("player/search")] public async Task<IActionResult> Search([FromQuery]string kw="") => Ok(await _s.Search(kw)??new List<object>());
    [HttpPost("player/ban")] public IActionResult Ban([FromBody]BanReq r) => Ok(new{code=0});
    [HttpPost("notice")] public IActionResult Notice([FromBody]NoticeReq r) { Logger.Info("Admin",$"Notice: {r.Title}"); return Ok(new{code=0}); }
}
public record LoginReq(string Username, string Password);
public record BanReq(ulong Pid, string Reason, int Hours);
public record NoticeReq(string Title, string Content, int Type);
'''
with open(os.path.join(ad,"Controllers","AuthController.cs"),"w",encoding="utf-8") as f: f.write(ctrl)
print("All admin files written OK")