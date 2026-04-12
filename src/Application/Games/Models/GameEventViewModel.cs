using Crpg.Domain.Entities.CrpgGameEvents;
using static Crpg.Domain.Entities.CrpgGameEvents.CrpgGameEvent;

namespace Crpg.Application.Games.Models;

public record GameEventViewModel
{
    public int? UserId { get; init; }
    public EventType Type { get; init; }
    public Dictionary<EventField, string>? EventData { get; init; }
}
