using System.Text.Json.Serialization;
using AutoMapper;
using Crpg.Application.Common.Interfaces;
using Crpg.Application.Common.Mediator;
using Crpg.Application.Common.Results;
using Crpg.Application.Common.Services;
using Crpg.Application.Quests.Models;
using Crpg.Application.Quests.Services;
using Crpg.Sdk.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using LoggerFactory = Crpg.Logging.LoggerFactory;

namespace Crpg.Application.Quests.Commands;

public record ClaimQuestRewardCommand : IMediatorRequest<UserQuestViewModel>
{
    [JsonIgnore]
    public int UserId { get; init; }

    [JsonIgnore]
    public int UserQuestId { get; init; }

    public int CharacterId { get; init; }

    internal class Handler(
        ICrpgDbContext db,
        IMapper mapper,
        IDateTime dateTime,
        ICharacterService characterService,
        IActivityLogService activityLogService,
        IUserNotificationService userNotificationService,
        IQuestEvaluationService questEvaluationService) : IMediatorRequestHandler<ClaimQuestRewardCommand, UserQuestViewModel>
    {
        private static readonly ILogger Logger = LoggerFactory.CreateLogger<ClaimQuestRewardCommand>();

        private readonly ICrpgDbContext _db = db;
        private readonly IMapper _mapper = mapper;
        private readonly IDateTime _dateTime = dateTime;
        private readonly ICharacterService _characterService = characterService;
        private readonly IActivityLogService _activityLogService = activityLogService;
        private readonly IUserNotificationService _userNotificationService = userNotificationService;
        private readonly IQuestEvaluationService _questEvaluationService = questEvaluationService;

        public async ValueTask<Result<UserQuestViewModel>> Handle(ClaimQuestRewardCommand req, CancellationToken cancellationToken)
        {
            var userQuest = await _db.UserQuests
                .Include(uq => uq.QuestDefinition)
                .Include(uq => uq.User!)
                .FirstOrDefaultAsync(uq => uq.Id == req.UserQuestId && uq.UserId == req.UserId, cancellationToken);

            if (userQuest == null)
            {
                return new(CommonErrors.UserQuestNotFound(req.UserQuestId, req.UserId));
            }

            if (userQuest.IsRewardClaimed)
            {
                return new(CommonErrors.QuestRewardAlreadyClaimed(req.UserQuestId));
            }

            if (userQuest.ExpiresAt <= _dateTime.UtcNow)
            {
                return new(CommonErrors.QuestExpired(req.UserQuestId));
            }

            if (userQuest.QuestDefinition == null)
            {
                return new(CommonErrors.QuestDefinitionNotFound(userQuest.QuestDefinitionId));
            }

            int currentValue = await _questEvaluationService.ComputeCurrentValueAsync(userQuest, cancellationToken);
            if (currentValue < userQuest.QuestDefinition.RequiredValue)
            {
                return new(CommonErrors.QuestNotCompleted(req.UserQuestId, currentValue, userQuest.QuestDefinition.RequiredValue));
            }

            var user = userQuest.User!;

            var character = await _db.Characters
                .FirstOrDefaultAsync(c => c.Id == req.CharacterId && c.UserId == req.UserId, cancellationToken);

            if (character == null)
            {
                return new(CommonErrors.CharacterNotFound(req.CharacterId, req.UserId));
            }

            user.Gold += userQuest.QuestDefinition.RewardGold;

            // Give flat experience to the specified character
            _characterService.GiveExperience(character, userQuest.QuestDefinition.RewardExperience, useExperienceMultiplier: false);

            userQuest.IsRewardClaimed = true;

            _db.ActivityLogs.Add(_activityLogService.CreateQuestRewardClaimedLog(
                req.UserId, userQuest.Id, userQuest.QuestDefinition.RewardGold, userQuest.QuestDefinition.RewardExperience));
            _db.UserNotifications.Add(_userNotificationService.CreateQuestRewardClaimedToUserNotification(
                req.UserId, userQuest.Id, userQuest.QuestDefinition.RewardGold, userQuest.QuestDefinition.RewardExperience));

            await _db.SaveChangesAsync(cancellationToken);
            Logger.LogInformation("User '{0}' claimed reward for quest '{1}' on character '{2}'", req.UserId, req.UserQuestId, req.CharacterId);

            var vm = _mapper.Map<UserQuestViewModel>(userQuest);
            currentValue = Math.Min(currentValue, userQuest.QuestDefinition.RequiredValue);
            vm = vm with { CurrentValue = currentValue };
            return new Result<UserQuestViewModel>(vm);
        }
    }
}
