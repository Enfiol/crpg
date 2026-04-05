using Crpg.Domain.Entities.Quests;

namespace Crpg.Application.Quests.Services;

public interface IQuestEvaluationService
{
    Task<int> ComputeCurrentValueAsync(UserQuest userQuest, CancellationToken cancellationToken = default);
}
