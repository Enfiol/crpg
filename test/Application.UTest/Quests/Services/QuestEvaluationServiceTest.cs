using Crpg.Application.Quests.Services;
using Crpg.Domain.Entities.BattleEvents;
using Crpg.Domain.Entities.Quests;
using NUnit.Framework;

namespace Crpg.Application.UTest.Quests.Services;

public class QuestEvaluationServiceTest : TestBase
{
    [Test]
    public async Task ComputeCurrentValueShouldCountOnlyMatchingEvents()
    {
        UserQuest userQuest = new()
        {
            UserId = 10,
            CreatedAt = new DateTime(2026, 04, 10, 15, 00, 00, DateTimeKind.Utc),
            QuestDefinition = new QuestDefinition
            {
                EventType = CrpgGameEvent.EventType.Kill,
                AggregationType = QuestAggregationType.Count,
            },
        };

        ArrangeDb.BattleEvents.AddRange(
            new CrpgGameEvent { UserId = 10, Type = CrpgGameEvent.EventType.Kill, CreatedAt = new DateTime(2026, 04, 10, 00, 00, 00, DateTimeKind.Utc) },
            new CrpgGameEvent { UserId = 10, Type = CrpgGameEvent.EventType.Kill, CreatedAt = new DateTime(2026, 04, 11, 12, 00, 00, DateTimeKind.Utc) },
            new CrpgGameEvent { UserId = 10, Type = CrpgGameEvent.EventType.Block, CreatedAt = new DateTime(2026, 04, 11, 12, 00, 00, DateTimeKind.Utc) },
            new CrpgGameEvent { UserId = 11, Type = CrpgGameEvent.EventType.Kill, CreatedAt = new DateTime(2026, 04, 11, 12, 00, 00, DateTimeKind.Utc) },
            new CrpgGameEvent { UserId = 10, Type = CrpgGameEvent.EventType.Kill, CreatedAt = new DateTime(2026, 04, 09, 23, 59, 59, DateTimeKind.Utc) });
        await ArrangeDb.SaveChangesAsync();

        QuestEvaluationService service = new(ActDb);

        int value = await service.ComputeCurrentValueAsync(userQuest);

        Assert.That(value, Is.EqualTo(2));
    }

    [Test]
    public async Task ComputeCurrentValueShouldApplyEventFilters()
    {
        UserQuest userQuest = new()
        {
            UserId = 10,
            CreatedAt = new DateTime(2026, 04, 10, 08, 00, 00, DateTimeKind.Utc),
            QuestDefinition = new QuestDefinition
            {
                EventType = CrpgGameEvent.EventType.Hit,
                AggregationType = QuestAggregationType.Count,
                EventFiltersJson =
                [
                    new Dictionary<string, string>
                    {
                        ["WeaponType"] = "Sword",
                        ["TargetType"] = "Player",
                    },
                    new Dictionary<string, string>
                    {
                        ["HitType"] = "Headshot",
                    },
                ],
            },
        };

        ArrangeDb.BattleEvents.AddRange(
            new CrpgGameEvent
            {
                UserId = 10,
                Type = CrpgGameEvent.EventType.Hit,
                CreatedAt = new DateTime(2026, 04, 10, 09, 00, 00, DateTimeKind.Utc),
                EventData = new Dictionary<CrpgGameEvent.EventField, string>
                {
                    [CrpgGameEvent.EventField.WeaponType] = "Sword",
                    [CrpgGameEvent.EventField.TargetType] = "Player",
                },
            },
            new CrpgGameEvent
            {
                UserId = 10,
                Type = CrpgGameEvent.EventType.Hit,
                CreatedAt = new DateTime(2026, 04, 10, 10, 00, 00, DateTimeKind.Utc),
                EventData = new Dictionary<CrpgGameEvent.EventField, string>
                {
                    [CrpgGameEvent.EventField.HitType] = "Headshot",
                },
            },
            new CrpgGameEvent
            {
                UserId = 10,
                Type = CrpgGameEvent.EventType.Hit,
                CreatedAt = new DateTime(2026, 04, 10, 11, 00, 00, DateTimeKind.Utc),
                EventData = new Dictionary<CrpgGameEvent.EventField, string>
                {
                    [CrpgGameEvent.EventField.WeaponType] = "Sword",
                },
            },
            new CrpgGameEvent
            {
                UserId = 10,
                Type = CrpgGameEvent.EventType.Hit,
                CreatedAt = new DateTime(2026, 04, 10, 12, 00, 00, DateTimeKind.Utc),
                EventData = null,
            });
        await ArrangeDb.SaveChangesAsync();

        QuestEvaluationService service = new(ActDb);

        int value = await service.ComputeCurrentValueAsync(userQuest);

        Assert.That(value, Is.EqualTo(2));
    }

    [Test]
    public async Task ComputeCurrentValueShouldSumOnlyParseableValues()
    {
        UserQuest userQuest = new()
        {
            UserId = 10,
            CreatedAt = new DateTime(2026, 04, 10, 08, 00, 00, DateTimeKind.Utc),
            QuestDefinition = new QuestDefinition
            {
                EventType = CrpgGameEvent.EventType.Hit,
                AggregationType = QuestAggregationType.Sum,
                SumField = CrpgGameEvent.EventField.Damage,
            },
        };

        ArrangeDb.BattleEvents.AddRange(
            new CrpgGameEvent
            {
                UserId = 10,
                Type = CrpgGameEvent.EventType.Hit,
                CreatedAt = new DateTime(2026, 04, 10, 09, 00, 00, DateTimeKind.Utc),
                EventData = new Dictionary<CrpgGameEvent.EventField, string>
                {
                    [CrpgGameEvent.EventField.Damage] = "12",
                },
            },
            new CrpgGameEvent
            {
                UserId = 10,
                Type = CrpgGameEvent.EventType.Hit,
                CreatedAt = new DateTime(2026, 04, 10, 10, 00, 00, DateTimeKind.Utc),
                EventData = new Dictionary<CrpgGameEvent.EventField, string>
                {
                    [CrpgGameEvent.EventField.Damage] = "not-an-int",
                },
            },
            new CrpgGameEvent
            {
                UserId = 10,
                Type = CrpgGameEvent.EventType.Hit,
                CreatedAt = new DateTime(2026, 04, 10, 11, 00, 00, DateTimeKind.Utc),
                EventData = new Dictionary<CrpgGameEvent.EventField, string>
                {
                    [CrpgGameEvent.EventField.TargetType] = "Player",
                },
            });
        await ArrangeDb.SaveChangesAsync();

        QuestEvaluationService service = new(ActDb);

        int value = await service.ComputeCurrentValueAsync(userQuest);

        Assert.That(value, Is.EqualTo(12));
    }

    [Test]
    public async Task ComputeCurrentValueShouldReturnZeroWhenSumFieldIsNull()
    {
        UserQuest userQuest = new()
        {
            UserId = 10,
            CreatedAt = new DateTime(2026, 04, 10, 08, 00, 00, DateTimeKind.Utc),
            QuestDefinition = new QuestDefinition
            {
                EventType = CrpgGameEvent.EventType.Hit,
                AggregationType = QuestAggregationType.Sum,
                SumField = null,
            },
        };

        ArrangeDb.BattleEvents.Add(new CrpgGameEvent
        {
            UserId = 10,
            Type = CrpgGameEvent.EventType.Hit,
            CreatedAt = new DateTime(2026, 04, 10, 09, 00, 00, DateTimeKind.Utc),
            EventData = new Dictionary<CrpgGameEvent.EventField, string>
            {
                [CrpgGameEvent.EventField.Damage] = "50",
            },
        });
        await ArrangeDb.SaveChangesAsync();

        QuestEvaluationService service = new(ActDb);

        int value = await service.ComputeCurrentValueAsync(userQuest);

        Assert.That(value, Is.EqualTo(0));
    }
}
