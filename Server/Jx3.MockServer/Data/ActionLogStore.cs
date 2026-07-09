using System.Text.Json;
namespace Jx3.MockServer.Data;

public class ActionLog
{
    public ulong Id { get; set; }
    public ulong PlayerId { get; set; }
    public string PlayerName { get; set; } = "";
    public string ActionType { get; set; } = "";
    public string Detail { get; set; } = "";
    public DateTime Timestamp { get; set; }
    public string IpAddress { get; set; } = "";
}

public class ActionLogStore
{
    private static readonly Lazy<ActionLogStore> _instance = new(() => new ActionLogStore());
    public static ActionLogStore Instance => _instance.Value;

    private readonly List<ActionLog> _logs = new();
    private ulong _nextId = 1;

    public void AddLog(ulong playerId, string playerName, string actionType, string detail, string ipAddress = "")
    {
        lock (_logs)
        {
            _logs.Add(new ActionLog
            {
                Id = _nextId++,
                PlayerId = playerId,
                PlayerName = playerName,
                ActionType = actionType,
                Detail = detail,
                Timestamp = DateTime.UtcNow,
                IpAddress = ipAddress
            });
            if (_logs.Count > 50000) _logs.RemoveRange(0, _logs.Count - 50000);
        }
    }

    public (List<ActionLog> items, int total) Search(ulong? playerId, string? actionType, DateTime? startDate, DateTime? endDate, int page, int pageSize)
    {
        lock (_logs)
        {
            var q = _logs.AsEnumerable();
            if (playerId.HasValue && playerId.Value > 0) q = q.Where(l => l.PlayerId == playerId.Value);
            if (!string.IsNullOrEmpty(actionType)) q = q.Where(l => l.ActionType == actionType);
            if (startDate.HasValue) q = q.Where(l => l.Timestamp >= startDate.Value);
            if (endDate.HasValue) q = q.Where(l => l.Timestamp <= endDate.Value);
            q = q.OrderByDescending(l => l.Timestamp);
            var total = q.Count();
            var items = q.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            return (items, total);
        }
    }

    public (int loginCount, int chatCount, int tradeCount, int dungeonCount, int pvpCount, int guildCount) GetTodayStats()
    {
        var today = DateTime.UtcNow.Date;
        lock (_logs)
        {
            var todayLogs = _logs.Where(l => l.Timestamp >= today).ToList();
            return (
                todayLogs.Count(l => l.ActionType == "login"),
                todayLogs.Count(l => l.ActionType == "chat"),
                todayLogs.Count(l => l.ActionType == "trade_buy" || l.ActionType == "trade_sell"),
                todayLogs.Count(l => l.ActionType == "dungeon"),
                todayLogs.Count(l => l.ActionType == "pvp"),
                todayLogs.Count(l => l.ActionType == "guild")
            );
        }
    }

    public List<string> GetActionTypes()
    {
        lock (_logs)
        {
            return _logs.Select(l => l.ActionType).Distinct().OrderBy(t => t).ToList();
        }
    }

    public List<ActionLog> GetRecent(int count)
    {
        lock (_logs)
        {
            return _logs.OrderByDescending(l => l.Timestamp).Take(count).ToList();
        }
    }
}
