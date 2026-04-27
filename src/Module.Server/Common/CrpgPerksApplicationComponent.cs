using Crpg.Module.Api.Models.Characters;
using TaleWorlds.MountAndBlade;

namespace Crpg.Module.Common;

/// <summary>
/// Provides static helpers to check if an agent has a specific perk.
/// </summary>
internal static class CrpgPerksApplicationComponent
{
    /// <summary>
    /// Checks whether the given agent's origin has a specific perk selected.
    /// </summary>
    public static bool HasPerk(Agent agent, CrpgCharacterPerkType perkType)
    {
        if (agent?.Origin is CrpgBattleAgentOrigin crpgOrigin)
        {
            return crpgOrigin.SelectedPerks.Contains(perkType);
        }

        return false;
    }

    /// <summary>
    /// Checks whether the given peer's character has a specific perk selected.
    /// </summary>
    public static bool HasPerk(CrpgPeer crpgPeer, CrpgCharacterPerkType perkType)
    {
        return crpgPeer.User?.Character?.Characteristics.Perks.SelectedPerks.Contains(perkType) ?? false;
    }
}
