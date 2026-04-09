using Crpg.Domain.Entities.BattleEvents;
using static Crpg.Domain.Entities.BattleEvents.CrpgGameEvent;

namespace Crpg.Application.Games.Models;

public record GameEventViewModel
{
    public int? UserId { get; init; }
    public EventType Type { get; init; }
    public Dictionary<EventField, string>? EventData { get; init; }
}
