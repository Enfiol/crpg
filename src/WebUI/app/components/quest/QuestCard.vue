<script setup lang="ts">
import { questRerollDailyQuestPrice } from '~root/data/constants.json'

import type { UserQuest } from '~/models/quest'

import { useQuestDescription } from '~/composables/quest/use-quest-description'
import { QUEST_TYPE } from '~/models/quest'

const { quest } = defineProps<{
  quest: UserQuest
}>()

defineEmits<{
  claimReward: [quest: UserQuest]
  rerollQuest: [questId: number]
}>()

const { questName, questDescription } = useQuestDescription(() => quest)

function progressPercent(quest: UserQuest): number {
  const required = quest.questDefinition?.requiredValue ?? 1
  return Math.min(100, Math.round((quest.currentValue / required) * 100))
}

const isCompleted = computed(() => quest.currentValue >= (quest.questDefinition?.requiredValue ?? 0))
const isExpired = computed(() => new Date(quest.expiresAt) < new Date())

const timeRemaining = computed(() => parseTimestamp((new Date(quest.expiresAt).getTime() - new Date().getTime())))

const canReroll = computed(() => !isExpired.value && !quest.isRewardClaimed && quest.questDefinition.type === QUEST_TYPE.Daily)
const canClaim = computed(() => !isExpired.value && !quest.isRewardClaimed && isCompleted.value)
</script>

<template>
  <UCard
    :variant="quest.isRewardClaimed ? 'soft' : 'subtle'"
    :ui="{
      body: 'space-y-4.5',
    }"
  >
    <template #header>
      <div class="flex items-start justify-between gap-2">
        <UiDataContent
          :label="questName"
          :caption="questDescription"
        />

        <div class="flex shrink-0 items-center gap-2">
          <UTooltip v-if="!isExpired && !quest.isRewardClaimed" :text="$d(quest.expiresAt, 'short')">
            <UBadge
              variant="subtle"
              icon="i-lucide-clock"
            >
              {{ quest.questDefinition.type === QUEST_TYPE.Daily
                ? $t('dateTimeFormat.hh:mm', { ...timeRemaining })
                : $t('dateTimeFormat.dd:hh:mm', { ...timeRemaining })
              }}
            </UBadge>
          </UTooltip>

          <UBadge
            v-if="quest.isRewardClaimed"
            color="success"
            :label="$t('user.quests.status.claimed')"
          />
          <UBadge
            v-else-if="isExpired"
            color="warning"
            :label="$t('user.quests.status.expired')"
          />
        </div>
      </div>
    </template>

    <UProgress
      :model-value="progressPercent(quest)"
      status
      size="lg"
      class="flex-1"
      :ui="{
        status: 'text-highlighted',
      }"
    >
      <template #status="{ percent }">
        {{ percent }}%&nbsp;&nbsp;·&nbsp;&nbsp;{{ $n(quest.currentValue) }}/{{ $n(quest.questDefinition?.requiredValue ?? 0) }}
      </template>
    </UProgress>

    <div class="flex flex-wrap items-center justify-between gap-4">
      <div class="flex items-center gap-4">
        <AppCoin :value="quest.questDefinition.rewardGold" />
        <AppExperience :value="quest.questDefinition.rewardExperience" />
      </div>

      <div class="flex justify-end gap-2">
        <AppConfirmActionPopover
          v-if="canReroll"
          :title="$t('user.quests.action.reroll.confirmation', { goldCost: questRerollDailyQuestPrice })"
          :confirm-label="$t('user.quests.action.reroll.label')"
          @confirm="$emit('rerollQuest', quest.id)"
        >
          <UButton
            color="neutral"
            variant="soft"
            icon="i-lucide-dices"
            size="lg"
            :label="$t('user.quests.action.reroll.label')"
          />
          <template #title>
            <i18n-t
              scope="global"
              keypath="user.quests.action.reroll.confirmation"
              tag="div"
            >
              <template #goldCost>
                <AppCoin :value="questRerollDailyQuestPrice" />
              </template>
            </i18n-t>
          </template>
        </AppConfirmActionPopover>

        <UButton
          v-if="canClaim"
          color="primary"
          size="lg"
          variant="subtle"
          icon="crpg:chest"
          :label="$t('user.quests.action.claim.label')"
          @click="$emit('claimReward', quest)"
        />
      </div>
    </div>
  </UCard>
</template>
