import type { QuestType as _QuestType } from '#api'
import type { ValueOf } from 'type-fest'

export const QUEST_TYPE = {
  Daily: 'Daily',
  Weekly: 'Weekly',
} as const satisfies Record<_QuestType, _QuestType>

export type QuestType = ValueOf<typeof QUEST_TYPE>

export interface UserQuest {
  id: number
  isRewardClaimed: boolean
  expiresAt: Date
  currentValue: number
  questDefinition: QuestDefinition
}

export interface QuestDefinition {
  id: number
  type: QuestType
  name: {
    [key: string]: string
  } | null
  description: {
    [key: string]: string
  } | null
  requiredValue: number
  rewardGold: number
  rewardExperience: number
}
