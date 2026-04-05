using Crpg.Domain.Entities.Quests;

namespace Crpg.Application.Common.Interfaces;

public interface IQuestSource
{
    Task<IEnumerable<QuestDefinition>> LoadQuests();
}
