import type { EventType as _EventType, QuestAggregationType as _QuestAggregationType, QuestType as _QuestType } from '#api'
import type { ValueOf } from 'type-fest'

export const QUEST_TYPE = {
  Daily: 'Daily',
  Weekly: 'Weekly',
} as const satisfies Record<_QuestType, _QuestType>

export type QuestType = ValueOf<typeof QUEST_TYPE>

export const QUEST_EVENT_TYPE = {
  Undefined: 'Undefined',
  Hit: 'Hit',
  Kill: 'Kill',
  Block: 'Block',
} as const satisfies Partial<Record<_EventType, _EventType>>

export type QuestEventType = ValueOf<typeof QUEST_EVENT_TYPE>

export const QUEST_AGGREGATION_TYPE = {
  Count: 'Count',
  Sum: 'Sum',
} as const satisfies Record<_QuestAggregationType, _QuestAggregationType>

export type QuestAggregationType = ValueOf<typeof QUEST_AGGREGATION_TYPE>

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
  eventType: QuestEventType
  aggregationType: QuestAggregationType
  sumField: string | null
  eventFiltersJson: Record<string, string>[]
  requiredValue: number
  rewardGold: number
  rewardExperience: number
}
