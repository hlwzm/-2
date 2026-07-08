#nullable disable
using Jx3.Common;
using Jx3.Common.Protocol;
using Jx3.Common.Service;
using Jx3.Common.Utils;

namespace Jx3.Quest;

public class QuestData
{
    public uint QuestId;
    public string Name = "";
    public uint QuestType; // 0主线 1支线 2日常 3成就
    public uint MinLevel;
    public string Description = "";
    public uint Progress;
    public uint Target;
    public bool IsCompleted;
}

public static class QuestService
{
    public static readonly List<QuestData> QuestTemplates = new()
    {
        new() { QuestId = 1001, Name = "初入江湖", QuestType = 0, MinLevel = 1, Description = "去稻香村找王大石说话", Target = 1 },
        new() { QuestId = 1002, Name = "初识英雄", QuestType = 0, MinLevel = 1, Description = "通过招募获得一名英雄", Target = 1 },
        new() { QuestId = 1003, Name = "第一次战斗", QuestType = 0, MinLevel = 2, Description = "在稻香村击败10个山贼", Target = 10 },
        new() { QuestId = 1004, Name = "装备强化", QuestType = 1, MinLevel = 5, Description = "强化一次装备", Target = 1 },
        new() { QuestId = 2001, Name = "每日签到", QuestType = 2, MinLevel = 1, Description = "登录游戏", Target = 1 },
        new() { QuestId = 2002, Name = "每日副本", QuestType = 2, MinLevel = 20, Description = "完成一次副本", Target = 1 },
        new() { QuestId = 2003, Name = "每日竞技", QuestType = 2, MinLevel = 15, Description = "参与一次PVP", Target = 1 },
        new() { QuestId = 3001, Name = "英雄收集者", QuestType = 3, MinLevel = 1, Description = "收集5名不同的英雄", Target = 5 },
        new() { QuestId = 3002, Name = "百战勇士", QuestType = 3, MinLevel = 1, Description = "累计击败100个敌人", Target = 100 },
        new() { QuestId = 3003, Name = "财富积累", QuestType = 3, MinLevel = 1, Description = "累计获得100000金币", Target = 100000 },
    };
}

public class QuestServer : GameServer
{
    public QuestServer() : base("Quest", 9008) { }

    protected override Task OnStartAsync()
    {
        ServiceRegistry.RegisterHandler((uint)MsgId.CSQuestList, HandleQuestList);
        ServiceRegistry.RegisterHandler((uint)MsgId.CSQuestAccept, HandleQuestAccept);
        ServiceRegistry.RegisterHandler((uint)MsgId.CSQuestSubmit, HandleQuestSubmit);
        Logger.Info("Quest", "QuestServer ready on port 9008");
        return Task.CompletedTask;
    }

    private Task<byte[]> HandleQuestList(byte[] body)
    {
        using var r = new BinaryReader(new MemoryStream(body));
        var playerId = r.ReadUInt64();

        using var ms = new MemoryStream();
        using var w = new BinaryWriter(ms);
        w.Write(QuestService.QuestTemplates.Count);
        foreach (var q in QuestService.QuestTemplates)
        {
            w.Write(q.QuestId); w.Write(q.Name); w.Write(q.QuestType);
            w.Write(q.MinLevel); w.Write(q.Description); w.Write(q.Progress);
            w.Write(q.Target); w.Write(q.IsCompleted);
        }
        return Task.FromResult(ms.ToArray());
    }

    private Task<byte[]> HandleQuestAccept(byte[] body)
    {
        using var r = new BinaryReader(new MemoryStream(body));
        var playerId = r.ReadUInt64(); var questId = r.ReadUInt32();
        Logger.Info("Quest", $"Player {playerId} accepted quest {questId}");
        return Task.FromResult(new byte[] { 0 });
    }

    private Task<byte[]> HandleQuestSubmit(byte[] body)
    {
        using var r = new BinaryReader(new MemoryStream(body));
        var playerId = r.ReadUInt64(); var questId = r.ReadUInt32();
        Logger.Info("Quest", $"Player {playerId} submitted quest {questId}");
        using var ms = new MemoryStream();
        using var w = new BinaryWriter(ms);
        w.Write(0); // code=success
        w.Write(questId);
        w.Write(1000); // exp reward
        w.Write(500); // gold reward
        return Task.FromResult(ms.ToArray());
    }
}

public class Program { public static async Task Main() { await new QuestServer().StartAsync(); } }