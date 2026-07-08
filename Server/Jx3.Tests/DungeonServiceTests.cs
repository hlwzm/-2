using Xunit;
using Jx3.Dungeon;

namespace Jx3.Tests;

public class DungeonServiceTests
{
    [Fact]
    public void DungeonConfig_AllFourDungeons_Defined()
    {
        var list = DungeonService.GetDungeonList();
        Assert.Equal(4, list.Count);
    }

    [Fact]
    public void EnterDungeon_ValidParams_ReturnsSuccess()
    {
        var r = DungeonService.EnterDungeon(1, 1, 1001, new List<ulong> { 1, 2, 3, 4 });
        Assert.Equal(0, r.code);
        Assert.NotNull(r.inst);
    }

    [Fact]
    public void EnterDungeon_InvalidId_ReturnsError()
    {
        var r = DungeonService.EnterDungeon(99, 1, 1002, new List<ulong> { 1, 2, 3, 4 });
        Assert.Equal(1, r.code);
        Assert.Null(r.inst);
    }

    [Fact]
    public void EnterDungeon_TooFewPlayers_ReturnsError()
    {
        var r = DungeonService.EnterDungeon(1, 1, 1003, new List<ulong> { 1, 2 });
        Assert.NotEqual(0, r.code);
    }

    [Fact]
    public void GetDungeonList_ValidMinLevels()
    {
        var list = DungeonService.GetDungeonList();
        Assert.True(list[0].MinLevel > 0);
        Assert.True(list[3].MinLevel >= list[0].MinLevel);
    }
}