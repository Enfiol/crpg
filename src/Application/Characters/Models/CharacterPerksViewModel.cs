using AutoMapper;
using Crpg.Application.Common.Mappings;
using Crpg.Domain.Entities.Characters;

namespace Crpg.Application.Characters.Models;

public record CharacterPerksViewModel : IMapFrom<CharacterPerks>
{
    public int Points { get; init; }
    public IList<CharacterPerkType> SelectedPerks { get; init; } = Array.Empty<CharacterPerkType>();
}
