using Xunit;
using Jx3.Quest;

namespace Jx3.Tests;

public class QuestServiceTests
{
    [Fact]
    public void QuestTemplates_TenQuests_Defined()
    {
        Assert.Equal(10, QuestService.QuestTemplates.Count);
    }

    [Fact]
    public void QuestTypes_AllTypes_Represented()
    {
        Assert.Contains(QuestService.QuestTemplates, q => q.QuestType == 0); // main
        Assert.Contains(QuestService.QuestTemplates, q => q.QuestType == 1); // side
        Assert.Contains(QuestService.QuestTemplates, q => q.QuestType == 2); // daily
        Assert.Contains(QuestService.QuestTemplates, q => q.QuestType == 3); // achievement
    }

    [Fact]
    public void MainQuests_ProgressiveMinLevel()
    {
        var mainQuests = QuestService.QuestTemplates.Where(q => q.QuestType == 0).OrderBy(q => q.QuestId).ToList();
        Assert.Equal(3, mainQuests.Count);
        Assert.True(mainQuests[0].MinLevel <= mainQuests[1].MinLevel);
        Assert.True(mainQuests[1].MinLevel <= mainQuests[2].MinLevel);
    }

    [Fact]
    public void DailyQuests_HaveReasonableTarget()
    {
        var dailies = QuestService.QuestTemplates.Where(q => q.QuestType == 2);
        foreach (var q in dailies)
        {
            Assert.True(q.Target >= 1);
            Assert.True(q.MinLevel >= 1);
        }
    }

    [Fact]
    public void AchievementQuests_HigherTargets()
    {
        var achievements = QuestService.QuestTemplates.Where(q => q.QuestType == 3).ToList();
        Assert.Equal(3, achievements.Count);
        Assert.Contains(achievements, q => q.Target >= 100);
    }

    [Fact]
    public void QuestNames_NotEmpty()
    {
        foreach (var q in QuestService.QuestTemplates)
        {
            Assert.False(string.IsNullOrWhiteSpace(q.Name));
            Assert.False(string.IsNullOrWhiteSpace(q.Description));
        }
    }

    [Fact]
    public void QuestIds_Unique()
    {
        var ids = QuestService.QuestTemplates.Select(q => q.QuestId).ToList();
        Assert.Equal(ids.Count, ids.Distinct().Count());
    }

    [Fact]
    public void MinLevel_FirstQuest_Is1()
    {
        Assert.Equal(1u, QuestService.QuestTemplates[0].MinLevel);
    }

    [Fact]
    public void DailySignIn_QuestExists()
    {
        Assert.Contains(QuestService.QuestTemplates, q => q.Name.Contains("签到") || q.QuestId == 2001);
    }
}
