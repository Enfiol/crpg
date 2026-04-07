using Crpg.Application.Common.Interfaces;
using Crpg.Application.Common.Mediator;
using Crpg.Application.Common.Results;
using Crpg.Application.Games.Models;
using Crpg.Application.Quests.Services;
using Crpg.Domain.Entities.BattleEvents;
using Microsoft.Extensions.Logging;
using LoggerFactory = Crpg.Logging.LoggerFactory;

namespace Crpg.Application.Games.Commands;

public record CreateBattleEventsCommand : IMediatorRequest
{
    public IList<GameEventViewModel> BattleEvents { get; init; } = Array.Empty<GameEventViewModel>();

    internal class Handler : IMediatorRequestHandler<CreateBattleEventsCommand>
    {
        private static readonly ILogger Logger = LoggerFactory.CreateLogger<CreateBattleEventsCommand>();

        private readonly ICrpgDbContext _db;

        public Handler(ICrpgDbContext db)
        {
            _db = db;
        }

        public async ValueTask<Result> Handle(CreateBattleEventsCommand req, CancellationToken cancellationToken)
        {
            var battleEvents = req.BattleEvents
                .Select(e => new CrpgGameEvent
                {
                    UserId = e.UserId,
                    Type = e.Type,
                    EventData = e.EventData,
                }).ToList();

            _db.BattleEvents.AddRange(battleEvents);
            await _db.SaveChangesAsync(cancellationToken);

            Logger.LogInformation("Inserted {0} battle events", battleEvents.Count);
            return Result.NoErrors;
        }
    }
}
