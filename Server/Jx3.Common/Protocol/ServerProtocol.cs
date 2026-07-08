using Jx3.Common.Network;

namespace Jx3.Common.Protocol;

/// <summary>消息ID枚举</summary>
public enum MsgId : uint
{
    None = 0,

    // 登录 (1001-1099)
    CSLoginAuth = 1001,
    SCLoginAuthResult = 1002,
    CSLoginRegister = 1003,
    SCLoginRegisterResult = 1004,
    CSLoginCreateRole = 1005,
    SCLoginRoleList = 1006,
    CSLoginEnterGame = 1007,
    SCLoginEnterGame = 1008,
    SCLoginKick = 1009,

    // 英雄 (1101-1199)
    CSHeroList = 1101,
    SCHeroList = 1102,
    CSHeroLevelUp = 1103,
    SCHeroLevelUpdate = 1104,
    CSHeroStarUp = 1105,
    SCHeroStarUpdate = 1106,
    CSHeroTeamSet = 1107,
    SCHeroTeamInfo = 1108,

    // 招募 (1201-1299)
    CSRecruitDraw = 1201,
    SCRecruitDrawResult = 1202,
    CSRecruitPoolList = 1203,
    SCRecruitPoolList = 1204,

    // 战斗 (2001-2099)
    CSCombatMove = 2001,
    CSCombatCastSkill = 2002,
    SCCombatDamage = 2003,
    SCCombatHPChange = 2004,
    SCCombatStateInit = 2005,
    SCCombatEnd = 2006,

    // 副本 (3001-3099)
    CSDungeonList = 3001,
    SCDungeonList = 3002,
    CSDungeonEnter = 3003,
    SCDungeonEnterResult = 3004,
    SCDungeonBossHP = 3005,
    SCDungeonUltimateUnlock = 3006,
    SCDungeonComplete = 3007,

    // 交易 (4001-4099)
    CSTradeSearch = 4001,
    SCTradeSearchResult = 4002,
    CSTradeSell = 4003,
    SCTradeSellResult = 4004,
    CSTradeBuy = 4005,
    SCTradeBuyResult = 4006,
    SCTradeItemSold = 4007,
    CSTradeCancel = 4008,
    SCTradeCancelResult = 4009,
    CSTradeMyListings = 4010,
    SCTradeMyListings = 4011,
    CSTradeClaimGold = 4012,
    CSTradeClaimItem = 4013,
    SCTradeClaimItem = 4014,

    // 聊天 (5001-5099)
    CSChatSend = 5001,
    SCChatMessage = 5002,
    CSChatPrivate = 5003,
    SCChatPrivate = 5004,
    SCChatSystemNotice = 5005,

    // 组队 (6001-6099)
    CSTeamCreate = 6001,
    SCTeamInfo = 6002,
    CSTeamInvite = 6003,
    SCTeamInvite = 6004,
    CSTeamLeave = 6005,
    SCTeamMemberLeave = 6006,

    // 同盟 (7001-7099)
    CSGuildCreate = 7001,
    SCGuildInfo = 7002,
    CSGuildApply = 7003,
    CSGuildLeave = 7004,

    // 商城 (8001-8099)
    CSShopList = 8001,
    SCShopList = 8002,
    CSShopBuy = 8003,
    SCShopBuyResult = 8004,
    CSShopRecharge = 8005,
    SCCurrencyUpdate = 8006,

    // 任务 (9001-9099)
    CSQuestList = 9001,
    SCQuestList = 9002,
    CSQuestAccept = 9003,
    CSQuestSubmit = 9004,
    SCQuestReward = 9005,
}