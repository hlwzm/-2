using System.Collections.Concurrent;
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
}