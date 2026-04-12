using Crpg.Application.Common.Interfaces;
using Crpg.Domain.Entities.CrpgGameEvents;
using Crpg.Domain.Entities.Quests;
using Microsoft.EntityFrameworkCore;

namespace Crpg.Application.Quests.Services;

public class QuestEvaluationService(ICrpgDbContext db) : IQuestEvaluationService
{
    public async Task<Dictionary<int, int>> ComputeCurrentValuesAsync(List<UserQuest> userQuests,
        CancellationToken cancellationToken = default)
    {
        if (userQuests.Count == 0)
        {
            return new Dictionary<int, int>();
        }

        var results = new Dictionary<int, int>();

        HashSet<int> userIds = [.. userQuests.Select(uq => uq.UserId)];

        var eventTypes = userQuests.Select(q => q.QuestDefinition!.EventType).Distinct().ToList();
        var earliestDate = userQuests.Min(q => q.CreatedAt.Date);

        var events = await db.CrpgGameEvents
            .Where(be => userIds.Contains(be.UserId!.Value)
                         && eventTypes.Contains(be.Type)
                         && be.CreatedAt >= earliestDate)
            .ToListAsync(cancellationToken);

        foreach (var userQuest in userQuests)
        {
            var questDefinition = userQuest.QuestDefinition!;
            var questEvents = events
                .Where(be => be.Type == questDefinition.EventType
                             && be.CreatedAt >= userQuest.CreatedAt.Date)
                .ToList();

            // Apply event filters in memory if any
            if (questDefinition.EventFiltersJson != null && questDefinition.EventFiltersJson.Length > 0)
            {
                questEvents = questEvents.Where(be => be.EventData != null
                                                      && questDefinition.EventFiltersJson.Any(filter =>
                                                          filter.All(kvp =>
                                                          {
                                                              if (!Enum.TryParse<CrpgGameEvent.EventField>(kvp.Key,
                                                                      out var field))
                                                              {
                                                                  return false;
                                                              }

                                                              return be.EventData!.TryGetValue(field,
                                                                         out string? value) &&
                                                                     value == kvp.Value;
                                                          }))).ToList();
            }

            int value = questDefinition.AggregationType switch
            {
                QuestAggregationType.Count => questEvents.Count,
                QuestAggregationType.Sum when questDefinition.SumField != null =>
                    questEvents.Sum(ev =>
                    {
                        if (ev.EventData != null &&
                            ev.EventData.TryGetValue(questDefinition.SumField.Value, out string? strValue) &&
                            int.TryParse(strValue, out int intValue))
                        {
                            return intValue;
                        }

                        return 0;
                    }),
                _ => 0,
            };

            results[userQuest.Id] = value;
        }

        return results;
    }
}
