#nullable disable
using Jx3.Common;
using Jx3.Common.Config;
using Jx3.Common.Protocol;
using Jx3.Common.Service;
using Jx3.Common.Utils;

namespace Jx3.PVP;

public class PvpServer : GameServer
{
    public PvpServer() : base("PVP", GameConfig.PvpPort) { }

    protected override Task OnStartAsync()
    {
        ServiceRegistry.RegisterHandler((uint)MsgId.CSPVPMatchStart, HandleMatchStart);
        ServiceRegistry.RegisterHandler((uint)MsgId.CSPVPMatchCancel, HandleMatchCancel);
        ServiceRegistry.RegisterHandler((uint)MsgId.CSPVPRankInfo, HandleRankInfo);

        // 匹配定时器: 每3秒尝试匹配
        _ = Task.Run(async () =>
        {
            while (true)
            {
                await Task.Delay(3000);
                var (match, a, b) = PvpService.TryMatch();
                if (match != null)
                {
                    Logger.Info("PVP", $"Auto-match: {a?.Name} vs {b?.Name}");
                }
            }
        });

        Logger.Info("PVP", "PVPServer ready on port 9007");
        return Task.CompletedTask;
    }

    private Task<byte[]> HandleMatchStart(byte[] body)
    {
        using var r = new BinaryReader(new MemoryStream(body));
        var playerId = r.ReadUInt64();
        var name = r.ReadString();

        var code = PvpService.JoinQueue(playerId, name);
        using var ms = new MemoryStream();
        using var w = new BinaryWriter(ms);
        w.Write(code);
        return Task.FromResult(ms.ToArray());
    }

    private Task<byte[]> HandleMatchCancel(byte[] body)
    {
        using var r = new BinaryReader(new MemoryStream(body));
        var playerId = r.ReadUInt64();
        var ok = PvpService.LeaveQueue(playerId);
        return Task.FromResult(new[] { ok ? (byte)1 : (byte)0 });
    }

    private Task<byte[]> HandleRankInfo(byte[] body)
    {
        using var r = new BinaryReader(new MemoryStream(body));
        var playerId = r.ReadUInt64();
        var player = PvpService.GetPlayer(playerId);

        using var ms = new MemoryStream();
        using var w = new BinaryWriter(ms);
        if (player != null)
        {
            w.Write(0); // code
            w.Write(player.Rating);
            w.Write(player.Tier);
            w.Write(PvpService.TierNames[player.Tier]);
            w.Write(player.TotalWins);
            w.Write(player.TotalLosses);
            w.Write(player.WinStreak);
        }
        else
        {
            w.Write(1); // not found
        }
        return Task.FromResult(ms.ToArray());
    }
}

public class Program { public static async Task Main() { GameConfigLoader.Load(); await new PvpServer().StartAsync(); } }