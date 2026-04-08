using Crpg.Domain.Common;
using Crpg.Domain.Entities.Users;

namespace Crpg.Domain.Entities.BattleEvents;

public class CrpgGameEvent : AuditableEntity
{
    public int Id { get; set; }
    public int? UserId { get; set; }
    public EventType Type { get; set; }
    public Dictionary<EventField, string>? EventData { get; set; }

    public User? User { get; set; }

    public enum EventType
    {
        Undefined,
        Hit,
        Kill,
        Block,
    }

    public enum EventField
    {
        Undefined,
        WeaponType,
        WeaponId,
        HitType,
        Damage,
        TargetType,
        BodyPart,
    }
}
