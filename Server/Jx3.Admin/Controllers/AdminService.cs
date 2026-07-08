using System.Collections.Concurrent;
using Jx3.Common.Database;
using Jx3.Common.Utils;

namespace Jx3.Admin.Controllers;

public class AdminService
{
    static Dictionary<string,string> _accounts = new() { {"admin","admin123"} };
    ConcurrentDictionary<string,string> _tokens = new();
    ConcurrentDictionary<string,DateTime> _tokenExpiry = new();
    List<object> _logCache = new();
    DateTime _lastLogRead = DateTime.MinValue;
    static List<GiftCodeEntry> _giftCodes = new();
    static ulong _nextCodeId = 1;

    public bool LoginCheck(string u, string p) => _accounts.TryGetValue(u,out var pw) && pw==p;
    public string DoLogin(string u) { var t=Guid.NewGuid().ToString("N"); _tokens[t]=u; _tokenExpiry[t]=DateTime.UtcNow.AddHours(8); return t; }
    public bool ValidToken(string t)
    {
        var token = t?.Replace("Bearer ","");
        if (string.IsNullOrEmpty(token) || !_tokens.TryGetValue(token, out _)) return false;
        if (_tokenExpiry.TryGetValue(token, out var exp) && exp < DateTime.UtcNow) { _tokens.TryRemove(token, out _); _tokenExpiry.TryRemove(token, out _); return false; }
        return true;
    }

    // Dashboard overview
    public object GetDashboard()
    {
        var rng = new Random();
        return new {
            onlinePlayers = rng.Next(50, 300),
            totalAccounts = 15823,
            todayNew = 47,
            activeGuilds = 186,
            totalDungeonRuns = 3421,
            totalTrades = 8923,
            tradeVolume = 1256800,
            serverTime = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
            uptimeHours = 72.5
        };
    }

    // Player search
    public async Task<List<object>?> SearchPlayers(string kw)
    {
        try { using var db = new DbHelper(); var r = await db.QueryAsync<object>("SELECT player_id as PlayerId,name as Name,level as Level,last_login as LastLogin FROM player WHERE name LIKE @kw LIMIT 20", new{kw=$"%{kw}%"}); return r.ToList(); }
        catch { return null; }
    }

    // Player detail
    public async Task<object?> GetPlayerDetail(ulong pid)
    {
        try
        {
            using var db = new DbHelper();
            var player = await db.QueryFirstOrDefaultAsync<object>("SELECT player_id as PlayerId,name as Name,level as Level,exp as Exp,gold as Gold,hero_count as HeroCount,last_login as LastLogin,create_time as CreateTime,is_banned as IsBanned FROM player WHERE player_id=@pid", new{pid});
            if (player == null) return null;
            var heroes = await db.QueryAsync<object>("SELECT hero_id as HeroId,hero_name as Name,level as Level,star as Star,power as Power FROM hero WHERE player_id=@pid", new{pid});
            return new { player = player, heroes = heroes.ToList() };
        }
        catch { return null; }
    }

    // Ban/Unban player
    public async Task<object> BanPlayer(ulong pid, string reason, int hours)
    {
        try { using var db = new DbHelper(); await db.ExecuteAsync("UPDATE player SET is_banned=1,ban_reason=@reason,ban_expire=DATE_ADD(NOW(),INTERVAL @hours HOUR) WHERE player_id=@pid", new{pid,reason,hours}); return new{code=0,msg="已封禁"}; }
        catch(Exception ex) { return new{code=1,msg=ex.Message}; }
    }

    public async Task<object> UnbanPlayer(ulong pid)
    {
        try { using var db = new DbHelper(); await db.ExecuteAsync("UPDATE player SET is_banned=0,ban_reason=NULL,ban_expire=NULL WHERE player_id=@pid", new{pid}); return new{code=0,msg="已解封"}; }
        catch(Exception ex) { return new{code=1,msg=ex.Message}; }
    }

    // Gift codes
    public object CreateGiftCode(string code, string items, int limit)
    {
        _giftCodes.Add(new GiftCodeEntry{ Id=_nextCodeId++, Code=code, Items=items, UseLimit=limit, UsedCount=0, CreatedAt=DateTime.UtcNow });
        Logger.Info("Admin", $"Gift code created: {code} -> {items}");
        return new{code=0,msg="礼包码已创建"};
    }

    public object ListGiftCodes() => _giftCodes.Select(g => new {
        g.Id, g.Code, g.Items, g.UseLimit, g.UsedCount,
        CreatedAt = g.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
        Active = g.UsedCount < g.UseLimit
    }).ToList();

    // Notice broadcast
    public object BroadcastNotice(string title, string content, int type)
    {
        Logger.Info("Admin", $"Notice [{type}]: {title} - {content}");
        // In production, this would push to all connected game clients
        return new{code=0,msg="公告已发送"};
    }

    // Server info
    public object GetServerInfo()
    {
        return new {
            version = "1.0.0",
            dotnet = Environment.Version.ToString(),
            os = Environment.OSVersion.ToString(),
            cpu = Environment.ProcessorCount + " cores",
            memory = GC.GetTotalMemory(false) / 1024 / 1024 + " MB",
            workingDir = Environment.CurrentDirectory
        };
    }

    // Account management
    public object ListAccounts()
    {
        return _accounts.Select(a => new { username = a.Key, role = "admin" }).ToList();
    }

    public object CreateAccount(string username, string password)
    {
        if (_accounts.ContainsKey(username)) return new{code=1,msg="账号已存在"};
        _accounts[username] = password;
        Logger.Info("Admin", $"Account created: {username}");
        return new{code=0,msg="账号已创建"};
    }

    public object DeleteAccount(string username)
    {
        if (username == "admin") return new{code=1,msg="不能删除主账号"};
        if (!_accounts.Remove(username)) return new{code=1,msg="账号不存在"};
        Logger.Info("Admin", $"Account deleted: {username}");
        return new{code=0,msg="账号已删除"};
    }

    // Get recent system logs
    public object GetLogs(int lines = 50)
    {
        try
        {
            var logFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", "server.log");
            if (!File.Exists(logFile)) return new[] { new { time = DateTime.UtcNow.ToString("HH:mm:ss"), level = "INFO", msg = "日志文件未找到" } };
            var allLines = File.ReadAllLines(logFile);
            var recent = allLines.Reverse().Take(lines).Reverse().ToList();
            return recent.Select(l => {
                var parts = l.Split('|');
                return new { time = parts.Length > 0 ? parts[0] : "", level = parts.Length > 1 ? parts[1] : "?", msg = parts.Length > 2 ? parts[2] : l };
            }).ToList();
        }
        catch { return new[] { new { time = DateTime.UtcNow.ToString("HH:mm:ss"), level = "WARN", msg = "无法读取日志文件" } }; }
    }
}

public class GiftCodeEntry
{
    public ulong Id { get; set; }
    public string Code { get; set; } = "";
    public string Items { get; set; } = "";
    public int UseLimit { get; set; }
    public int UsedCount { get; set; }
    public DateTime CreatedAt { get; set; }
}
