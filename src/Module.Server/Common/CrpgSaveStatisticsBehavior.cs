using Crpg.Domain.Entities.BattleEvents;
using Crpg.Module.Api;
using Crpg.Module.Api.Models.Items;
using Crpg.Module.Modes.Warmup;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace Crpg.Module.Common;

internal class CrpgSaveStatisticsBehavior : MissionBehavior
{
    private const int FlushIntervalMilliseconds = 5000;

    private static readonly Dictionary<EquipmentIndex, CrpgItemSlot> EquipmentIndexToItemSlot = new()
    {
        [EquipmentIndex.Head] = CrpgItemSlot.Head,
        [EquipmentIndex.Cape] = CrpgItemSlot.Shoulder,
        [EquipmentIndex.Body] = CrpgItemSlot.Body,
        [EquipmentIndex.Gloves] = CrpgItemSlot.Hand,
        [EquipmentIndex.Leg] = CrpgItemSlot.Leg,
        [EquipmentIndex.HorseHarness] = CrpgItemSlot.MountHarness,
        [EquipmentIndex.Horse] = CrpgItemSlot.Mount,
        [EquipmentIndex.Weapon0] = CrpgItemSlot.Weapon0,
        [EquipmentIndex.Weapon1] = CrpgItemSlot.Weapon1,
        [EquipmentIndex.Weapon2] = CrpgItemSlot.Weapon2,
        [EquipmentIndex.Weapon3] = CrpgItemSlot.Weapon3,
        [EquipmentIndex.ExtraWeaponSlot] = CrpgItemSlot.WeaponExtra,
    };

    private readonly CrpgWarmupComponent? _warmupComponent;
    private readonly ICrpgClient _crpgClient;
    private readonly List<CrpgGameEvent> _buffer;
    private DateTime _nextFlushTime;

    public CrpgSaveStatisticsBehavior(CrpgWarmupComponent? warmupComponent, ICrpgClient crpgClient)
    {
        _warmupComponent = warmupComponent;
        _crpgClient = crpgClient;
        _buffer = new();
        _nextFlushTime = DateTime.Now.AddMilliseconds(FlushIntervalMilliseconds);
    }

    private string? GetCrpgItemIdForWeapon(CrpgPeer affectorCrpgPeer, EquipmentIndex equipmentIndex)
    {
        if (!EquipmentIndexToItemSlot.TryGetValue(equipmentIndex, out CrpgItemSlot itemSlot))
        {
            return null;
        }

        var equippedItem = affectorCrpgPeer.User?.Character.EquippedItems
            .FirstOrDefault(ei => ei.Slot == itemSlot);
        return equippedItem?.UserItem.ItemId;
    }

    public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;

    public override void OnAgentHit(
        Agent affectedAgent,
        Agent affectorAgent,
        in MissionWeapon affectorWeapon,
        in Blow blow,
        in AttackCollisionData attackCollisionData)
    {
        if (_warmupComponent is { IsInWarmup: true })
        {
            return;
        }

        // Affector must be a player
        if (affectorAgent.MissionPeer == null)
        {
            return;
        }

        var affectorCrpgPeer = affectorAgent.MissionPeer.Peer.GetComponent<CrpgPeer>();
        if (affectorCrpgPeer?.User == null)
        {
            return;
        }

        // Affected must be either a player or a horse
        if (affectedAgent.MissionPeer == null && !affectedAgent.IsMount)
        {
            return;
        }

        // Don't process self-hits or team hits for statistics
        if (affectedAgent == affectorAgent) // disabled for testing || affectedAgent.Team == affectorAgent.Team)
        {
            return;
        }

        bool isRanged = !affectorWeapon.IsEmpty && affectorWeapon.CurrentUsageItem.IsRangedWeapon;
        string targetType = affectedAgent.IsMount ? "Horse" : "Player";

        // Check if attack was blocked (by shield or weapon)
        bool isBlocked = attackCollisionData.AttackBlockedWithShield
                         || attackCollisionData.MissileBlockedWithWeapon
                         || attackCollisionData.CollisionResult == CombatCollisionResult.Blocked
                         || attackCollisionData.CollisionResult == CombatCollisionResult.Parried
                         || attackCollisionData.CollisionResult == CombatCollisionResult.ChamberBlocked;

        if (isBlocked)
        {
            // Get the blocker's CrpgPeer (affected agent)
            var blockerCrpgPeer = affectedAgent.MissionPeer?.Peer?.GetComponent<CrpgPeer>();
            if (blockerCrpgPeer?.User == null)
            {
                return; // Can't track block event for non-player blockers
            }

            CrpgGameEvent blockEvt = new()
            {
                UserId = blockerCrpgPeer.User.Id, // Blocker's user ID, not attacker's
                Type = CrpgGameEvent.EventType.Block,
                EventData = new Dictionary<CrpgGameEvent.EventField, string>
                {
                    { CrpgGameEvent.EventField.HitType, isRanged ? "Ranged" : "Melee" },
                    { CrpgGameEvent.EventField.TargetType, targetType },
                },
            };

            // Determine block type and find the blocking item
            string? blockingItemId = null;
            string? blockingWeaponType = null;
            bool isShieldBlock = attackCollisionData.AttackBlockedWithShield;

            if (isShieldBlock)
            {
                // Damage to shield
                blockEvt.EventData![CrpgGameEvent.EventField.Damage] = attackCollisionData.InflictedDamage.ToString();
                // Try to find shield item ID from blocker's equipment
                var possibleShieldSlots = new[]
                {
                    EquipmentIndex.Weapon0, EquipmentIndex.Weapon1, EquipmentIndex.Weapon2, EquipmentIndex.Weapon3,
                };
                foreach (var slot in possibleShieldSlots)
                {
                    var weapon = affectedAgent.Equipment[slot];
                    if (!weapon.IsEmpty && weapon.CurrentUsageItem?.IsShield == true)
                    {
                        blockingItemId = GetCrpgItemIdForWeapon(blockerCrpgPeer, slot);
                        blockingWeaponType = weapon.CurrentUsageItem.WeaponClass.ToString();
                        break;
                    }
                }
            }
            else
            {
                // Weapon block - try to find the wielded weapon that blocked
                EquipmentIndex wieldedIndex = affectedAgent.GetPrimaryWieldedItemIndex();
                if (wieldedIndex == EquipmentIndex.None)
                {
                    wieldedIndex = affectedAgent.GetOffhandWieldedItemIndex();
                }

                if (wieldedIndex != EquipmentIndex.None)
                {
                    blockingItemId = GetCrpgItemIdForWeapon(blockerCrpgPeer, wieldedIndex);
                    var weapon = affectedAgent.Equipment[wieldedIndex];
                    if (!weapon.IsEmpty)
                    {
                        blockingWeaponType = weapon.CurrentUsageItem.WeaponClass.ToString();
                    }
                }
            }

            // Store blocking item info
            if (blockingItemId != null)
            {
                blockEvt.EventData![CrpgGameEvent.EventField.WeaponId] = blockingItemId;
            }

            if (blockingWeaponType != null)
            {
                blockEvt.EventData![CrpgGameEvent.EventField.WeaponType] = blockingWeaponType;
            }

            _buffer.Add(blockEvt);
            return; // Don't create a hit event for blocked attacks
        }

        CrpgGameEvent evt = new()
        {
            UserId = affectorCrpgPeer.User.Id,
            Type = CrpgGameEvent.EventType.Hit,
            EventData = new Dictionary<CrpgGameEvent.EventField, string>
            {
                { CrpgGameEvent.EventField.Damage, attackCollisionData.InflictedDamage.ToString() },
                { CrpgGameEvent.EventField.HitType, isRanged ? "Ranged" : "Melee" },
                { CrpgGameEvent.EventField.TargetType, targetType },
                { CrpgGameEvent.EventField.DamageType, ((DamageTypes)attackCollisionData.DamageType).ToString() },
            },
        };

        string? bodyPart = ToString(attackCollisionData.VictimHitBodyPart);

        if (bodyPart != null)
        {
            evt.EventData![CrpgGameEvent.EventField.BodyPart] = bodyPart;
        }

        if (!affectorWeapon.IsEmpty)
        {
            evt.EventData![CrpgGameEvent.EventField.WeaponType] =
                affectorWeapon.CurrentUsageItem.WeaponClass.ToString();
            if (affectorWeapon.Item != null)
            {
                string? weaponId = null;
                // Try to get crpg item ID first
                if (attackCollisionData.AffectorWeaponSlotOrMissileIndex >= 0)
                {
                    EquipmentIndex equipmentIndex =
                        (EquipmentIndex)attackCollisionData.AffectorWeaponSlotOrMissileIndex;
                    weaponId = GetCrpgItemIdForWeapon(affectorCrpgPeer, equipmentIndex);
                }

                // If we have a weaponId (crpg item ID), add it to event data
                if (weaponId != null)
                {
                    evt.EventData![CrpgGameEvent.EventField.WeaponId] = weaponId;
                }
            }
        }

        _buffer.Add(evt);
    }

    public override void OnAgentRemoved(Agent affectedAgent, Agent? affectorAgent, AgentState agentState,
        KillingBlow blow)
    {
        base.OnAgentRemoved(affectedAgent, affectorAgent, agentState, blow);

        if (_warmupComponent is { IsInWarmup: true })
        {
            return;
        }

        // Only process kills
        if (agentState != AgentState.Killed || affectorAgent == null)
        {
            return;
        }

        // Affector must be a player
        if (affectorAgent.MissionPeer == null)
        {
            return;
        }

        var affectorCrpgPeer = affectorAgent.MissionPeer.Peer.GetComponent<CrpgPeer>();
        if (affectorCrpgPeer?.User == null)
        {
            return;
        }

        // Affected must be either a player or a horse
        if (affectedAgent.MissionPeer == null && !affectedAgent.IsMount)
        {
            return;
        }

        // Don't process self-kills or team kills for statistics
        if (affectedAgent == affectorAgent) // DEBUG || affectedAgent.Team == affectorAgent.Team)
        {
            return;
        }

        string targetType = affectedAgent.IsMount ? "Horse" : "Player";

        CrpgGameEvent evt = new()
        {
            UserId = affectorCrpgPeer.User.Id,
            Type = CrpgGameEvent.EventType.Kill,
            EventData = new Dictionary<CrpgGameEvent.EventField, string>
            {
                { CrpgGameEvent.EventField.TargetType, targetType },
                { CrpgGameEvent.EventField.DamageType, blow.DamageType.ToString() },
            },
        };

        // Get weapon info from currently wielded weapon
        EquipmentIndex weaponIndex = affectorAgent.GetPrimaryWieldedItemIndex();

        if (weaponIndex != EquipmentIndex.None)
        {
            // Get weapon ID
            string? weaponId = GetCrpgItemIdForWeapon(affectorCrpgPeer, weaponIndex);
            if (weaponId != null)
            {
                evt.EventData![CrpgGameEvent.EventField.WeaponId] = weaponId;
            }

            // Get weapon type and determine hit type
            var weapon = affectorAgent.Equipment[weaponIndex];
            if (!weapon.IsEmpty)
            {
                // Weapon type
                evt.EventData![CrpgGameEvent.EventField.WeaponType] =
                    weapon.CurrentUsageItem.WeaponClass.ToString();

                // Hit type (ranged or melee)
                bool isRanged = weapon.CurrentUsageItem.IsRangedWeapon;
                evt.EventData![CrpgGameEvent.EventField.HitType] = isRanged ? "Ranged" : "Melee";
            }
        }

        // Body part
        string? bodyPart = ToString(blow.VictimBodyPart);
        if (bodyPart != null)
        {
            evt.EventData![CrpgGameEvent.EventField.BodyPart] = bodyPart;
        }

        _buffer.Add(evt);
    }

    public override void OnMissionTick(float dt)
    {
        base.OnMissionTick(dt);

        if (DateTime.Now >= _nextFlushTime && _buffer.Count > 0)
        {
            FlushBuffer();
            _nextFlushTime = DateTime.Now.AddMilliseconds(FlushIntervalMilliseconds);
        }
    }

    private void FlushBuffer()
    {
        var events = _buffer.ToArray();
        _buffer.Clear();
        _ = _crpgClient.CreateBattleEventsAsync(events); // Fire and forget
        Debug.Print($"Sent {events.Length} battle events");
    }

    private string? ToString(BoneBodyPartType partType)
    {
        switch (partType)
        {
            case BoneBodyPartType.Head:
                return nameof(BoneBodyPartType.Head); // Conflict with value CriticalBodyPartsBegin
            case BoneBodyPartType.ArmLeft:
                return nameof(BoneBodyPartType.ArmLeft); // Conflict with value CriticalBodyPartsEnd
            case BoneBodyPartType.Neck:
            case BoneBodyPartType.Chest:
            case BoneBodyPartType.Abdomen:
            case BoneBodyPartType.ShoulderLeft:
            case BoneBodyPartType.ShoulderRight:
            case BoneBodyPartType.ArmRight:
            case BoneBodyPartType.Legs:
                return partType.ToString(); // Other valid cases
            default:
                return null;
        }
    }
}
