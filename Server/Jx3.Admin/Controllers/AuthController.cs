using Microsoft.AspNetCore.Mvc;

namespace Jx3.Admin.Controllers;

[ApiController]
[Route("api/admin")]
public class AuthController : ControllerBase
{
    AdminService _s;
    public AuthController(AdminService s) => _s = s;

    // Auth
    [HttpPost("login")] public IActionResult Login([FromBody] LoginReq r) =>
        _s.LoginCheck(r.Username, r.Password)
            ? Ok(new { code = 0, token = _s.DoLogin(r.Username) })
            : Unauthorized(new { code = 1, msg = "账号或密码错误" });

    [HttpGet("accounts")] public IActionResult ListAccounts() => Ok(_s.ListAccounts());
    [HttpPost("accounts")] public IActionResult CreateAccount([FromBody] AccountReq r) => Ok(_s.CreateAccount(r.Username, r.Password));
    [HttpDelete("accounts/{username}")] public IActionResult DeleteAccount(string username) => Ok(_s.DeleteAccount(username));

    // Dashboard
    [HttpGet("dashboard")] public IActionResult Dashboard() => Ok(_s.GetDashboard());

    // Player
    [HttpGet("player/search")] public async Task<IActionResult> Search([FromQuery] string kw = "") =>
        Ok(await _s.SearchPlayers(kw) ?? new List<object>());

    [HttpGet("player/{pid}")] public async Task<IActionResult> PlayerDetail(ulong pid) {
        var r = await _s.GetPlayerDetail(pid);
        return r != null ? Ok(r) : NotFound(new { code = 1, msg = "玩家不存在" });
    }

    [HttpPost("player/ban")] public async Task<IActionResult> Ban([FromBody] BanReq r) => Ok(await _s.BanPlayer(r.Pid, r.Reason, r.Hours));
    [HttpPost("player/unban")] public async Task<IActionResult> Unban([FromBody] PidReq r) => Ok(await _s.UnbanPlayer(r.Pid));

    // Notice
    [HttpPost("notice")] public IActionResult Notice([FromBody] NoticeReq r) => Ok(_s.BroadcastNotice(r.Title, r.Content, r.Type));

    // Gift codes
    [HttpPost("giftcode")] public IActionResult CreateGiftCode([FromBody] GiftCodeReq r) => Ok(_s.CreateGiftCode(r.Code, r.Items, r.Limit));
    [HttpGet("giftcodes")] public IActionResult ListGiftCodes() => Ok(_s.ListGiftCodes());

    // Server
    [HttpGet("server/info")] public IActionResult ServerInfo() => Ok(_s.GetServerInfo());
    [HttpGet("server/logs")] public IActionResult Logs([FromQuery] int lines = 50) => Ok(_s.GetLogs(lines));
}

public record LoginReq(string Username, string Password);
public record BanReq(ulong Pid, string Reason, int Hours);
public record PidReq(ulong Pid);
public record NoticeReq(string Title, string Content, int Type);
public record GiftCodeReq(string Code, string Items, int Limit);
public record AccountReq(string Username, string Password);
