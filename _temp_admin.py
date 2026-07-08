import os, sys
sys.stdout.reconfigure(encoding="utf-8")
ad = "D:\\CodexWorkSpace\\MyGame\\指尖江湖2\\Server\\Jx3.Admin"
os.makedirs(os.path.join(ad,"Controllers"), exist_ok=True)
os.makedirs(os.path.join(ad,"Properties"), exist_ok=True)

# Program.cs
p = 'using System.Text;using Microsoft.AspNetCore.Authentication.JwtBearer;using Microsoft.IdentityModel.Tokens;'
p += 'using Jx3.Admin.Controllers;using Jx3.Common.Config;using Jx3.Common.Utils;'
p += 'var cfg=GameConfigLoader.Load();var key="Jx3AdminSecret2026!#$%";'
p += 'var b=WebApplication.CreateBuilder(args);'
p += 'b.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o=>{o.TokenValidationParameters=new TokenValidationParameters{ValidateIssuer=false,ValidateAudience=false,ValidateLifetime=true,IssuerSigningKey=new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key))};});'
p += 'b.Services.AddAuthorization();b.Services.AddControllers();'
p += 'b.Services.AddCors(o=>o.AddDefaultPolicy(p=>p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));'
p += 'b.Services.AddSingleton<AdminService>();'
p += 'var app=b.Build();app.UseCors();app.UseAuthentication();app.UseAuthorization();app.MapControllers();'
p += 'app.MapGet("/",async(ctx)=>{ctx.Response.ContentType="text/html;charset=utf-8";await ctx.Response.WriteAsync("<h1>JX3 Admin</h1><p>OK</p>");});'
p += 'Logger.Info("Admin",$"Starting on port {cfg.AdminPort}");'
p += f'app.Run($"http://0.0.0.0:{cfg.AdminPort}");'

with open(os.path.join(ad,"Program.cs"),"w",encoding="utf-8") as f: f.write(p)

# csproj 
cs = '<Project Sdk="Microsoft.NET.Sdk.Web"><PropertyGroup><TargetFramework>net9.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable></PropertyGroup><ItemGroup><PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="9.0.0"/></ItemGroup><ItemGroup><ProjectReference Include="..\\Jx3.Common\\Jx3.Common.csproj"/></ItemGroup></Project>'
with open(os.path.join(ad,"Jx3.Admin.csproj"),"w",encoding="utf-8") as f: f.write(cs)

# appsettings
with open(os.path.join(ad,"appsettings.json"),"w",encoding="utf-8") as f: f.write("{}")
with open(os.path.join(ad,"Properties","launchSettings.json"),"w",encoding="utf-8") as f: f.write('{"profiles":{"Jx3.Admin":{"commandName":"Project","launchBrowser":false}}}')

print("Files created")