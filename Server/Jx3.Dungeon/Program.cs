#nullable disable
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using Jx3.Common;
using Jx3.Common.Config;
using Jx3.Common.Database;
using Jx3.Common.Network;
using Jx3.Common.Protocol;
using Jx3.Common.Service;
using Jx3.Common.Utils;

namespace Jx3.Dungeon;

public class DungeonServer : GameServer
{
    private TcpListener? _listener;
    private readonly ConcurrentDictionary<uint, TcpClient> _sessions = new();

    public DungeonServer() : base("Dungeon", 9005) { }

    protected override async Task OnStartAsync()
    {
        // 注册消息处理器
        ServiceRegistry.RegisterHandler((uint)MsgId.CSDungeonList, HandleDungeonList);
        ServiceRegistry.RegisterHandler((uint)MsgId.CSDungeonEnter, HandleDungeonEnter);
        ServiceRegistry.RegisterHandler((uint)MsgId.CSDungeonLeave, HandleDungeonLeave);
        ServiceRegistry.RegisterHandler((uint)MsgId.CSDungeonRevive, HandleRevive);
        ServiceRegistry.RegisterHandler((uint)MsgId.CSDungeonLoot, HandleLoot);

        // 启动过期清理定时器
        _ = Task.Run(async () =>
        {
            while (true)
            {
                await Task.Delay(300000); // 5分钟
                DungeonService.CleanupExpired();
            }
        });

        Logger.Info("Dungeon", "DungeonServer ready on port 9005");
        await Task.Delay(-1);
    }

    private Task<byte[]> HandleDungeonList(byte[] body)
    {
        var dungeons = DungeonService.GetDungeonList();
        using var ms = new MemoryStream();
        using var w = new BinaryWriter(ms);
        w.Write(dungeons.Count);
        foreach (var d in dungeons)
        {
            w.Write(d.DungeonId);
            w.Write(d.Name);
            w.Write(d.MinLevel);
            w.Write(d.MinPlayers);
            w.Write(d.MaxPlayers);
            w.Write(d.Description);
        }
        return Task.FromResult(ms.ToArray());
    }

    private async Task<byte[]> HandleDungeonEnter(byte[] body)
    {
        using var r = new BinaryReader(new MemoryStream(body));
        var dungeonId = r.ReadInt32();
        var difficulty = r.ReadInt32();
        var teamId = r.ReadUInt64();
        var playerCount = r.ReadInt32();
        var players = new List<ulong>();
        for (int i = 0; i < playerCount; i++)
            players.Add(r.ReadUInt64());

        var (code, inst) = DungeonService.EnterDungeon(dungeonId, difficulty, teamId, players);
        using var ms = new MemoryStream();
        using var w = new BinaryWriter(ms);
        w.Write(code);
        if (code == 0 && inst != null)
        {
            w.Write(inst.ProgressId);
            w.Write(inst.DungeonId);
            w.Write(inst.Difficulty);
            WriteBossStates(w, inst);
        }
        else
        {
            w.Write(0UL); // progressId placeholder
            w.Write(0);
            w.Write(0);
        }
        return ms.ToArray();
    }

    private Task<byte[]> HandleDungeonLeave(byte[] body)
    {
        using var r = new BinaryReader(new MemoryStream(body));
        var progressId = r.ReadUInt64();
        var playerId = r.ReadUInt64();
        var inst = DungeonService.LeaveDungeon(progressId, playerId);
        return Task.FromResult(new[] { (byte)(inst != null ? 1 : 0) });
    }

    private Task<byte[]> HandleRevive(byte[] body)
    {
        using var r = new BinaryReader(new MemoryStream(body));
        var progressId = r.ReadUInt64();
        var playerId = r.ReadUInt64();
        var useGold = r.ReadBoolean();
        var result = DungeonService.RevivePlayer(progressId, playerId, useGold);
        return Task.FromResult(BitConverter.GetBytes(result));
    }

    private Task<byte[]> HandleLoot(byte[] body)
    {
        // 简化的掉落处理：返回成功
        using var ms = new MemoryStream();
        using var w = new BinaryWriter(ms);
        w.Write(0); // code=成功
        w.Write(1001); // item_id
        w.Write(1); // count
        return Task.FromResult(ms.ToArray());
    }

    private static void WriteBossStates(BinaryWriter w, DungeonInstance inst)
    {
        foreach (var boss in inst.Bosses)
        {
            w.Write(boss.BossIndex);
            w.Write(boss.Name);
            w.Write(boss.MaxHp);
            w.Write(boss.CurrentHp);
            w.Write(boss.Phase);
            w.Write(boss.IsDead);
            w.Write(boss.KillTimeSec);
        }
        w.Write(inst.UltimateUnlocked);
        w.Write(inst.ElapsedSeconds);
    }
}

public class Program
{
    public static async Task Main()
    {
        await new DungeonServer().StartAsync();
    }
}