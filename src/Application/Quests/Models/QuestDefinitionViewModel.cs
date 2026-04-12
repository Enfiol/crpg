using System.Text.Json.Serialization;
using Crpg.Application.Common.Mappings;
using Crpg.Domain.Entities.Quests;
using static Crpg.Domain.Entities.CrpgGameEvents.CrpgGameEvent;

namespace Crpg.Application.Quests.Models;

public record QuestDefinitionViewModel : IMapFrom<QuestDefinition>
{
    public int Id { get; init; }
    public QuestType Type { get; set; }
    public EventType EventType { get; init; }
    public QuestAggregationType AggregationType { get; init; }

    [JsonRequired]
    public EventField SumField { get; init; }

    [JsonRequired]
    public Dictionary<string, string>[] EventFiltersJson { get; set; } = [];

    public int RequiredValue { get; init; }
    public int RewardGold { get; init; }
    public int RewardExperience { get; init; }
}
