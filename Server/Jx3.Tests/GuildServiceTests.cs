using Xunit;
using Jx3.Social;

namespace Jx3.Tests;

public class GuildServiceTests
{
    [Fact]
    public void CreateGuild_NewName_Success()
    {
        var code = GuildService.CreateGuild(101, "TestPlayer", "test_uniq_1");
        Assert.Equal(0, code);
    }

    [Fact]
    public void CreateGuild_DuplicateName_Fails()
    {
        GuildService.CreateGuild(201, "P1", "dup_name_guild");
        var code = GuildService.CreateGuild(202, "P2", "dup_name_guild");
        Assert.Equal(1, code);
    }

    [Fact]
    public void JoinLeaveGuild_RoundTrip()
    {
        GuildService.CreateGuild(301, "Leader", "join_test_guild");
        var guild = GuildService.FindPlayerGuild(301);
        Assert.NotNull(guild);
        var joinCode = GuildService.JoinGuild(guild.GuildId, 302, "NewMember");
        Assert.Equal(0, joinCode);
        var left = GuildService.LeaveGuild(302);
        Assert.True(left);
    }

    [Fact]
    public void FindPlayerGuild_NotInGuild_ReturnsNull()
    {
        var guild = GuildService.FindPlayerGuild(99901);
        Assert.Null(guild);
    }
}