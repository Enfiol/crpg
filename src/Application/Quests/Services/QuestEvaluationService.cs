using Crpg.Application.Common.Interfaces;
using Crpg.Domain.Entities.BattleEvents;
using Crpg.Domain.Entities.Quests;
using Microsoft.EntityFrameworkCore;

namespace Crpg.Application.Quests.Services;

public class QuestEvaluationService : IQuestEvaluationService
{
    private readonly ICrpgDbContext _db;

    public QuestEvaluationService(ICrpgDbContext db)
    {
        _db = db;
    }

    public async Task<int> ComputeCurrentValueAsync(UserQuest userQuest, CancellationToken cancellationToken = default)
    {
        var questDefinition = userQuest.QuestDefinition!;

        // Fetch events matching the basic criteria
        var events = await _db.BattleEvents
            .Where(be => be.UserId == userQuest.UserId
                         && be.Type == questDefinition.EventType
                         && be.CreatedAt >= userQuest.CreatedAt.Date) // trim time part
            .ToListAsync(cancellationToken);

        // Apply event filters in memory if any
        if (questDefinition.EventFiltersJson != null && questDefinition.EventFiltersJson.Length > 0)
        {
            events = events.Where(be => be.EventData != null
                                        && questDefinition.EventFiltersJson.Any(filter =>
                                            filter.All(kvp =>
                                            {
                                                if (!Enum.TryParse<CrpgGameEvent.EventField>(kvp.Key, out var field))
                                                {
                                                    return false;
                                                }

                                                return be.EventData!.TryGetValue(field, out string? value) &&
                                                       value == kvp.Value;
                                            }))).ToList();
        }

        switch (questDefinition.AggregationType)
        {
            case QuestAggregationType.Count:
                return events.Count;
            case QuestAggregationType.Sum when questDefinition.SumField != null:
                {
                    int sum = 0;
                    foreach (var ev in events)
                    {
                        if (ev.EventData != null &&
                            ev.EventData.TryGetValue(questDefinition.SumField.Value, out string? value) &&
                            int.TryParse(value, out int intValue))
                        {
                            sum += intValue;
                        }
                    }

                    return sum;
                }

            default:
                return 0;
        }
    }
}
