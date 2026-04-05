using System.Text.Json;
using System.Text.Json.Serialization;
using Crpg.Application.Common.Interfaces;
using Crpg.Domain.Entities.Quests;

namespace Crpg.Application.Common.Files;

internal class FileQuestSource : IQuestSource
{
    private static readonly string QuestsPath = FileDataPathResolver.Resolve(
        Path.Combine("Common", "Files", "quests.json"));

    public async Task<IEnumerable<QuestDefinition>> LoadQuests()
    {
        await using var file = File.OpenRead(QuestsPath);
        return (await JsonSerializer.DeserializeAsync<IEnumerable<QuestDefinition>>(file, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter() },
        }).AsTask())!;
    }
}
