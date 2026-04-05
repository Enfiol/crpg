using Crpg.Application.Common.Mappings;
using Crpg.Domain.Entities.Quests;

namespace Crpg.Application.Quests.Models;

public record QuestDefinitionViewModel : IMapFrom<QuestDefinition>
{
    public int Id { get; init; }
    public Dictionary<string, string>? Name { get; init; }
    public Dictionary<string, string>? Description { get; init; }
    public int RequiredValue { get; init; }
    public int RewardGold { get; init; }
    public int RewardExperience { get; init; }
}
