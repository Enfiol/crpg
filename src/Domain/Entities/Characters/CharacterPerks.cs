namespace Crpg.Domain.Entities.Characters;

/// <summary>
/// Perks of a character.
/// </summary>
public class CharacterPerks
{
    /// <summary>
    /// Points to spent.
    /// </summary>
    public int Points { get; set; }

    /// <summary>
    /// Selected perk ids stored as JSON array.
    /// </summary>
    public IList<CharacterPerkType> SelectedPerks { get; set; } = new List<CharacterPerkType>();
}
