using Crpg.Application.Quests.Commands;
using Mediator;

namespace Crpg.WebApi.Workers;

public class QuestAssignmentWorker : BackgroundService
{
    private static readonly ILogger Logger = Logging.LoggerFactory.CreateLogger<QuestAssignmentWorker>();

    private readonly IServiceScopeFactory _serviceScopeFactory;

    public QuestAssignmentWorker(IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await AssignQuestsAsync(stoppingToken);

            var now = DateTime.UtcNow;
            var nextRun = now.Date.AddDays(1); // Next midnight UTC
            var delay = nextRun - now;

            Logger.LogInformation("Next quest assignment scheduled at {NextRun} (in {Delay})", nextRun, delay);
            await Task.Delay(delay, stoppingToken);
        }
    }

    private async Task AssignQuestsAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            Logger.LogInformation("Assigning daily quests to all users");
            await mediator.Send(new AssignDailyQuestsToAllUsersCommand(), cancellationToken);
            Logger.LogInformation("Daily quests assigned");


            Logger.LogInformation("Assigning weekly quests to all users");
            await mediator.Send(new AssignWeeklyQuestsToAllUsersCommand(), cancellationToken);
            Logger.LogInformation("Weekly quests assigned");
        }
        catch (Exception e)
        {
            Logger.LogError(e, "An error occurred while assigning quests");
        }
    }
}
