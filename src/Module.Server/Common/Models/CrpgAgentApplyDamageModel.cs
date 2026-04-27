using Crpg.Module.Api.Models.Characters;
using Crpg.Module.Helpers;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace Crpg.Module.Common.Models;

/// <summary>
/// Used to adjust dmg calculations.
/// </summary>
internal class CrpgAgentApplyDamageModel : MultiplayerAgentApplyDamageModel
{
    private readonly CrpgConstants _constants;

    public CrpgAgentApplyDamageModel(CrpgConstants constants)
    {
        _constants = constants;
    }

    public override float ApplyGeneralDamageModifiers(
        in AttackInformation attackInformation,
        in AttackCollisionData collisionData,
        float baseDamage)
    {
        List<WeaponClass> meleeClass = new()
        {
            WeaponClass.Dagger,
            WeaponClass.Mace,
            WeaponClass.TwoHandedMace,
            WeaponClass.OneHandedSword,
            WeaponClass.TwoHandedSword,
            WeaponClass.OneHandedAxe,
            WeaponClass.TwoHandedAxe,
            WeaponClass.Pick,
            WeaponClass.LowGripPolearm,
            WeaponClass.OneHandedPolearm,
            WeaponClass.TwoHandedPolearm,
        };
        List<WeaponClass> swordClass = new()
        {
            WeaponClass.Dagger,
            WeaponClass.OneHandedSword,
            WeaponClass.TwoHandedSword,
        };
        List<WeaponClass> axeClass = new()
        {
            WeaponClass.OneHandedAxe,
            WeaponClass.TwoHandedAxe,
        };
        float finalDamage = base.ApplyGeneralDamageModifiers(attackInformation, collisionData, baseDamage);

        if (IsPlayerCharacterAttackingVipBot(attackInformation))
        {
            return 0f;
        }

        if (attackInformation.AttackerWeapon.IsEmpty)
        {
            // Increase fist damage with strength and glove armor.
            int strengthSkill = GetSkillValue(attackInformation.AttackerAgentOrigin, CrpgSkills.Strength);
            int glovearmor = GetGloveArmor(attackInformation.AttackerAgentOrigin);
            if (collisionData.IsAlternativeAttack) // Kick
            {
                return finalDamage * 0.75f * (1 + 0.02f * strengthSkill);
            }

            return finalDamage * 0.75f * (1 + 0.02f * strengthSkill + 0.04f * glovearmor);
        }

        if (IsPlayerCharacterAttackingDtvBot(attackInformation))
        {
            switch (attackInformation.AttackerWeapon.CurrentUsageItem.WeaponClass)
            {
                case WeaponClass.Bolt:
                case WeaponClass.Cartridge:
                    finalDamage *= 2.5f;
                    break;
                case WeaponClass.Arrow:
                    finalDamage *= 1.8f;
                    break;
                case WeaponClass.Javelin:
                case WeaponClass.ThrowingAxe:
                case WeaponClass.ThrowingKnife:
                case WeaponClass.Stone:
                    finalDamage *= 2.6f;
                    break;
            }
        }

        if (IsPlayerCharacterAttackingDtvBoss(attackInformation))
        {
            if (attackInformation.AttackerWeapon.Item.StringId.Contains("ballista_projectile"))
            {
                finalDamage *= 0.05f;
            }
        }

        // CalculateShieldDamage only has dmg as parameter. Therefore it cannot be used to get any Skill values.
        if (collisionData.AttackBlockedWithShield && finalDamage > 0)
        {
            int shieldSkill = GetSkillValue(attackInformation.VictimAgentOrigin, CrpgSkills.Shield);
            finalDamage /= MathHelper.RecursivePolynomialFunctionOfDegree2(shieldSkill, _constants.DurabilityFactorForShieldRecursiveCoefs);
            if (meleeClass.Contains(attackInformation.AttackerWeapon.CurrentUsageItem.WeaponClass))
            {
                // in bannerlord/Src/TaleWorlds.MountAndBlade/MissionCombatMechanicsHelper.cs/GetAttackCollisionResults()
                // ComputeBlowDamageOnShield is fed the basemagnitude from ComputeBlowMagnitude() instead of the specialmagnitude
                // specialmagnitude takes in account the damagefactor which is the bladesharpness.
                //  specialmagnitude is the damage you deal to agents , while basemagnitude is the blow from strikemagnitudemodel
                //  basemagnitude only takes in account both sweetspots and speedbonus , but not the damage multiplicator that each weapon have
                finalDamage *=
                    collisionData.StrikeType == (int)StrikeType.Thrust
                        ? attackInformation.AttackerWeapon.CurrentUsageItem.ThrustDamageFactor
                        : attackInformation.AttackerWeapon.CurrentUsageItem.SwingDamageFactor;

                if (attackInformation.AttackerWeapon.CurrentUsageItem.WeaponFlags.HasAnyFlag(WeaponFlags.BonusAgainstShield))
                {
                    // this bonus is on top of the native x2 in MissionCombatMechanicsHelper
                    // so the final bonus is 5.0 for one- and two- handed axes and 3.5 for everything else. We do this instead of nerfing the impact of shield skill so shield can stay virtually unbreakable against sword.
                    // it is the same logic as arrows not dealing a lot of damage to horse but spears dealing extra damage to horses
                    // As we want archer to fear cavs and cavs to fear spears, we want swords to fear shielders and shielders to fear axes.

                    finalDamage *= axeClass.Contains(attackInformation.AttackerWeapon.CurrentUsageItem.WeaponClass) ? 2.5f : 1.75f;
                }
            }
        }

        // Horse HP and eHP is currently good. To adjust their performance, adjust global melee damage and global non-mounted ranged damage. Mounted ranged damage is not increase to help cavalry attack HA
        if (!attackInformation.IsVictimAgentHuman && attackInformation.AttackerWeapon.CurrentUsageItem.IsMeleeWeapon)
        {
            finalDamage *= 1.4f;
        }

        if (!attackInformation.IsVictimAgentHuman
            && attackInformation.AttackerWeapon.CurrentUsageItem.IsRangedWeapon
            && !attackInformation.DoesAttackerHaveMountAgent)
        {
            finalDamage *= 1.3f;
        }

        // For bashes (with and without shield) - Not for allies cause teamdmg might reduce the "finalDamage" below zero. That will break teamhits with bashes.
        else if (collisionData.IsAlternativeAttack && !attackInformation.IsFriendlyFire)
        {
            finalDamage = 1f;
        }

        if (attackInformation.DoesAttackerHaveMountAgent && attackInformation.IsAttackerAgentDoingPassiveAttack)
        {
            finalDamage *= 0.23f; // Decrease damage from couched lance.
        }

        if (IsSwashbuckler(attackInformation.AttackerWeapon, collisionData, attackInformation.AttackerAgent))
        {
            finalDamage *= 1.10f;
        }

        // Perk effects
        if (attackInformation.AttackerAgent != null)
        {
            // Headhunter: +30% headshot damage
            if (CrpgPerksApplicationComponent.HasPerk(attackInformation.AttackerAgent, CrpgCharacterPerkType.Headhunter)
                && (collisionData.VictimHitBodyPart == BoneBodyPartType.Head || collisionData.VictimHitBodyPart == BoneBodyPartType.Neck))
            {
                finalDamage *= CrpgPerksConstants.HeadhunterHeadshotDamageMultiplier;
            }

            // Marksman: +10% ranged damage
            if (CrpgPerksApplicationComponent.HasPerk(attackInformation.AttackerAgent, CrpgCharacterPerkType.Marksman)
                && attackInformation.AttackerWeapon.CurrentUsageItem.IsRangedWeapon)
            {
                finalDamage *= CrpgPerksConstants.MarksmanRangedDamageMultiplier;
            }

            // Brusier: +8% melee damage
            if (CrpgPerksApplicationComponent.HasPerk(attackInformation.AttackerAgent, CrpgCharacterPerkType.Brusier)
                && !attackInformation.AttackerWeapon.IsEmpty
                && attackInformation.AttackerWeapon.CurrentUsageItem.IsMeleeWeapon)
            {
                finalDamage *= CrpgPerksConstants.BrusierMeleeDamageMultiplier;
            }

            // Executioner: deals up to +30% damage the less armor the victim has on the hit body part
            if (CrpgPerksApplicationComponent.HasPerk(attackInformation.AttackerAgent, CrpgCharacterPerkType.Executioner)
                && attackInformation.VictimAgent != null)
            {
                float victimArmor = GetVictimHitArmor(attackInformation.VictimAgent, collisionData.VictimHitBodyPart);
                float t = Math.Min(victimArmor, CrpgPerksConstants.ExecutionerMaxArmorThreshold) / CrpgPerksConstants.ExecutionerMaxArmorThreshold;
                float executionerMultiplier = 1f + (CrpgPerksConstants.ExecutionerMaxDamageMultiplier - 1f) * (1f - t);
                finalDamage *= executionerMultiplier;
            }

            // BeastSlayer: +25% damage to mounts
            if (CrpgPerksApplicationComponent.HasPerk(attackInformation.AttackerAgent, CrpgCharacterPerkType.BeastSlayer)
                && !attackInformation.IsVictimAgentHuman
                && attackInformation.VictimAgent != null)
            {
                finalDamage *= CrpgPerksConstants.BeastSlayerMountDamageMultiplier;
            }
        }

        // ShieldExpert: -20% shield damage taken
        if (collisionData.AttackBlockedWithShield
            && attackInformation.VictimAgent != null
            && CrpgPerksApplicationComponent.HasPerk(attackInformation.VictimAgent, CrpgCharacterPerkType.ShieldExpert))
        {
            finalDamage *= CrpgPerksConstants.ShieldExpertShieldDamageMultiplier;
        }

        // ShieldBreaker: +25% shield damage
        if (collisionData.AttackBlockedWithShield
            && attackInformation.AttackerAgent != null
            && CrpgPerksApplicationComponent.HasPerk(attackInformation.AttackerAgent, CrpgCharacterPerkType.ShieldBreaker))
        {
            finalDamage *= CrpgPerksConstants.ShieldBreakerShieldDamageMultiplier;
        }

        // Hardheaded: -20% headshot damage taken
        if (attackInformation.VictimAgent != null
            && CrpgPerksApplicationComponent.HasPerk(attackInformation.VictimAgent, CrpgCharacterPerkType.Hardheaded)
            && (collisionData.VictimHitBodyPart == BoneBodyPartType.Head || collisionData.VictimHitBodyPart == BoneBodyPartType.Neck))
        {
            finalDamage *= CrpgPerksConstants.HardheadedHeadshotDamageMultiplier;
        }

        // Deflector: -20% projectile damage taken
        if (attackInformation.VictimAgent != null
            && CrpgPerksApplicationComponent.HasPerk(attackInformation.VictimAgent, CrpgCharacterPerkType.Deflector)
            && !attackInformation.AttackerWeapon.IsEmpty
            && attackInformation.AttackerWeapon.CurrentUsageItem.IsRangedWeapon)
        {
            finalDamage *= CrpgPerksConstants.DeflectorProjectileDamageMultiplier;
        }

        // Fate: 10% chance to avoid all damage when hit
        if (attackInformation.VictimAgent != null
            && CrpgPerksApplicationComponent.HasPerk(attackInformation.VictimAgent, CrpgCharacterPerkType.Fate))
        {
            if (MBRandom.RandomFloat < CrpgPerksConstants.FateAvoidChance)
            {
                finalDamage = 0f;
            }
        }

        return finalDamage;
    }

    public override float GetDamageMultiplierForBodyPart(BoneBodyPartType bodyPart, DamageTypes type, bool isHuman, bool isMissile)
    {
        if (isMissile)
        {
            return isHuman ? CalculateRangedDamageMultiplierForHumanBodyPart(bodyPart, type) : CalculateRangedDamageMultiplierForNonHumanBodyPart(bodyPart, type);
        }

        return isHuman ? CalculateMeleeDamageMultiplierForHumanBodyPart(bodyPart, type) : CalculateMeleeDamageMultiplierForNonHumanBodyPart(bodyPart, type);
    }

    public float CalculateRangedDamageMultiplierForHumanBodyPart(BoneBodyPartType bodyPart, DamageTypes type)
    {
        float result = 1f;
        switch (bodyPart)
        {
            case BoneBodyPartType.None:
                result = 1f;
                break;
            case BoneBodyPartType.Head:
            case BoneBodyPartType.Neck:
                switch (type)
                {
                    case DamageTypes.Invalid:
                        result = 2f;
                        break;
                    case DamageTypes.Cut:
                    case DamageTypes.Blunt:
                        result = 1.2f;
                        break;
                    case DamageTypes.Pierce:
                        result = 1.7f;
                        break;
                }

                break;
            case BoneBodyPartType.Chest:
            case BoneBodyPartType.Abdomen:
            case BoneBodyPartType.ShoulderLeft:
            case BoneBodyPartType.ShoulderRight:
                result = 0.9f;
                break;
            case BoneBodyPartType.ArmLeft:
            case BoneBodyPartType.ArmRight:
            case BoneBodyPartType.Legs:
                result = 0.75f;
                break;
        }

        return result;
    }

    public float CalculateRangedDamageMultiplierForNonHumanBodyPart(BoneBodyPartType bodyPart, DamageTypes type)
    {
        float result = 1f;
        switch (bodyPart)
        {
            case BoneBodyPartType.None:
                result = 1f;
                break;
            case BoneBodyPartType.Head:
            case BoneBodyPartType.Neck:
                switch (type)
                {
                    case DamageTypes.Invalid:
                        result = 2f;
                        break;
                    case DamageTypes.Cut:
                    case DamageTypes.Pierce:
                    case DamageTypes.Blunt:
                        result = 1.2f;
                        break;
                }

                break;
            case BoneBodyPartType.Chest:
            case BoneBodyPartType.Abdomen:
            case BoneBodyPartType.ShoulderLeft:
            case BoneBodyPartType.ShoulderRight:
            case BoneBodyPartType.ArmLeft:
            case BoneBodyPartType.ArmRight:
            case BoneBodyPartType.Legs:
                result = 0.8f;
                break;
        }

        return result;
    }

    public float CalculateMeleeDamageMultiplierForHumanBodyPart(BoneBodyPartType bodyPart, DamageTypes type)
    {
        float result = 1f;
        switch (bodyPart)
        {
            case BoneBodyPartType.None:
                result = 1f;
                break;
            case BoneBodyPartType.Head:
            case BoneBodyPartType.Neck:
                switch (type)
                {
                    case DamageTypes.Invalid:
                        result = 2f;
                        break;
                    case DamageTypes.Cut:
                    case DamageTypes.Blunt:
                        result = 1.2f;
                        break;
                    case DamageTypes.Pierce:
                        result = 1.3f;
                        break;
                }

                break;
            case BoneBodyPartType.Chest:
            case BoneBodyPartType.Abdomen:
            case BoneBodyPartType.ShoulderLeft:
            case BoneBodyPartType.ShoulderRight:
            case BoneBodyPartType.ArmLeft:
            case BoneBodyPartType.ArmRight:
                result = 1f;
                break;
            case BoneBodyPartType.Legs:
                result = 0.8f;
                break;
        }

        return result;
    }

    public float CalculateMeleeDamageMultiplierForNonHumanBodyPart(BoneBodyPartType bodyPart, DamageTypes type)
    {
        float result = 1f;
        switch (bodyPart)
        {
            case BoneBodyPartType.None:
                result = 1f;
                break;
            case BoneBodyPartType.Head:
            case BoneBodyPartType.Neck:
                switch (type)
                {
                    case DamageTypes.Invalid:
                        result = 2f;
                        break;
                    case DamageTypes.Cut:
                    case DamageTypes.Blunt:
                        result = 1.2f;
                        break;
                    case DamageTypes.Pierce:
                        result = 1.3f;
                        break;
                }

                break;
            case BoneBodyPartType.Chest:
            case BoneBodyPartType.Abdomen:
            case BoneBodyPartType.ShoulderLeft:
            case BoneBodyPartType.ShoulderRight:
            case BoneBodyPartType.ArmLeft:
            case BoneBodyPartType.ArmRight:
            case BoneBodyPartType.Legs:
                result = 0.8f;
                break;
        }

        return result;
    }

    public override void CalculateDefendedBlowStunMultipliers(
        Agent attackerAgent,
        Agent defenderAgent,
        CombatCollisionResult collisionResult,
        WeaponComponentData attackerWeapon,
        WeaponComponentData defenderWeapon,
        ref float attackerStunPeriod,
        ref float defenderStunPeriod)
    {
        if (collisionResult == CombatCollisionResult.Blocked && defenderAgent.WieldedOffhandWeapon.IsShield())
        {
            int shieldSkill = 0;
            if (defenderAgent.Origin is CrpgBattleAgentOrigin crpgOrigin)
            {
                shieldSkill = crpgOrigin.Skills.Skills.GetPropertyValue(CrpgSkills.Shield);
            }

            defenderStunPeriod /= MathHelper.RecursivePolynomialFunctionOfDegree2(shieldSkill, _constants.ShieldDefendStunMultiplierForSkillRecursiveCoefs);
        }
    }

    // TODO : Consider reworking once https://forums.taleworlds.com/index.php?threads/missioncombatmechanicshelper-getdefendcollisionresults-bypass-strikemagnitudecalculationmodel.459379 is fixed
    public override bool DecideCrushedThrough(
        Agent attackerAgent,
        Agent defenderAgent,
        float totalAttackEnergy,
        Agent.UsageDirection attackDirection,
        StrikeType strikeType,
        WeaponComponentData? defendItem,
        bool isPassiveUsage)
    {
        EquipmentIndex wieldedItemIndex = attackerAgent.GetOffhandWieldedItemIndex();
        if (wieldedItemIndex == EquipmentIndex.None)
        {
            wieldedItemIndex = attackerAgent.GetPrimaryWieldedItemIndex();
        }

        var weaponComponentData = wieldedItemIndex != EquipmentIndex.None
                ? attackerAgent.Equipment[wieldedItemIndex].CurrentUsageItem
                : null;
        if (weaponComponentData == null
            || isPassiveUsage
            || !weaponComponentData.WeaponFlags.HasAnyFlag(WeaponFlags.CanCrushThrough)
            || strikeType != StrikeType.Swing
            || attackDirection != Agent.UsageDirection.AttackUp)
        {
            return false;
        }

        int powerStrike = GetSkillValue(attackerAgent, CrpgSkills.PowerStrike);
        int defenderStrength = GetSkillValue(defenderAgent, CrpgSkills.Strength);
        int defenderShield = GetSkillValue(defenderAgent, CrpgSkills.Shield);

        float attackerPower = 3f * powerStrike;
        float defenderDefendPower = defendItem?.IsShield == true
                ? (float)Math.Max(defenderShield * 6 + 3, defenderStrength)
                : defenderStrength;
        defenderDefendPower = Math.Max(defenderDefendPower, 1f);

        int randomNumber = MBRandom.RandomInt(0, 1000);
        return randomNumber / 10f < (float)Math.Pow(attackerPower / defenderDefendPower / 2.6f, 2.6f) * 100f;
    }

    // MissionCombatMechanicsHelper.cs/DecideMountRearedByBlow
    public override bool DecideMountRearedByBlow(
        Agent attackerAgent,
        Agent victimAgent,
        in AttackCollisionData collisionData,
        WeaponComponentData attackerWeapon,
        in Blow blow)
    {
        // Only allow if damage type is Pierce
        if (blow.DamageType != DamageTypes.Pierce)
        {
            return false;
        }

        // Call the original/base logic (which evolves with TW updates)
        return base.DecideMountRearedByBlow(attackerAgent, victimAgent, in collisionData, attackerWeapon, in blow);
    }

    private int GetSkillValue(IAgentOriginBase agentOrigin, SkillObject skill)
    {
        if (agentOrigin is CrpgBattleAgentOrigin crpgOrigin)
        {
            return crpgOrigin.Skills.Skills.GetPropertyValue(skill);
        }

        return 0;
    }

    private int GetSkillValue(Agent? agent, SkillObject? skill)
    {
        if (agent?.Origin is CrpgBattleAgentOrigin crpgOrigin)
        {
            return crpgOrigin.Skills.Skills.GetPropertyValue(skill);
        }

        if (agent?.Character != null && skill != null)
        {
            return agent.Character.GetSkillValue(skill);
        }

        return 0;
    }

    private bool IsPlayerCharacterAttackingVipBot(AttackInformation attackInformation)
    {
        if (attackInformation.AttackerAgentOrigin is CrpgBattleAgentOrigin)
        {
            bool isVictimTheVipBot = attackInformation.VictimAgentCharacter != null
                                     && attackInformation.VictimAgentCharacter.StringId.StartsWith("crpg_dtv_vip_");

            return isVictimTheVipBot;
        }

        return false;
    }

    private bool IsPlayerCharacterAttackingDtvBot(AttackInformation attackInformation)
    {
        if (attackInformation.AttackerAgentOrigin is CrpgBattleAgentOrigin)
        {
            bool isVictimDtvBot = attackInformation.VictimAgentCharacter != null
                                  && attackInformation.VictimAgentCharacter.StringId.StartsWith("crpg_dtv_");

            return isVictimDtvBot;
        }

        return false;
    }

    private bool IsPlayerCharacterAttackingDtvBoss(AttackInformation attackInformation)
    {
        if (attackInformation.AttackerAgentOrigin is CrpgBattleAgentOrigin)
        {
            bool isVictimDtvBoss = attackInformation.VictimAgentCharacter != null &&
                                   attackInformation.VictimAgentCharacter.StringId.EndsWith("_boss");

            return isVictimDtvBoss;
        }

        return false;
    }

    private bool IsSwashbuckler(MissionWeapon weapon, AttackCollisionData collisionData, Agent attackerAgent)
    {
        if (weapon.CurrentUsageItem.WeaponClass != WeaponClass.OneHandedSword
            || collisionData.StrikeType == (int)StrikeType.Thrust
            || collisionData.DamageType != (int)DamageTypes.Cut)
        {
            return false;
        }

        bool hasRangedWeapon = false;
        bool hasShield = false;

        var offhandItem = attackerAgent.Equipment[EquipmentIndex.Weapon2];
        if (offhandItem.Item?.PrimaryWeapon?.IsShield == true)
        {
            hasShield = true;
        }

        for (int i = 0; i < (int)EquipmentIndex.NumAllWeaponSlots; i++)
        {
            var equipmentElement = attackerAgent.Equipment[i];
            var item = equipmentElement.Item;
            if (item == null)
            {
                continue;
            }

            var usage = item.PrimaryWeapon;
            if (usage == null)
            {
                continue;
            }

            if (!hasRangedWeapon)
            {
                bool isAmmoEmpty = equipmentElement.Amount == 0 && usage.IsConsumable;
                bool hasAlternateThrowable = false;

                for (int j = 0; j < item.WeaponComponent?.Weapons?.Count; j++)
                {
                    var altUsage = item.WeaponComponent.Weapons[j];
                    if (altUsage != usage && altUsage.IsConsumable && altUsage.IsRangedWeapon)
                    {
                        hasAlternateThrowable = true;
                        break;
                    }
                }

                if ((usage.IsRangedWeapon && !isAmmoEmpty) || hasAlternateThrowable)
                {
                    hasRangedWeapon = true;
                }
            }

            if (!hasShield && usage.IsShield)
            {
                hasShield = true;
            }

            if (hasShield || hasRangedWeapon)
            {
                break;
            }
        }

        return !attackerAgent.HasMount && !hasShield && !hasRangedWeapon;
    }

    private int GetGloveArmor(IAgentOriginBase agentOrigin)
    {
        if (agentOrigin is CrpgBattleAgentOrigin crpgOrigin)
        {
            return crpgOrigin.ArmorItems.FirstOrDefault(a => a.type == ItemObject.ItemTypeEnum.HandArmor).armor?.ArmArmor ?? 0;
        }

        return 0;
    }

    private static float GetVictimHitArmor(Agent victimAgent, BoneBodyPartType hitBodyPart)
    {
        float armor = 0;
        switch (hitBodyPart)
        {
            case BoneBodyPartType.Head:
            case BoneBodyPartType.Neck:
            {
                var element = victimAgent.SpawnEquipment[EquipmentIndex.NumAllWeaponSlots];
                armor = element.Item?.ArmorComponent?.HeadArmor ?? 0;
                break;
            }

            case BoneBodyPartType.Chest:
            case BoneBodyPartType.Abdomen:
            case BoneBodyPartType.ShoulderLeft:
            case BoneBodyPartType.ShoulderRight:
            {
                var element = victimAgent.SpawnEquipment[EquipmentIndex.Body];
                armor = element.Item?.ArmorComponent?.BodyArmor ?? 0;
                break;
            }

            case BoneBodyPartType.ArmLeft:
            case BoneBodyPartType.ArmRight:
            {
                var element = victimAgent.SpawnEquipment[EquipmentIndex.Gloves];
                armor = element.Item?.ArmorComponent?.ArmArmor ?? 0;
                break;
            }

            case BoneBodyPartType.Legs:
            {
                var element = victimAgent.SpawnEquipment[EquipmentIndex.Leg];
                armor = element.Item?.ArmorComponent?.LegArmor ?? 0;
                break;
            }
        }

        return armor;
    }
}
