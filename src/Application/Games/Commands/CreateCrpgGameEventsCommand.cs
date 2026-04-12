using Crpg.Application.Common.Interfaces;
using Crpg.Application.Common.Mediator;
using Crpg.Application.Common.Results;
using Crpg.Application.Games.Models;
using Crpg.Application.Quests.Services;
using Crpg.Domain.Entities.CrpgGameEvents;
using Microsoft.Extensions.Logging;
using LoggerFactory = Crpg.Logging.LoggerFactory;

namespace Crpg.Application.Games.Commands;

public record CreateCrpgGameEventsCommand : IMediatorRequest
{
    public IList<GameEventViewModel> CrpgGameEvents { get; init; } = Array.Empty<GameEventViewModel>();

    internal class Handler : IMediatorRequestHandler<CreateCrpgGameEventsCommand>
    {
        private static readonly ILogger Logger = LoggerFactory.CreateLogger<CreateCrpgGameEventsCommand>();

        private readonly ICrpgDbContext _db;

        public Handler(ICrpgDbContext db)
        {
            _db = db;
        }

        public async ValueTask<Result> Handle(CreateCrpgGameEventsCommand req, CancellationToken cancellationToken)
        {
            var crpgGameEvents = req.CrpgGameEvents
                .Select(e => new CrpgGameEvent
                {
                    UserId = e.UserId,
                    Type = e.Type,
                    EventData = e.EventData,
                }).ToList();

            _db.CrpgGameEvents.AddRange(crpgGameEvents);
            await _db.SaveChangesAsync(cancellationToken);

            Logger.LogInformation("Inserted {0} crpg game events", crpgGameEvents.Count);
            return Result.NoErrors;
        }
    }
}
