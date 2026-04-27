using Crpg.Module.Api.Models.Characters;

namespace Crpg.Module.Common;

/// <summary>
/// Numerical constants for perk effects.
/// </summary>
internal static class CrpgPerksConstants
{
    /// <summary>Headhunter: +30% headshot damage.</summary>
    public const float HeadhunterHeadshotDamageMultiplier = 1.30f;

    /// <summary>Berserker: +5% attack speed (melee only).</summary>
    public const float BerserkerAttackSpeedMultiplier = 1.05f;

    /// <summary>Marksman: +10% ranged damage.</summary>
    public const float MarksmanRangedDamageMultiplier = 1.10f;

    /// <summary>Tank: +10 HP.</summary>
    public const int TankBonusHealth = 10;

    /// <summary>FleetFooted: +5% movement speed.</summary>
    public const float FleetFootedSpeedMultiplier = 1.05f;

    /// <summary>ShieldExpert: -20% shield damage taken.</summary>
    public const float ShieldExpertShieldDamageMultiplier = 0.80f;

    /// <summary>HorseArcher: -25% mounted ranged penalty.</summary>
    public const float HorseArcherAccuracyMultiplier = 0.75f;

    /// <summary>Brusier: +8% melee damage.</summary>
    public const float BrusierMeleeDamageMultiplier = 1.08f;

    /// <summary>QuickHands: +10% reload speed.</summary>
    public const float QuickHandsReloadSpeedMultiplier = 1.10f;

    /// <summary>Strong: -15% armor weight penalty.</summary>
    public const float StrongEncumbranceMultiplier = 0.85f;

    /// <summary>Veteran: +5% experience gain.</summary>
    public const float VeteranExperienceMultiplier = 1.05f;

    /// <summary>Hardheaded: -20% headshot damage taken.</summary>
    public const float HardheadedHeadshotDamageMultiplier = 0.80f;

    /// <summary>Deflector: -20% projectile damage taken.</summary>
    public const float DeflectorProjectileDamageMultiplier = 0.80f;

    /// <summary>Armorer: +10% armor rating on all equipped armor pieces.</summary>
    public const float ArmorerArmorMultiplier = 1.10f;

    /// <summary>ArmorPiercer: ignores 20% of the enemy's armor when dealing damage.</summary>
    public const float ArmorPiercerArmorMultiplier = 0.80f;

    /// <summary>Executioner: max armor threshold for bonus calculation.</summary>
    public const float ExecutionerMaxArmorThreshold = 60f;

    /// <summary>Executioner: max bonus damage multiplier at 0 armor.</summary>
    public const float ExecutionerMaxDamageMultiplier = 1.30f;

    /// <summary>BeastSlayer: +25% damage to mounts.</summary>
    public const float BeastSlayerMountDamageMultiplier = 1.25f;

    /// <summary>ShieldBreaker: +25% shield damage.</summary>
    public const float ShieldBreakerShieldDamageMultiplier = 1.25f;

    /// <summary>Fate: 10% chance to avoid all damage when hit.</summary>
    public const float FateAvoidChance = 0.10f;

    /// <summary>QuiverMaster: +50% ammo capacity for arrows, bolts, and bullets.</summary>
    public const float QuiverMasterAmmoMultiplier = 1.50f;

    /// <summary>Regeneration: health restored per second.</summary>
    public const float RegenerationHealPerSecond = 2.0f;

    /// <summary>BountyHunter: bonus gold per kill.</summary>
    public const int BountyHunterGoldPerKill = 100;
}



