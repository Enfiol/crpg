namespace Crpg.Domain.Entities.CrpgGameEvents;

public class CrpgGameEvent
{
    public int? UserId { get; set; }
    public EventType Type { get; set; }
    public Dictionary<EventField, string>? EventData { get; set; }

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
        DamageType,
    }
}
