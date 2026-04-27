namespace Crpg.Module.Api.Models.Characters;

// Copy of Crpg.Application.Characters.Models.CharacterPerksViewModel
internal class CrpgCharacterPerks
{
    public int Points { get; set; }
    public IList<CrpgCharacterPerkType> SelectedPerks { get; set; } = Array.Empty<CrpgCharacterPerkType>();
}
