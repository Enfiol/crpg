using System;
using System.Collections.Concurrent;
using Crpg.Domain.Entities.BattleEvents;
using Crpg.Module.Api;
using Crpg.Module.Api.Models;
using Crpg.Module.Api.Models.Characters;
using Crpg.Module.Modes.Warmup;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace Crpg.Module.Common;

internal class CrpgSaveStatisticsBehavior : MissionBehavior
{
    private readonly CrpgWarmupComponent? _warmupComponent;
    private readonly ICrpgClient _crpgClient;
    private readonly List<CrpgGameEvent> _buffer;

    public CrpgSaveStatisticsBehavior(CrpgWarmupComponent? warmupComponent, ICrpgClient crpgClient)
    {
        _warmupComponent = warmupComponent;
        _crpgClient = crpgClient;
        _buffer = new();
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

        // Only process hits between players
        if (affectedAgent.MissionPeer == null || affectorAgent.MissionPeer == null)
        {
            return;
        }

        var affectedCrpgPeer = affectedAgent.MissionPeer.Peer.GetComponent<CrpgPeer>();
        var affectorCrpgPeer = affectorAgent.MissionPeer.Peer.GetComponent<CrpgPeer>();

        if (affectedCrpgPeer?.User == null || affectorCrpgPeer?.User == null)
        {
            return;
        }


        // Don't process self-hits or team hits for statistics
        if (affectedAgent == affectorAgent) // disabled for testing || affectedAgent.Team == affectorAgent.Team)
        {
            return;
        }

        // Create CrpgGameEvent
        CrpgGameEvent evt = new()
        {
            UserId = affectorCrpgPeer.User.Id,
            Type = CrpgGameEvent.EventType.Hit,
            EventData = new Dictionary<CrpgGameEvent.EventField, string>
            {
                { CrpgGameEvent.EventField.Damage, attackCollisionData.InflictedDamage.ToString() },
                { CrpgGameEvent.EventField.BodyPart, attackCollisionData.VictimHitBodyPart.ToString() },
            },
        };

        _buffer.Add(evt);
        FlushBuffer(); // TODO: Debug only. Should be replaced with time based function
    }

    /*public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState,
        KillingBlow blow)
    {
        if (_warmupComponent is { IsInWarmup: true })
        {
            return;
        }

        // Only process player agents
        if (affectedAgent.MissionPeer == null)
        {
            return;
        }

        var affectedCrpgPeer = affectedAgent.MissionPeer.Peer.GetComponent<CrpgPeer>();
        if (affectedCrpgPeer?.User == null)
        {
            return;
        }

        int affectedUserId = affectedCrpgPeer.User.Id;
        int affectedCharacterId = affectedCrpgPeer.User.Character.Id;

        // If there's a killer (affectorAgent), update their kill count
        if (affectorAgent?.MissionPeer != null)
        {
            var affectorCrpgPeer = affectorAgent.MissionPeer.Peer.GetComponent<CrpgPeer>();
            if (affectorCrpgPeer?.User != null && affectedAgent != affectorAgent &&
                affectedAgent.Team != affectorAgent.Team)
            {
                int affectorUserId = affectorCrpgPeer.User.Id;
                // TODO: Track kill for statistics
            }
        }
    }*/

    public override void OnRemoveBehavior()
    {
        FlushBuffer();
        base.OnRemoveBehavior();
    }

    private void FlushBuffer()
    {
        var events = _buffer.ToArray();
        _buffer.Clear();
        _ = _crpgClient.CreateBattleEventsAsync(events);
        Debug.Print($"Sent {events.Length} battle events");
    }
}
