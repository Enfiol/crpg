using Crpg.Module.Api.Models.Characters;
using Crpg.Module.Api.Models.Items;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Crpg.Module.Common;

internal class CrpgBattleAgentOrigin : BasicBattleAgentOrigin
{
    public MBCharacterSkills Skills { get; }
    public IList<CrpgCharacterPerkType> SelectedPerks { get; }
    public List<(CrpgItemArmorComponent armor, ItemObject.ItemTypeEnum type)> ArmorItems { get; } = new();

    public CrpgBattleAgentOrigin(BasicCharacterObject? troop, MBCharacterSkills skills, IList<CrpgCharacterPerkType>? selectedPerks = null)
        : base(troop)
    {
        Skills = skills;
        SelectedPerks = selectedPerks ?? Array.Empty<CrpgCharacterPerkType>();
    }
}
