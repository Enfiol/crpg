using Crpg.Application.Common.Interfaces;
using Crpg.Domain.Entities.Quests;
using Microsoft.EntityFrameworkCore;

namespace Crpg.Application.Quests.Services;

public class QuestAssignmentService(ICrpgDbContext db) : IQuestAssignmentService
{
    private const int DailyQuestsPerUser = 3; // todo move to config
    private readonly ICrpgDbContext _db = db;

    public async Task AssignDailyQuestsToAllUsersAsync(CancellationToken cancellationToken = default)
    {
        await _db.UserQuests.Where(uq => uq.ExpiresAt <= DateTime.UtcNow.Date).ExecuteDeleteAsync(cancellationToken);

        var userIds = await _db.Users
            .Where(u => u.Characters.Any())
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

        var availableDefinitions = await _db.QuestDefinitions
            .Where(qd => qd.IsActive)
            .ToListAsync(cancellationToken);

        var userActiveQuestsCount = await _db.UserQuests.GroupBy(x => x.UserId)
            .ToDictionaryAsync(key => key.Key, value => value.Count(), cancellationToken: cancellationToken);

        foreach (int userId in userIds)
        {
            int questsToAddCount = DailyQuestsPerUser - userActiveQuestsCount.GetValueOrDefault(userId);

            if (questsToAddCount == 0)
            {
                continue;
            }

            var selectedQuest = availableDefinitions.Shuffle().Take(questsToAddCount);

            foreach (var definition in selectedQuest)
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

    public async Task AssignDailyQuestsToNewUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        var availableDefinitions = await _db.QuestDefinitions
            .Where(qd => qd.IsActive)
            .ToListAsync(cancellationToken);

        await _db.UserQuests.Where(uq => uq.UserId == userId).ExecuteDeleteAsync(cancellationToken);

        var selectedQuests = availableDefinitions.Shuffle().Take(DailyQuestsPerUser);

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
