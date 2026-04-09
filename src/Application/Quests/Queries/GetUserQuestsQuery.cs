using AutoMapper;
using Crpg.Application.Common.Interfaces;
using Crpg.Application.Common.Mediator;
using Crpg.Application.Common.Results;
using Crpg.Application.Quests.Models;
using Crpg.Application.Quests.Services;
using Microsoft.EntityFrameworkCore;

namespace Crpg.Application.Quests.Queries;

public record GetUserQuestsQuery : IMediatorRequest<IList<UserQuestViewModel>>
{
    public int UserId { get; init; }

    internal class Handler(ICrpgDbContext db, IMapper mapper, IQuestEvaluationService questEvaluationService)
        : IMediatorRequestHandler<GetUserQuestsQuery, IList<UserQuestViewModel>>
    {
        public async ValueTask<Result<IList<UserQuestViewModel>>> Handle(GetUserQuestsQuery req,
            CancellationToken cancellationToken)
        {
            var userQuests = await db.UserQuests
                .Include(uq => uq.QuestDefinition)
                .Where(uq => uq.UserId == req.UserId)
                .ToListAsync(cancellationToken);

            var viewModels = new List<UserQuestViewModel>();
            foreach (var userQuest in userQuests)
            {
                int currentValue = await questEvaluationService.ComputeCurrentValueAsync(userQuest, cancellationToken);
                var vm = mapper.Map<UserQuestViewModel>(userQuest);
                currentValue = Math.Min(currentValue, userQuest.QuestDefinition!.RequiredValue);
                vm = vm with { CurrentValue = currentValue };
                viewModels.Add(vm);
            }

            return new(viewModels);
        }
    }
}
