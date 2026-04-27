using Crpg.Module.Api.Models.Characters;
using TaleWorlds.MountAndBlade;

namespace Crpg.Module.Common;

/// <summary>
/// MissionBehavior that slowly regenerates health for agents with the Regeneration perk.
/// </summary>
internal class CrpgPerkRegenerationBehavior : MissionBehavior
{
    public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;

    public override void OnMissionTick(float dt)
    {
        base.OnMissionTick(dt);

        if (!GameNetwork.IsServer)
        {
            return;
        }

        float healAmount = CrpgPerksConstants.RegenerationHealPerSecond * dt;

        foreach (Agent agent in Mission.Agents)
        {
            if (!agent.IsHuman
                || !agent.IsActive()
                || agent.Health <= 0f
                || agent.Health >= agent.HealthLimit)
            {
                continue;
            }

            if (!CrpgPerksApplicationComponent.HasPerk(agent, CrpgCharacterPerkType.Regeneration))
            {
                continue;
            }

            float newHealth = Math.Min(agent.Health + healAmount, agent.HealthLimit);
            agent.Health = newHealth;
        }


    }
}
