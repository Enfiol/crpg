using Crpg.Application.Common;
using Crpg.Application.Common.Interfaces;
using Crpg.Domain.Entities.Quests;
using Microsoft.EntityFrameworkCore;

namespace Crpg.Application.Quests.Services;

public class QuestAssignmentService : IQuestAssignmentService
{
    private readonly int _dailyQuestsPerUser;
    private readonly int _weeklyQuestsPerUser;
    private readonly ICrpgDbContext _db;

    public QuestAssignmentService(ICrpgDbContext db, Constants constants)
    {
        _dailyQuestsPerUser = constants.QuestDailyQuestsPerUser;
        _weeklyQuestsPerUser = constants.QuestWeeklyQuestsPerUser;
        _db = db;
    }



    public async Task AssignDailyQuestsToAllUsersAsync(CancellationToken cancellationToken = default)
    {
        await _db.UserQuests.Where(uq => uq.ExpiresAt <= DateTime.UtcNow.Date).ExecuteDeleteAsync(cancellationToken);

        var userIds = await _db.Users
            .Where(u => u.Characters.Any())
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

        var availableDefinitions = await _db.QuestDefinitions
            .Where(qd => qd.IsActive && qd.Type == QuestType.Daily)
            .ToListAsync(cancellationToken);

        var userActiveQuestsCount = await _db.UserQuests
            .Include(uq => uq.QuestDefinition)
            .Where(uq => uq.QuestDefinition!.Type == QuestType.Daily)
            .GroupBy(x => x.UserId)
            .ToDictionaryAsync(key => key.Key, value => value.Count(), cancellationToken: cancellationToken);

        foreach (int userId in userIds)
        {
            int questsToAddCount = _dailyQuestsPerUser - userActiveQuestsCount.GetValueOrDefault(userId);

            if (questsToAddCount <= 0)
            {
                continue;
            }

            var selectedQuests = availableDefinitions.Shuffle().Take(questsToAddCount);

            foreach (var definition in selectedQuests)
            {
                var userQuest = new UserQuest
                {
                    UserId = userId,
                    QuestDefinitionId = definition.Id,
                    IsRewardClaimed = false,
                    ExpiresAt = DateTime.UtcNow.Date.AddDays(1),
                };
                _db.UserQuests.Add(userQuest);
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task AssignWeeklyQuestsToAllUsersAsync(CancellationToken cancellationToken = default)
    {
        await _db.UserQuests.Where(uq => uq.ExpiresAt <= DateTime.UtcNow.Date).ExecuteDeleteAsync(cancellationToken);
        await _db.WeeklyQuestAssignments.Where(wqa => wqa.ExpiresAt <= DateTime.UtcNow.Date)
            .ExecuteDeleteAsync(cancellationToken);

        var userIds = await _db.Users
            .Where(u => u.Characters.Any())
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

        var availableWeeklyDefinitions = await _db.QuestDefinitions
            .Where(qd => qd.IsActive && qd.Type == QuestType.Weekly)
            .ToListAsync(cancellationToken);

        if (availableWeeklyDefinitions.Count == 0)
        {
            return;
        }

        var currentWeeklyAssignments = await _db.WeeklyQuestAssignments
            .Where(wqa => wqa.ExpiresAt > DateTime.UtcNow)
            .Select(wqa => wqa.QuestDefinitionId)
            .ToListAsync(cancellationToken);

        List<int> selectedWeeklyQuestIds;
        var now = DateTime.UtcNow;
        var expiresAt = NextMonday(now.Date);
        if (currentWeeklyAssignments.Count == 0)
        {
            // Create new weekly assignments for this week
            selectedWeeklyQuestIds = availableWeeklyDefinitions
                .Shuffle()
                .Take(_weeklyQuestsPerUser)
                .Select(q => q.Id)
                .ToList();


            foreach (int questId in selectedWeeklyQuestIds)
            {
                var assignment = new WeeklyQuestAssignment
                {
                    QuestDefinitionId = questId, AssignedAt = now, ExpiresAt = expiresAt,
                };
                _db.WeeklyQuestAssignments.Add(assignment);
            }
        }
        else
        {
            // Use existing assignments for this week
            selectedWeeklyQuestIds = currentWeeklyAssignments;
        }

        // Get existing weekly quests per user to avoid duplicates
        var userWeeklyQuests = await _db.UserQuests
            .Include(uq => uq.QuestDefinition)
            .Where(uq => uq.QuestDefinition!.Type == QuestType.Weekly)
            .GroupBy(uq => uq.UserId)
            .ToDictionaryAsync(g => g.Key, g => g.Select(uq => uq.QuestDefinitionId).ToHashSet(), cancellationToken);

        foreach (int userId in userIds)
        {
            var existingWeeklyQuestIds = userWeeklyQuests.GetValueOrDefault(userId, new HashSet<int>());
            int questsToAddCount = _weeklyQuestsPerUser - existingWeeklyQuestIds.Count;

            if (questsToAddCount <= 0)
            {
                continue;
            }

            // Take quests that the user doesn't already have from the selected weekly quests
            var questsToAssign = selectedWeeklyQuestIds
                .Where(q => !existingWeeklyQuestIds.Contains(q))
                .Take(questsToAddCount);

            foreach (int questDefinitionId in questsToAssign)
            {
                var userQuest = new UserQuest
                {
                    UserId = userId,
                    QuestDefinitionId = questDefinitionId,
                    IsRewardClaimed = false,
                    ExpiresAt = expiresAt,
                };
                _db.UserQuests.Add(userQuest);
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task AssignQuestsToNewUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        // Daily
        await _db.UserQuests.Where(uq => uq.UserId == userId).ExecuteDeleteAsync(cancellationToken);

        var availableDailyDefinitions = await _db.QuestDefinitions
            .Where(qd => qd.IsActive && qd.Type == QuestType.Daily)
            .ToListAsync(cancellationToken);

        var selectedDailyQuests = availableDailyDefinitions.Shuffle().Take(_dailyQuestsPerUser);

        foreach (var definition in selectedDailyQuests)
        {
            var userQuest = new UserQuest
            {
                UserId = userId,
                QuestDefinitionId = definition.Id,
                IsRewardClaimed = false,
                ExpiresAt = DateTime.UtcNow.Date.AddDays(1),
            };
            _db.UserQuests.Add(userQuest);
        }

        // Weekly
        var currentWeeklyAssignments = await _db.WeeklyQuestAssignments
            .Where(wqa => wqa.ExpiresAt > DateTime.UtcNow)
            .Select(wqa => wqa.QuestDefinitionId)
            .Take(_weeklyQuestsPerUser)
            .ToListAsync(cancellationToken);

        if (currentWeeklyAssignments.Count != 0)
        {
            foreach (int questDefinitionId in currentWeeklyAssignments)
            {
                var userQuest = new UserQuest
                {
                    UserId = userId,
                    QuestDefinitionId = questDefinitionId,
                    IsRewardClaimed = false,
                    ExpiresAt = NextMonday(DateTime.UtcNow.Date),
                };
                _db.UserQuests.Add(userQuest);
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<UserQuest> ReplaceDailyUserQuestAsync(UserQuest userQuest,
        CancellationToken cancellationToken = default)
    {
        var questDefinitions = await _db.QuestDefinitions
            .Where(qd => qd.IsActive && userQuest.QuestDefinition!.Id != qd.Id)
            .ToListAsync(cancellationToken);

        var randomQuestDefinition = questDefinitions.Shuffle().Single();

        var newUserQuest = new UserQuest
        {
            UserId = userQuest.UserId,
            QuestDefinitionId = randomQuestDefinition!.Id,
            IsRewardClaimed = false,
            ExpiresAt = userQuest.ExpiresAt,
        };

        _db.UserQuests.Remove(userQuest);
        _db.UserQuests.Add(newUserQuest);
        await _db.SaveChangesAsync(cancellationToken);

        return newUserQuest;
    }

    private static DateTime NextMonday(DateTime date)
    {
        int daysUntilMonday = ((int)DayOfWeek.Monday - (int)date.DayOfWeek + 7) % 7;
        return date.AddDays(daysUntilMonday == 0 ? 7 : daysUntilMonday);
    }
}
