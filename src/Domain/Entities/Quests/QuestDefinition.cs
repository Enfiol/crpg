using Crpg.Domain.Common;
using Crpg.Domain.Entities.BattleEvents;

namespace Crpg.Domain.Entities.Quests;

public class QuestDefinition : AuditableEntity
{
    public int Id { get; set; }
    public QuestType Type { get; set; }
    public Dictionary<string, string>? Name { get; set; }
    public Dictionary<string, string>? Description { get; set; }
    public CrpgGameEvent.EventType EventType { get; set; }
    public Dictionary<string, string>[]? EventFiltersJson { get; set; }
    public QuestAggregationType AggregationType { get; set; }
    public CrpgGameEvent.EventField? SumField { get; set; }
    public int RequiredValue { get; set; }
    public int RewardGold { get; set; }
    public int RewardExperience { get; set; }
    public bool IsActive { get; set; }
}

public enum QuestAggregationType
{
    Count,
    Sum,
}
