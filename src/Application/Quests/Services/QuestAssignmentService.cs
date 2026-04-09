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

    public QuestAssignmentService(ICrpgDbContext db,  Constants constants)
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
        // Delete expired weekly quests (same condition as daily - expired quests are deleted)
        await _db.UserQuests.Where(uq => uq.ExpiresAt <= DateTime.UtcNow.Date).ExecuteDeleteAsync(cancellationToken);

        // Delete expired weekly quest assignments
        await _db.WeeklyQuestAssignments.Where(wqa => wqa.ExpiresAt <= DateTime.UtcNow.Date).ExecuteDeleteAsync(cancellationToken);

        var userIds = await _db.Users
            .Where(u => u.Characters.Any())
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

        var availableWeeklyDefinitions = await _db.QuestDefinitions
            .Where(qd => qd.IsActive && qd.Type == QuestType.Weekly)
            .ToListAsync(cancellationToken);

        if (!availableWeeklyDefinitions.Any())
        {
            return; // No weekly quest definitions available
        }

        // Get current active weekly quest assignments (for this week)
        var currentWeeklyAssignments = await _db.WeeklyQuestAssignments
            .Where(wqa => wqa.ExpiresAt > DateTime.UtcNow)
            .Select(wqa => wqa.QuestDefinitionId)
            .ToListAsync(cancellationToken);

        List<int> selectedWeeklyQuestIds;
        if (currentWeeklyAssignments.Any())
        {
            // Use existing assignments for this week
            selectedWeeklyQuestIds = currentWeeklyAssignments.Take(_weeklyQuestsPerUser).ToList();
        }
        else
        {
            // Create new weekly assignments for this week
            selectedWeeklyQuestIds = availableWeeklyDefinitions
                .Shuffle()
                .Take(_weeklyQuestsPerUser)
                .Select(q => q.Id)
                .ToList();

            var now = DateTime.UtcNow;
            var expiresAt = now.Date.AddDays(7);
            foreach (var questId in selectedWeeklyQuestIds)
            {
                var assignment = new WeeklyQuestAssignment
                {
                    QuestDefinitionId = questId,
                    AssignedAt = now,
                    ExpiresAt = expiresAt,
                };
                _db.WeeklyQuestAssignments.Add(assignment);
            }
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

            foreach (var questDefinitionId in questsToAssign)
            {
                var userQuest = new UserQuest
                {
                    UserId = userId,
                    QuestDefinitionId = questDefinitionId,
                    IsRewardClaimed = false,
                    ExpiresAt = DateTime.UtcNow.Date.AddDays(7), // Weekly quests expire in 7 days
                };
                _db.UserQuests.Add(userQuest);
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task AssignDailyQuestsToNewUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        // Delete any existing quests for the user (should be none, but just in case)
        await _db.UserQuests.Where(uq => uq.UserId == userId).ExecuteDeleteAsync(cancellationToken);

        // Assign daily quests
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

        // Assign weekly quests - use the current weekly quest assignments for this week
        var currentWeeklyAssignments = await _db.WeeklyQuestAssignments
            .Where(wqa => wqa.ExpiresAt > DateTime.UtcNow)
            .Select(wqa => wqa.QuestDefinitionId)
            .Take(_weeklyQuestsPerUser)
            .ToListAsync(cancellationToken);

        if (currentWeeklyAssignments.Any())
        {
            // Take up to the configured number of weekly quests per user
            foreach (var questDefinitionId in currentWeeklyAssignments)
            {
                var userQuest = new UserQuest
                {
                    UserId = userId,
                    QuestDefinitionId = questDefinitionId,
                    IsRewardClaimed = false,
                    ExpiresAt = DateTime.UtcNow.Date.AddDays(7),
                };
                _db.UserQuests.Add(userQuest);
            }
        }
        else
        {
            // If no active weekly assignments exist (e.g., before Monday assignment), fall back to random selection
            var availableWeeklyDefinitions = await _db.QuestDefinitions
                .Where(qd => qd.IsActive && qd.Type == QuestType.Weekly)
                .ToListAsync(cancellationToken);

            if (availableWeeklyDefinitions.Any())
            {
                var selectedWeeklyQuests = availableWeeklyDefinitions.Shuffle().Take(_weeklyQuestsPerUser).ToList();
                foreach (var definition in selectedWeeklyQuests)
                {
                    var userQuest = new UserQuest
                    {
                        UserId = userId,
                        QuestDefinitionId = definition.Id,
                        IsRewardClaimed = false,
                        ExpiresAt = DateTime.UtcNow.Date.AddDays(7),
                    };
                    _db.UserQuests.Add(userQuest);
                }
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task AssignWeeklyQuestsToNewUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        // Delete any existing weekly quests for the user
        await _db.UserQuests
            .Where(uq => uq.UserId == userId && uq.QuestDefinition!.Type == QuestType.Weekly)
            .ExecuteDeleteAsync(cancellationToken);

        // Use the current weekly quest assignments for this week
        var currentWeeklyAssignments = await _db.WeeklyQuestAssignments
            .Where(wqa => wqa.ExpiresAt > DateTime.UtcNow)
            .Select(wqa => wqa.QuestDefinitionId)
            .Take(_weeklyQuestsPerUser)
            .ToListAsync(cancellationToken);

        if (currentWeeklyAssignments.Any())
        {
            // Take up to the configured number of weekly quests per user
            foreach (var questDefinitionId in currentWeeklyAssignments)
            {
                var userQuest = new UserQuest
                {
                    UserId = userId,
                    QuestDefinitionId = questDefinitionId,
                    IsRewardClaimed = false,
                    ExpiresAt = DateTime.UtcNow.Date.AddDays(7),
                };
                _db.UserQuests.Add(userQuest);
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<UserQuest> ReplaceDailyUserQuestAsync(UserQuest userQuest, CancellationToken cancellationToken = default)
    {
        var questDefinitions = await _db.QuestDefinitions
            .Where(qd => qd.IsActive && userQuest.QuestDefinition!.Id != qd.Id)
            .ToListAsync(cancellationToken);

        var randomQuestDefinition = questDefinitions.Shuffle().FirstOrDefault() ?? throw new Exception("No quest definition found");

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
}
