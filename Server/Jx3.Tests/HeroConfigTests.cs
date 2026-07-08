using Xunit;
using Jx3.Hero;

namespace Jx3.Tests;

public class HeroConfigTests
{
    [Fact]
    public void HeroTemplates_AllDefined()
    {
        Assert.NotEmpty(HeroTemplateConfig.All);
        Assert.True(HeroTemplateConfig.All.Count >= 8);
    }

    [Fact]
    public void LiFu_IsQuality5()
    {
        var h = HeroTemplateConfig.All[1001];
        Assert.Equal("李复", h.Name);
        Assert.Equal(5, h.Quality);
        Assert.Equal(1, h.RoleType);
    }

    [Fact]
    public void ChenYue_IsHealer()
    {
        var h = HeroTemplateConfig.All[1003];
        Assert.Equal("陈月", h.Name);
        Assert.Equal(3, h.RoleType);
        Assert.True(h.BaseHealPower > 0);
    }

    [Fact]
    public void LiChengEn_IsTank()
    {
        var h = HeroTemplateConfig.All[1005];
        Assert.Equal("李承恩", h.Name);
        Assert.Equal(2, h.RoleType);
        Assert.True(h.BaseDef > 150);
        Assert.True(h.BaseHp > 3500);
    }

    [Fact]
    public void LevelUpGoldCost_Level1_Returns100()
    {
        Assert.Equal(100, HeroTemplateConfig.LevelUpGoldCost(1));
    }

    [Fact]
    public void LevelUpGoldCost_Level50_Returns5000()
    {
        Assert.Equal(5000, HeroTemplateConfig.LevelUpGoldCost(50));
    }

    [Fact]
    public void LevelUpExpCost_Level1_Returns200()
    {
        Assert.Equal(200, HeroTemplateConfig.LevelUpExpCost(1));
    }

    [Fact]
    public void MaxLevel_Is100()
    {
        Assert.Equal(100, HeroTemplateConfig.MaxLevel);
    }

    [Fact]
    public void StarUpFragmentCost_Defined()
    {
        Assert.Equal(30, HeroTemplateConfig.StarUpFragmentCost[1]);
        Assert.Equal(60, HeroTemplateConfig.StarUpFragmentCost[2]);
        Assert.Equal(90, HeroTemplateConfig.StarUpFragmentCost[3]);
    }

    [Fact]
    public void RecruitPools_ThreePools_Defined()
    {
        Assert.Equal(3, RecruitPoolConfig.Pools.Count);
        Assert.Contains(RecruitPoolConfig.Pools, p => p.PoolId == 1);
        Assert.Contains(RecruitPoolConfig.Pools, p => p.PoolId == 2);
        Assert.Contains(RecruitPoolConfig.Pools, p => p.PoolId == 3);
    }

    [Fact]
    public void NovicePool_Has90Pity()
    {
        var pool = RecruitPoolConfig.Pools[0];
        Assert.True(pool.IsNovice);
        Assert.Equal(90, pool.MaxPity);
    }
}
