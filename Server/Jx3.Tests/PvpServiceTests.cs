using Xunit;
using Jx3.PVP;

namespace Jx3.Tests;

public class PvpServiceTests
{
    [Fact]
    public void JoinQueue_AddsPlayer_Success()
    {
        var code = PvpService.JoinQueue(1, "P1");
        Assert.Equal(0, code);
        PvpService.LeaveQueue(1);
    }

    [Fact]
    public void JoinQueue_Duplicate_ReturnsError()
    {
        PvpService.JoinQueue(100, "P100");
        var code = PvpService.JoinQueue(100, "P100");
        Assert.Equal(1, code);
        PvpService.LeaveQueue(100);
    }

    [Fact]
    public void LeaveQueue_RemovesPlayer()
    {
        PvpService.JoinQueue(200, "P200");
        var result = PvpService.LeaveQueue(200);
        Assert.True(result);
    }

    [Fact]
    public void TryMatch_TwoPlayers_ReturnsMatch()
    {
        PvpService.JoinQueue(301, "A");
        PvpService.JoinQueue(302, "B");
        var (match, a, b) = PvpService.TryMatch();
        Assert.NotNull(match);
        Assert.NotNull(a);
        Assert.NotNull(b);
        PvpService.LeaveQueue(301);
        PvpService.LeaveQueue(302);
    }

    [Fact]
    public void EndMatch_WinnerGainsRating()
    {
        PvpService.JoinQueue(401, "P401");
        PvpService.LeaveQueue(401);
        PvpService.JoinQueue(402, "P402");
        PvpService.LeaveQueue(402);
        var match = new MatchResult { MatchId = 1, TeamA = new() { 401 }, TeamB = new() { 402 }, StartTime = new System.DateTime(2025, 1, 1, 0, 0, 0, System.DateTimeKind.Utc) };
        var (chA, chB) = PvpService.EndMatch(match, 1);
        Assert.True(chA > 0);
        Assert.True(chB < 0);
    }

    [Fact]
    public void TierNames_AllDefined()
    {
        Assert.Equal(7, PvpService.TierNames.Length);
        Assert.Equal("青铜", PvpService.TierNames[0]);
        Assert.Equal("传说", PvpService.TierNames[6]);
    }

    [Fact]
    public void GetPlayer_ReturnsCorrectPlayer()
    {
        PvpService.JoinQueue(501, "P501");
        PvpService.LeaveQueue(501);
        var player = PvpService.GetPlayer(501);
        Assert.NotNull(player);
        Assert.Equal("P501", player.Name);
    }
}