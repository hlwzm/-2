#nullable disable
﻿using Jx3.Common;
using Jx3.Common.Config;
using Jx3.Common.Database;
using Jx3.Common.Protocol;
using Jx3.Common.Service;
using Jx3.Common.Utils;
using System.Text.Json;

namespace Jx3.Hero;

public class HeroServer : GameServer
{
    public HeroServer() : base("Hero", GameConfig.HeroPort) { }

    protected override async Task OnStartAsync()
    {
        Logger.Info("Hero", "Initializing Hero + Recruit services...");

        var db = new DbHelper(GameConfig.MySQLConn);
        var redis = new RedisHelper(GameConfig.RedisConn);
        var heroSvc = new HeroService(db, redis);
        var recruitSvc = new RecruitService(db, redis);

        // 注册到服务容器
        ServiceRegistry.RegisterService("HeroService", heroSvc);
        ServiceRegistry.RegisterService("RecruitService", recruitSvc);

        // ========== 英雄消息注册 ==========

        // CSHeroList (1101) -> SCHeroList (1102)
        ServiceRegistry.RegisterHandler((uint)MsgId.CSHeroList, async body =>
        {
            try
            {
                var req = JsonSerializer.Deserialize<PlayerReq>(body);
                if (req == null) return ErrJson("参数错误");
                var result = await heroSvc.GetHeroListAsync(req.player_id);
                return EncodeResponse((uint)MsgId.SCHeroList, result);
            }
            catch (Exception ex)
            {
                Logger.Error("Hero", $"CSHeroList error: {ex.Message}");
                return ErrJson("服务器内部错误");
            }
        });

        // CSHeroLevelUp (1103) -> SCHeroLevelUpdate (1104)
        ServiceRegistry.RegisterHandler((uint)MsgId.CSHeroLevelUp, async body =>
        {
            try
            {
                var req = JsonSerializer.Deserialize<HeroUidReq>(body);
                if (req == null) return ErrJson("参数错误");
                var result = await heroSvc.LevelUpAsync(req.hero_uid, req.player_id);
                return EncodeResponse((uint)MsgId.SCHeroLevelUpdate, result);
            }
            catch (Exception ex)
            {
                Logger.Error("Hero", $"CSHeroLevelUp error: {ex.Message}");
                return ErrJson("服务器内部错误");
            }
        });

        // CSHeroStarUp (1105) -> SCHeroStarUpdate (1106)
        ServiceRegistry.RegisterHandler((uint)MsgId.CSHeroStarUp, async body =>
        {
            try
            {
                var req = JsonSerializer.Deserialize<HeroUidReq>(body);
                if (req == null) return ErrJson("参数错误");
                var result = await heroSvc.StarUpAsync(req.hero_uid, req.player_id);
                return EncodeResponse((uint)MsgId.SCHeroStarUpdate, result);
            }
            catch (Exception ex)
            {
                Logger.Error("Hero", $"CSHeroStarUp error: {ex.Message}");
                return ErrJson("服务器内部错误");
            }
        });

        // CSHeroTeamSet (1107) -> SCHeroTeamInfo (1108)
        ServiceRegistry.RegisterHandler((uint)MsgId.CSHeroTeamSet, async body =>
        {
            try
            {
                var req = JsonSerializer.Deserialize<TeamSetReq>(body);
                if (req == null) return ErrJson("参数错误");
                var result = await heroSvc.SetTeamAsync(req.player_id, req.hero_ids);
                return EncodeResponse((uint)MsgId.SCHeroTeamInfo, result);
            }
            catch (Exception ex)
            {
                Logger.Error("Hero", $"CSHeroTeamSet error: {ex.Message}");
                return ErrJson("服务器内部错误");
            }
        });

        // ========== 招募消息注册 ==========

        // CSRecruitDraw (1201) -> SCRecruitDrawResult (1202)
        ServiceRegistry.RegisterHandler((uint)MsgId.CSRecruitDraw, async body =>
        {
            try
            {
                var req = JsonSerializer.Deserialize<DrawReq>(body);
                if (req == null) return ErrJson("参数错误");
                var result = await recruitSvc.DrawAsync(req.player_id, req.pool_id, req.count);
                return EncodeResponse((uint)MsgId.SCRecruitDrawResult, result);
            }
            catch (Exception ex)
            {
                Logger.Error("Hero", $"CSRecruitDraw error: {ex.Message}");
                return ErrJson("服务器内部错误");
            }
        });

        // CSRecruitPoolList (1203) -> SCRecruitPoolList (1204)
        ServiceRegistry.RegisterHandler((uint)MsgId.CSRecruitPoolList, body =>
        {
            try
            {
                var result = recruitSvc.GetPoolList();
                return Task.FromResult(EncodeResponse((uint)MsgId.SCRecruitPoolList, result));
            }
            catch (Exception ex)
            {
                Logger.Error("Hero", $"CSRecruitPoolList error: {ex.Message}");
                return Task.FromResult(ErrJson("服务器内部错误"));
            }
        });

        Logger.Info("Hero", "Hero + Recruit services ready, 6 handlers registered");
        await Task.CompletedTask;
    }

    private static byte[]? EncodeResponse(uint msgId, string jsonBody)
    {
        var packet = new Jx3.Common.Network.MessagePacket
        {
            MsgId = msgId,
            Body = System.Text.Encoding.UTF8.GetBytes(jsonBody)
        };
        return packet.Encode();
    }

    private static byte[]? ErrJson(string msg)
    {
        var json = JsonSerializer.Serialize(new { code = 1, msg });
        return System.Text.Encoding.UTF8.GetBytes(json);
    }
}

// ========== 请求DTO ==========

internal class PlayerReq
{
    public long player_id { get; set; }
}

internal class HeroUidReq
{
    public long player_id { get; set; }
    public ulong hero_uid { get; set; }
}

internal class TeamSetReq
{
    public long player_id { get; set; }
    public List<ulong> hero_ids { get; set; } = new();
}

internal class DrawReq
{
    public long player_id { get; set; }
    public int pool_id { get; set; }
    public int count { get; set; }
}

// ========== 入口 ==========

public class Program
{
    public static async Task Main()
    {
        GameConfigLoader.Load();
        await new HeroServer().StartAsync();
    }
}
