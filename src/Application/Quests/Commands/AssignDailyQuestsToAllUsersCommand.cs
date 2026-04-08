using Crpg.Application.Common.Mediator;
using Crpg.Application.Common.Results;
using Crpg.Application.Quests.Services;
using Microsoft.Extensions.Logging;
using LoggerFactory = Crpg.Logging.LoggerFactory;

namespace Crpg.Application.Quests.Commands;

public record AssignDailyQuestsToAllUsersCommand : IMediatorRequest
{
    internal class Handler(IQuestAssignmentService questAssignmentService) : IMediatorRequestHandler<AssignDailyQuestsToAllUsersCommand>
    {
        private static readonly ILogger Logger = LoggerFactory.CreateLogger<AssignDailyQuestsToAllUsersCommand>();

        private readonly IQuestAssignmentService _questAssignmentService = questAssignmentService;

        public async ValueTask<Result> Handle(AssignDailyQuestsToAllUsersCommand req, CancellationToken cancellationToken)
        {
            Logger.LogInformation("Assigning daily quests to all users");
            await _questAssignmentService.AssignDailyQuestsToAllUsersAsync(cancellationToken);
            Logger.LogInformation("Daily quests assigned");
            return Result.NoErrors;
        }
    }
}
