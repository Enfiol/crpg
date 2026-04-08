<script setup lang="ts">
import type { SelectItem } from '@nuxt/ui'

import { useAsyncState } from '@vueuse/core'

import type { UserQuestViewModel } from '~/services/quests-service'

import { useUser } from '~/composables/user/use-user'
import { useAsyncCallback } from '~/composables/utils/use-async-callback'
import { SomeRole } from '~/models/role'
import { getCharacters } from '~/services/character-service'
import { claimQuestReward, getUserQuests, rerollQuest } from '~/services/quests-service'

definePageMeta({
  roles: SomeRole,
})

const { locale } = useI18n()
const { n } = useI18n()

const { fetchUser } = useUser()

const {
  state: quests,
  isLoading,
  execute: loadQuests,
} = useAsyncState(() => getUserQuests(), [], { resetOnExecute: false })

const {
  state: characters,
  isLoading: loadingCharacters,
} = useAsyncState(() => getCharacters(), [])

// Claim dialog state
const claimDialogOpen = ref(false)
const claimingQuest = ref<UserQuestViewModel | null>(null)
const selectedCharacterId = ref<number | null>(null)

function openClaimDialog(quest: UserQuestViewModel) {
  claimingQuest.value = quest
  claimDialogOpen.value = true
}

const [onClaim, claiming] = useAsyncCallback(
  async () => {
    if (!claimingQuest.value || !selectedCharacterId.value) {
      return
    }
    await claimQuestReward(claimingQuest.value.id, selectedCharacterId.value)
    claimDialogOpen.value = false
    await Promise.all([loadQuests(), fetchUser()])
  },
  { successMessage: 'Reward claimed!' },
)

const [onReroll, rerolling] = useAsyncCallback(
  async (questId: number) => {
    await rerollQuest(questId)
    await Promise.all([loadQuests(), fetchUser()])
  },
  { successMessage: 'Quest rerolled!' },
)

function getQuestName(quest: UserQuestViewModel): string {
  return quest.questDefinition?.name?.[locale.value] ?? quest.questDefinition?.name?.en ?? '—'
}

function getQuestDescription(quest: UserQuestViewModel): string {
  return quest.questDefinition?.description?.[locale.value] ?? quest.questDefinition?.description?.en ?? ''
}

function isCompleted(quest: UserQuestViewModel): boolean {
  return quest.currentValue >= (quest.questDefinition?.requiredValue ?? 0)
}

function isExpired(quest: UserQuestViewModel): boolean {
  return new Date(quest.expiresAt) < new Date()
}

function progressPercent(quest: UserQuestViewModel): number {
  const required = quest.questDefinition?.requiredValue ?? 1
  return Math.min(100, Math.round((quest.currentValue / required) * 100))
}
</script>

<template>
  <UContainer
    class="
      py-8
      md:py-16
    "
  >
    <div class="mx-auto max-w-3xl">
      <h1 class="mb-10 text-center text-3xl font-bold">
        Daily Quests
      </h1>

      <div class="relative flex flex-col gap-4">
        <UiLoading :active="isLoading" />

        <UCard
          v-for="quest in quests"
          :key="quest.id"
          :class="{ 'opacity-60': quest.isRewardClaimed }"
        >
          <div class="flex flex-col gap-3">
            <!-- Header row -->
            <div class="flex items-start justify-between gap-2">
              <div>
                <p class="text-lg font-semibold">
                  {{ getQuestName(quest) }}
                </p>
                <p class="text-sm text-gray-400">
                  {{ getQuestDescription(quest) }}
                </p>
              </div>

              <div class="flex shrink-0 items-center gap-2">
                <UBadge
                  v-if="quest.isRewardClaimed"
                  color="success"
                  variant="subtle"
                  size="md"
                >
                  Claimed
                </UBadge>
                <UBadge
                  v-else-if="isExpired(quest)"
                  color="error"
                  variant="subtle"
                  size="md"
                >
                  Expired
                </UBadge>
                <UBadge
                  v-else-if="isCompleted(quest)"
                  color="primary"
                  variant="subtle"
                  size="md"
                >
                  Completed
                </UBadge>
              </div>
            </div>

            <!-- Progress -->
            <div class="flex items-center gap-3">
              {{ progressPercent(quest) }}
              <UProgress
                :model-value="progressPercent(quest)"
                color="primary"
                size="sm"
                class="flex-1"
              />
              <span class="min-w-24 text-right text-sm tabular-nums">
                {{ n(quest.currentValue) }} / {{ n(quest.questDefinition?.requiredValue ?? 0) }}
              </span>
            </div>

            <!-- Rewards and expiry -->
            <div class="flex flex-wrap items-center justify-between gap-2 text-sm">
              <div class="flex items-center gap-4">
                <span class="flex items-center gap-1">
                  <AppCoin :value="quest.questDefinition?.rewardGold ?? 0" />
                </span>
                <span class="flex items-center gap-1">
                  <AppExperience :value="quest.questDefinition?.rewardExperience ?? 0" />
                </span>
              </div>

              <span class="text-gray-500">
                Expires {{ new Date(quest.expiresAt).toLocaleDateString() }}
              </span>
            </div>

            <!-- Actions -->
            <div
              v-if="!quest.isRewardClaimed && !isExpired(quest)"
              class="flex justify-end gap-2"
            >
              <!-- v-if="isCompleted(quest)" -->
              <UButton
                color="primary"
                size="sm"
                :loading="claiming"
                @click="openClaimDialog(quest)"
              >
                Claim Reward
              </UButton>

              <!-- v-if="!isCompleted(quest)" -->
              <UButton
                color="neutral"
                variant="outline"
                size="sm"
                :loading="rerolling"
                @click="onReroll(quest.id)"
              >
                Reroll
              </UButton>
            </div>
          </div>
        </UCard>

        <UiCard v-if="!isLoading && quests.length === 0">
          <UiResultNotFound />
        </UiCard>
      </div>
    </div>

    <!-- Claim reward dialog -->
    <UModal v-model:open="claimDialogOpen">
      <template #content>
        <UCard>
          <template #header>
            <p class="text-lg font-semibold">
              Claim Reward
            </p>
          </template>

          <div class="flex flex-col gap-4">
            <p class="text-sm text-gray-400">
              Select a character to receive the reward for
              <span class="font-medium text-white">{{ claimingQuest ? getQuestName(claimingQuest) : '' }}</span>
            </p>

            <USelect
              v-model="selectedCharacterId"
              size="xl"
              :items="characters.map<SelectItem>((character) => ({
                label: character.name,
                value: character.id,
              }))"
              class="w-full"
            />

            <!-- <USelectMenu
              v-model="selectedCharacter"
              :items="characters"
              :loading="loadingCharacters"
              value-key="id"
              label-key="name"
              placeholder="Select character..."
              size="xl"
            /> -->

            <div
              v-if="claimingQuest"
              class="flex items-center gap-4 rounded-md px-4 py-3 text-sm"
            >
              <span class="flex items-center gap-1">
                <AppCoin :value="claimingQuest.questDefinition?.rewardGold ?? 0" />
              </span>
              <span class="flex items-center gap-1">
                <AppExperience :value="claimingQuest.questDefinition?.rewardExperience ?? 0" />
              </span>
            </div>
          </div>

          <template #footer>
            <div class="flex justify-end gap-2">
              <UButton
                color="neutral"
                variant="outline"
                @click="claimDialogOpen = false"
              >
                Cancel
              </UButton>
              <UButton
                color="primary"
                :disabled="!selectedCharacterId"
                :loading="claiming"
                @click="onClaim()"
              >
                Confirm
              </UButton>
            </div>
          </template>
        </UCard>
      </template>
    </UModal>
  </UContainer>
</template>
