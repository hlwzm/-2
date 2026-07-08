using Microsoft.AspNetCore.Mvc;
namespace Jx3.Admin.Controllers;
[ApiController][Route("api/admin")]
public class AuthController : ControllerBase {
    AdminService _s; public AuthController(AdminService s) => _s = s;
    [HttpPost("login")] public IActionResult Login([FromBody]LoginReq r) => _s.LoginCheck(r.Username,r.Password)?Ok(new{code=0,token=_s.DoLogin(r.Username)}):Unauthorized(new{code=1});
    [HttpGet("dashboard")] public IActionResult Dash() => Ok(_s.Dash());
    [HttpGet("server/status")] public IActionResult Status() => Ok(new{mem="OK"});
    [HttpGet("player/search")] public async Task<IActionResult> Search([FromQuery]string kw="") => Ok(await _s.Search(kw)??new List<object>());
    [HttpPost("player/ban")] public IActionResult Ban([FromBody]BanReq r) => Ok(new{code=0});
    [HttpPost("notice")] public IActionResult Notice([FromBody]NoticeReq r) { Jx3.Common.Utils.Logger.Info("Admin","Notice: "+r.Title); return Ok(new{code=0}); }
}
public record LoginReq(string Username, string Password);
public record BanReq(ulong Pid, string Reason, int Hours);
public record NoticeReq(string Title, string Content, int Type);