using System.Text.Json.Serialization;

namespace Jx3.Hero;

/// <summary>英雄模板数据</summary>
public class HeroTemplate
{
    public int TemplateId { get; set; }
    public string Name { get; set; } = "";
    public int Quality { get; set; }
    public int RoleType { get; set; } // 1=输出 2=坦克 3=治疗
    public int AttackType { get; set; } // 1=外功 2=内功
    public int BaseAtk { get; set; }
    public int BaseDef { get; set; }
    public int BaseHp { get; set; }
    public int BaseHealPower { get; set; }
    public double BaseCritRate { get; set; }
    public double BaseCritDamage { get; set; }
    public double BaseDodgeRate { get; set; }
    public int BaseSpeed { get; set; }
}

/// <summary>英雄模板配置（硬编码）</summary>
public static class HeroTemplateConfig
{
    public static readonly Dictionary<int, HeroTemplate> All = new()
    {
        [1001] = new() { TemplateId = 1001, Name = "李复",   Quality = 5, RoleType = 1, AttackType = 1, BaseAtk = 220, BaseDef = 80,  BaseHp = 2800, BaseHealPower = 0,   BaseCritRate = 0.15, BaseCritDamage = 1.5, BaseDodgeRate = 0.10, BaseSpeed = 110 },
        [1002] = new() { TemplateId = 1002, Name = "秋叶青", Quality = 4, RoleType = 1, AttackType = 2, BaseAtk = 140, BaseDef = 50,  BaseHp = 1700, BaseHealPower = 0,   BaseCritRate = 0.12, BaseCritDamage = 1.4, BaseDodgeRate = 0.08, BaseSpeed = 105 },
        [1003] = new() { TemplateId = 1003, Name = "陈月",   Quality = 4, RoleType = 3, AttackType = 2, BaseAtk = 80,  BaseDef = 45,  BaseHp = 1600, BaseHealPower = 150, BaseCritRate = 0.05, BaseCritDamage = 1.2, BaseDodgeRate = 0.05, BaseSpeed = 100 },
        [1004] = new() { TemplateId = 1004, Name = "裴元",   Quality = 4, RoleType = 3, AttackType = 2, BaseAtk = 85,  BaseDef = 45,  BaseHp = 1600, BaseHealPower = 140, BaseCritRate = 0.05, BaseCritDamage = 1.2, BaseDodgeRate = 0.05, BaseSpeed = 100 },
        [1005] = new() { TemplateId = 1005, Name = "李承恩", Quality = 5, RoleType = 2, AttackType = 1, BaseAtk = 130, BaseDef = 180, BaseHp = 4000, BaseHealPower = 0,   BaseCritRate = 0.08, BaseCritDamage = 1.3, BaseDodgeRate = 0.05, BaseSpeed = 90  },
        [1006] = new() { TemplateId = 1006, Name = "叶英",   Quality = 5, RoleType = 1, AttackType = 1, BaseAtk = 240, BaseDef = 75,  BaseHp = 2600, BaseHealPower = 0,   BaseCritRate = 0.18, BaseCritDamage = 1.6, BaseDodgeRate = 0.12, BaseSpeed = 115 },
        [1007] = new() { TemplateId = 1007, Name = "玄正",   Quality = 5, RoleType = 2, AttackType = 2, BaseAtk = 120, BaseDef = 190, BaseHp = 4200, BaseHealPower = 0,   BaseCritRate = 0.06, BaseCritDamage = 1.3, BaseDodgeRate = 0.05, BaseSpeed = 85  },
        [1008] = new() { TemplateId = 1008, Name = "渡会",   Quality = 4, RoleType = 1, AttackType = 1, BaseAtk = 150, BaseDef = 55,  BaseHp = 1800, BaseHealPower = 0,   BaseCritRate = 0.12, BaseCritDamage = 1.4, BaseDodgeRate = 0.08, BaseSpeed = 105 },
    };

    /// <summary>升级消耗 金币=等级*100 经验=等级*200</summary>
    public static int LevelUpGoldCost(int level) => level * 100;
    public static int LevelUpExpCost(int level) => level * 200;

    /// <summary>升星碎片消耗</summary>
    public static readonly Dictionary<int, int> StarUpFragmentCost = new()
    {
        [1] = 30,
        [2] = 60,
        [3] = 90,
    };

    /// <summary>重复英雄转换碎片数</summary>
    public const int DuplicateHeroFragments = 50;

    /// <summary>最大等级</summary>
    public const int MaxLevel = 100;

    /// <summary>最大星级</summary>
    public const int MaxStar = 6;
}

/// <summary>招募卡池配置</summary>
public static class RecruitPoolConfig
{
    public static readonly List<RecruitPoolInfo> Pools = new()
    {
        new()
        {
            PoolId = 1, Name = "新手招募", Description = "首次十连必出5星英雄",
            CostType = 1, CostAmount = 300, MaxPity = 90, UpHeroId = 0,
            EndTime = 0, IsNovice = true
        },
        new()
        {
            PoolId = 2, Name = "常驻招募", Description = "标准概率，无UP英雄",
            CostType = 1, CostAmount = 300, MaxPity = 90, UpHeroId = 0,
            EndTime = 0
        },
        new()
        {
            PoolId = 3, Name = "限时UP·李复", Description = "李复获取概率大幅提升",
            CostType = 1, CostAmount = 300, MaxPity = 90, UpHeroId = 1001,
            EndTime = DateTimeOffset.UtcNow.AddDays(14).ToUnixTimeSeconds()
        },
    };
}

/// <summary>卡池信息</summary>
public class RecruitPoolInfo
{
    public int PoolId { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public int CostType { get; set; }
    public int CostAmount { get; set; }
    public int MaxPity { get; set; } = 90;
    public int UpHeroId { get; set; }
    public long EndTime { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool IsNovice { get; set; }
}
