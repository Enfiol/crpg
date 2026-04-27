namespace Crpg.Domain.Entities.Characters;

/// <summary>
/// Perks that can be selected for a character.
/// </summary>
public enum CharacterPerkType
{
    /// <summary>+30% headshot damage.</summary>
    Headhunter,
    /// <summary>+5% attack speed.</summary>
    Berserker,
    /// <summary>+10% ranged damage.</summary>
    Marksman,
    /// <summary>+10 HP.</summary>
    Tank,
    /// <summary>+3% movement speed.</summary>
    FleetFooted,
    /// <summary>+20% shield durability.</summary>
    ShieldExpert,
    /// <summary>-25% mounted ranged penalty.</summary>
    HorseArcher,
    /// <summary>+15% melee damage.</summary>
    Brusier,
    /// <summary>+10% reload speed.</summary>
    QuickHands,
    /// <summary>-15% armor weight penalty.</summary>
    Strong,
    /// <summary>+5% experience gain.</summary>
    Veteran,
    /// <summary>-20% headshot damage taken.</summary>
    Hardheaded,
    /// <summary>-20% projectile damage taken.</summary>
    Deflector,
    /// <summary>+10% armor rating on all equipped armor pieces.</summary>
    Armorer,
    /// <summary>Ignores 20% of the enemy's armor when dealing damage.</summary>
    ArmorPiercer,
    /// <summary>Deals up to +30% damage against lightly armored targets.</summary>
    Executioner,
    /// <summary>+25% damage to mounts.</summary>
    BeastSlayer,
    /// <summary>+25% shield damage.</summary>
    ShieldBreaker,
    /// <summary>10% chance to avoid all damage when hit.</summary>
    Fate,
    /// <summary>+50% ammo capacity for arrows, bolts, and bullets.</summary>
    QuiverMaster,
    /// <summary>Slowly regenerate health over time.</summary>
    Regeneration,
    /// <summary>Bonus gold per kill.</summary>
    BountyHunter,
}


