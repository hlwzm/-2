using Xunit;
using Jx3.Shop;

namespace Jx3.Tests;

public class ShopServiceTests
{
    [Fact]
    public void ShopItems_AllCategories_Defined()
    {
        Assert.NotEmpty(ShopService.Items);
        Assert.Contains(ShopService.Items, i => i.Category == 1);
        Assert.Contains(ShopService.Items, i => i.Category == 2);
        Assert.Contains(ShopService.Items, i => i.Category == 3);
        Assert.Contains(ShopService.Items, i => i.Category == 4);
        Assert.Contains(ShopService.Items, i => i.Category == 5);
    }

    [Fact]
    public void RechargeTiers_SixTiers_Defined()
    {
        Assert.Equal(6, ShopService.RechargeTiers.Count);
        Assert.Equal(6u, ShopService.RechargeTiers[0].RmbAmount);
        Assert.Equal(648u, ShopService.RechargeTiers[5].RmbAmount);
    }

    [Fact]
    public void BuyItem_ValidItem_ReturnsSuccess()
    {
        var (code, err) = ShopService.BuyItem(1001, 1001);
        Assert.Equal(0, code);
        Assert.Null(err);
    }

    [Fact]
    public void BuyItem_InvalidItem_ReturnsError()
    {
        var (code, err) = ShopService.BuyItem(1001, 99999);
        Assert.Equal(1, code);
        Assert.NotNull(err);
    }

    [Fact]
    public void Recharge_ValidTier_ReturnsTongbao()
    {
        var (code, tongbao, bonus) = ShopService.Recharge(1001, 1);
        Assert.Equal(0, code);
        Assert.Equal(60u, tongbao);
        Assert.Equal(6u, bonus);
    }

    [Fact]
    public void Recharge_InvalidTier_ReturnsError()
    {
        var (code, _, _) = ShopService.Recharge(1001, 999);
        Assert.Equal(1, code);
    }

    [Fact]
    public void UseGiftCode_ValidCode_Success()
    {
        var (code, msg) = ShopService.UseGiftCode(1001, "JX3WELCOME");
        Assert.Equal(0, code);
        Assert.NotNull(msg);
    }

    [Fact]
    public void UseGiftCode_InvalidCode_ReturnsError()
    {
        var (code, _) = ShopService.UseGiftCode(1001, "INVALID_CODE_123");
        Assert.Equal(1, code);
    }

    [Fact]
    public void MonthlyInfo_ReturnsDefault()
    {
        var (remainDays, claimedToday) = ShopService.GetMonthlyInfo(1001);
        Assert.Equal(0, remainDays);
        Assert.False(claimedToday);
    }

    [Fact]
    public void ClaimMonthly_ReturnsTrue()
    {
        Assert.True(ShopService.ClaimMonthly(1001));
    }
}
