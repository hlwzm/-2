using Xunit;
using Jx3.Chat;

namespace Jx3.Tests;

public class ChatServiceTests
{
    [Fact]
    public void FilterSensitive_NoSensitiveWords_Unchanged()
    {
        var result = ChatService.FilterSensitive("hello world");
        Assert.Equal("hello world", result);
    }

    [Fact]
    public void FilterSensitive_ReplacesWords()
    {
        var result = ChatService.FilterSensitive("this is shit content");
        Assert.Contains("****", result);
        Assert.DoesNotContain("shit", result);
    }

    [Fact]
    public void FilterSensitive_MultipleWords()
    {
        var result = ChatService.FilterSensitive("fuck and shit");
        Assert.Contains("****", result);
        Assert.DoesNotContain("fuck", result);
    }

    [Fact]
    public void IsValidChannel_World_ReturnsTrue()
    {
        Assert.True(ChatService.IsValidChannel(0));
    }

    [Fact]
    public void IsValidChannel_System_ReturnsTrue()
    {
        Assert.True(ChatService.IsValidChannel(5));
    }

    [Fact]
    public void IsValidChannel_Negative_ReturnsFalse()
    {
        Assert.False(ChatService.IsValidChannel(-1));
    }

    [Fact]
    public void IsValidChannel_TooHigh_ReturnsFalse()
    {
        Assert.False(ChatService.IsValidChannel(6));
    }

    [Fact]
    public void IsValidChannel_Team_ReturnsTrue()
    {
        Assert.True(ChatService.IsValidChannel(2));
    }

    [Fact]
    public void GetChannelName_AllChannels_Defined()
    {
        Assert.Equal("世界", ChatService.GetChannelName(0));
        Assert.Equal("当前", ChatService.GetChannelName(1));
        Assert.Equal("队伍", ChatService.GetChannelName(2));
        Assert.Equal("同盟", ChatService.GetChannelName(3));
        Assert.Equal("门派", ChatService.GetChannelName(4));
        Assert.Equal("系统", ChatService.GetChannelName(5));
        Assert.Equal("未知", ChatService.GetChannelName(99));
    }
}
